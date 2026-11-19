using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using CampusBite.Api.Tests.Infrastructure;
using CampusBite.Shared.DTOs;
using CampusBite.Shared.Enums;
using CampusBite.Shared.Requests;
using CampusBite.Shared.Responses;
using FluentAssertions;
using Xunit;

namespace CampusBite.Api.Tests.Controllers;

public class SocialDiningAndSplitTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public SocialDiningAndSplitTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    private async Task<HttpClient> CreateAuthenticatedClientAsync(string email, string password)
    {
        var client = _factory.CreateClient();
        var loginResponse = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest
        {
            Email = email,
            Password = password
        });

        loginResponse.StatusCode.Should().Be(HttpStatusCode.OK, "Login helper must succeed");

        var authResult = await loginResponse.Content.ReadFromJsonAsync<ApiResponse<AuthResponse>>();
        authResult!.Data.Should().NotBeNull();

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", authResult.Data!.Token);
        return client;
    }

    private async Task<(HttpClient Client, string Email, string Username)> CreateUniqueStudentAsync(string usernamePrefix = "student")
    {
        var safePrefix = usernamePrefix.Length > 12 ? usernamePrefix[..12] : usernamePrefix;
        var uniqueId = Guid.NewGuid().ToString("N")[..6];
        var email = $"{safePrefix}_{uniqueId}@aau.edu.et";
        var username = $"{safePrefix}_{uniqueId}";

        var client = _factory.CreateClient();
        var regResponse = await client.PostAsJsonAsync("/api/auth/register", new RegisterRequest
        {
            FullName = $"Student {uniqueId}",
            Username = username,
            Email = email,
            Password = "Password123!",
            Role = UserRole.Student,
            UniversityId = 1
        });
        var body = await regResponse.Content.ReadAsStringAsync();
        regResponse.StatusCode.Should().Be(HttpStatusCode.OK, $"Registration must succeed for username '{username}'. Response: {body}");

        var authClient = await CreateAuthenticatedClientAsync(email, "Password123!");
        return (authClient, email, username);
    }

    private async Task SubscribeStudentToPlanAsync(HttpClient studentClient, int cafeId = 1)
    {
        var plansResponse = await studentClient.GetAsync($"/api/contracts/plans/cafe/{cafeId}");
        plansResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var plansResult = await plansResponse.Content.ReadFromJsonAsync<ApiResponse<List<ContractPlanDto>>>();
        plansResult!.Data.Should().NotBeEmpty();
        var plan = plansResult.Data!.First();

        var subResponse = await studentClient.PostAsJsonAsync("/api/contracts/subscribe", new SubscribeToPlanRequest
        {
            ContractPlanId = plan.Id
        });
        subResponse.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Single_Student_Pass_Covers_Meal_Up_To_Base_Credit_With_No_Cash_Due()
    {
        // 1. Arrange: Student with active pass (Base credit: 130 ETB)
        var (studentClient, _, _) = await CreateUniqueStudentAsync("full_cover");
        await SubscribeStudentToPlanAsync(studentClient);

        // Chechebsa with Honey (ProductId = 3) costs exactly 130 ETB
        var orderRequest = new CreateOrderRequest
        {
            CafeId = 1,
            DeliveryLocation = "Pickup Counter",
            PaymentMethod = PaymentMethod.MealContract,
            Items = new List<CreateOrderItemRequest>
            {
                new() { ProductId = 3, Quantity = 1 } // 130 ETB
            }
        };

        // 2. Act: Place pickup order
        var response = await studentClient.PostAsJsonAsync("/api/orders", orderRequest);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<ApiResponse<OrderDto>>();
        result!.Success.Should().BeTrue();
        var order = result.Data!;

        // 3. Assert: 100% of 130 ETB is covered by pass, 0 ETB cash due
        order.Subtotal.Should().Be(130.00m);
        order.DiscountAmount.Should().Be(130.00m);
        order.TotalAmount.Should().Be(0.00m);

        // Verify 1 credit was decremented
        var walletResponse = await studentClient.GetAsync("/api/contracts/my-contracts");
        var wallet = await walletResponse.Content.ReadFromJsonAsync<ApiResponse<List<StudentContractDto>>>();
        var activeContract = wallet!.Data!.First(c => c.CafeId == 1 && c.IsActive);
        activeContract.RemainingCredits.Should().Be(29); // 30 - 1 = 29
    }

    [Fact]
    public async Task Smart_Upgrade_Differential_Charges_Only_Price_Above_Base_Credit()
    {
        // 1. Arrange: Student with active pass (Base credit: 130 ETB)
        var (studentClient, _, _) = await CreateUniqueStudentAsync("upgrade");
        await SubscribeStudentToPlanAsync(studentClient);

        // Special Tibs (ProductId = 1) costs 220 ETB (220 > 130)
        var orderRequest = new CreateOrderRequest
        {
            CafeId = 1,
            DeliveryLocation = "Pickup Counter",
            PaymentMethod = PaymentMethod.MealContract,
            Items = new List<CreateOrderItemRequest>
            {
                new() { ProductId = 1, Quantity = 1 } // 220 ETB
            }
        };

        // 2. Act
        var response = await studentClient.PostAsJsonAsync("/api/orders", orderRequest);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<ApiResponse<OrderDto>>();
        var order = result!.Data!;

        // 3. Assert: 130 ETB covered, exactly 220 - 130 = 90 ETB cash upgrade due
        order.Subtotal.Should().Be(220.00m);
        order.DiscountAmount.Should().Be(130.00m);
        order.TotalAmount.Should().Be(90.00m); // 90 ETB cash upgrade
    }

    [Fact]
    public async Task Multi_Diner_Order_Dequeues_Passes_From_Host_And_Tagged_Buddy_Atomically()
    {
        // 1. Arrange: Host and Buddy both have active passes at Central Diner
        var (hostClient, _, hostUsername) = await CreateUniqueStudentAsync("host");
        await SubscribeStudentToPlanAsync(hostClient);

        var (buddyClient, _, buddyUsername) = await CreateUniqueStudentAsync("buddy");
        await SubscribeStudentToPlanAsync(buddyClient);

        // Host orders 2 meals: 1x Special Tibs (220 ETB) + 1x Chechebsa (130 ETB)
        // Tags buddy's username
        var orderRequest = new CreateOrderRequest
        {
            CafeId = 1,
            DeliveryLocation = "Pickup Counter",
            PaymentMethod = PaymentMethod.MealContract,
            CoDinerUsernames = new List<string> { buddyUsername },
            Items = new List<CreateOrderItemRequest>
            {
                new() { ProductId = 1, Quantity = 1 }, // 220 ETB
                new() { ProductId = 3, Quantity = 1 }  // 130 ETB
            }
        };

        // 2. Act
        var response = await hostClient.PostAsJsonAsync("/api/orders", orderRequest);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<ApiResponse<OrderDto>>();
        result!.Success.Should().BeTrue();
        var order = result.Data!;

        // 3. Assert Price Calculations:
        // Portions sorted descending: [220, 130]
        // Meal 1 (220 ETB): Host pass covers 130 ETB -> 90 ETB cash upgrade
        // Meal 2 (130 ETB): Buddy pass covers 130 ETB -> 0 ETB cash upgrade
        // Total Subtotal: 350 ETB
        // Total Discount: 130 + 130 = 260 ETB
        // Total Cash Due: 90 ETB
        order.Subtotal.Should().Be(350.00m);
        order.DiscountAmount.Should().Be(260.00m);
        order.TotalAmount.Should().Be(90.00m);

        // 4. Assert Host credits decremented by 1
        var hostWallet = await (await hostClient.GetAsync("/api/contracts/my-contracts"))
            .Content.ReadFromJsonAsync<ApiResponse<List<StudentContractDto>>>();
        hostWallet!.Data!.First(c => c.CafeId == 1 && c.IsActive).RemainingCredits.Should().Be(29);

        // 5. Assert Buddy credits decremented by 1
        var buddyWallet = await (await buddyClient.GetAsync("/api/contracts/my-contracts"))
            .Content.ReadFromJsonAsync<ApiResponse<List<StudentContractDto>>>();
        buddyWallet!.Data!.First(c => c.CafeId == 1 && c.IsActive).RemainingCredits.Should().Be(29);
    }

    [Fact]
    public async Task Multi_Diner_Fails_When_Tagged_Buddy_Has_No_Active_Pass()
    {
        // 1. Arrange: Host has pass, but buddy DOES NOT subscribe to any pass
        var (hostClient, _, _) = await CreateUniqueStudentAsync("host_ok");
        await SubscribeStudentToPlanAsync(hostClient);

        var (_, _, buddyWithNoPass) = await CreateUniqueStudentAsync("nobuddy_pass");

        var orderRequest = new CreateOrderRequest
        {
            CafeId = 1,
            DeliveryLocation = "Pickup Counter",
            PaymentMethod = PaymentMethod.MealContract,
            CoDinerUsernames = new List<string> { buddyWithNoPass },
            Items = new List<CreateOrderItemRequest>
            {
                new() { ProductId = 3, Quantity = 2 }
            }
        };

        // 2. Act
        var response = await hostClient.PostAsJsonAsync("/api/orders", orderRequest);

        // 3. Assert: Must reject and clearly indicate the buddy has no pass
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<OrderDto>>();
        result!.Success.Should().BeFalse();
        result.Message.Should().Contain(buddyWithNoPass);
        result.Message.Should().Contain("active pass");
    }

    [Fact]
    public async Task Group_Dining_With_3_Pass_Holders_And_1_Cash_Friend_Calculates_Differential_And_Bills_4th_Meal_To_Cash()
    {
        // 1. Arrange: 3 students with active passes (Host + Buddy A + Buddy B)
        var (hostClient, _, hostUsername) = await CreateUniqueStudentAsync("c_host");
        await SubscribeStudentToPlanAsync(hostClient);

        var (buddyAClient, _, buddyAUsername) = await CreateUniqueStudentAsync("c_buda");
        await SubscribeStudentToPlanAsync(buddyAClient);

        var (buddyBClient, _, buddyBUsername) = await CreateUniqueStudentAsync("c_budb");
        await SubscribeStudentToPlanAsync(buddyBClient);

        // 2. Act: Host places a table group order for 4 meals (3 friends with pass + 1 friend without pass)
        // 3x Special Tibs (220 ETB each) + 1x Chechebsa with Honey (130 ETB)
        // Subtotal = 220 + 220 + 220 + 130 = 790.00 ETB
        var orderRequest = new CreateOrderRequest
        {
            CafeId = 1,
            DeliveryLocation = "Pickup Counter",
            PaymentMethod = PaymentMethod.MealContract,
            CoDinerUsernames = new List<string> { buddyAUsername, buddyBUsername },
            Items = new List<CreateOrderItemRequest>
            {
                new() { ProductId = 1, Quantity = 3 }, // 3x 220 ETB = 660 ETB
                new() { ProductId = 3, Quantity = 1 }  // 1x 130 ETB = 130 ETB
            }
        };

        var response = await hostClient.PostAsJsonAsync("/api/orders", orderRequest);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<ApiResponse<OrderDto>>();
        result!.Success.Should().BeTrue();
        var order = result.Data!;

        // 3. Assert Smart Upgrade & Uncovered Breakdown:
        // Portions sorted descending: [220, 220, 220, 130]
        // Meal 1 (220 ETB): Host pass covers 130 ETB -> 90 ETB upgrade cash
        // Meal 2 (220 ETB): Buddy A pass covers 130 ETB -> 90 ETB upgrade cash
        // Meal 3 (220 ETB): Buddy B pass covers 130 ETB -> 90 ETB upgrade cash
        // Meal 4 (130 ETB): Uncovered 4th meal (cash friend) -> 130 ETB full cash
        // Subtotal: 790 ETB
        // Total Discount: 130 + 130 + 130 = 390.00 ETB
        // Total Cash Due: 90 + 90 + 90 + 130 = 400.00 ETB
        order.Subtotal.Should().Be(790.00m);
        order.DiscountAmount.Should().Be(390.00m);
        order.DeliveryFee.Should().Be(0.00m);
        order.TotalAmount.Should().Be(400.00m);

        // 4. Assert Atomic Decrements: Exactly 1 credit deducted from each of the 3 pass holders
        var hostWallet = await (await hostClient.GetAsync("/api/contracts/my-contracts"))
            .Content.ReadFromJsonAsync<ApiResponse<List<StudentContractDto>>>();
        hostWallet!.Data!.First(c => c.CafeId == 1 && c.IsActive).RemainingCredits.Should().Be(29);

        var buddyAWallet = await (await buddyAClient.GetAsync("/api/contracts/my-contracts"))
            .Content.ReadFromJsonAsync<ApiResponse<List<StudentContractDto>>>();
        buddyAWallet!.Data!.First(c => c.CafeId == 1 && c.IsActive).RemainingCredits.Should().Be(29);

        var buddyBWallet = await (await buddyBClient.GetAsync("/api/contracts/my-contracts"))
            .Content.ReadFromJsonAsync<ApiResponse<List<StudentContractDto>>>();
        buddyBWallet!.Data!.First(c => c.CafeId == 1 && c.IsActive).RemainingCredits.Should().Be(29);
    }
}

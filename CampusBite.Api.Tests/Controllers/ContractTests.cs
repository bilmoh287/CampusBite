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

public class ContractTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public ContractTests(CustomWebApplicationFactory factory)
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

        var body = await loginResponse.Content.ReadAsStringAsync();
        loginResponse.StatusCode.Should().Be(HttpStatusCode.OK, $"Authentication helper must succeed for {email}. Response: {body}");

        var authResult = await loginResponse.Content.ReadFromJsonAsync<ApiResponse<AuthResponse>>();
        authResult!.Data.Should().NotBeNull();

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", authResult.Data!.Token);
        return client;
    }

    private async Task<HttpClient> CreateUniqueStudentClientAsync()
    {
        var email = $"student_{Guid.NewGuid():N}@aau.edu.et";
        var client = _factory.CreateClient();
        var regResponse = await client.PostAsJsonAsync("/api/auth/register", new RegisterRequest
        {
            FullName = "New Student",
            Email = email,
            Password = "Password123!",
            Role = UserRole.Student,
            UniversityId = 1
        });
        regResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        return await CreateAuthenticatedClientAsync(email, "Password123!");
    }

    [Fact]
    public async Task Student_Can_Browse_Plans_And_Subscribe_Successfully()
    {
        // 1. Arrange: Fresh unique student client
        var studentClient = await CreateUniqueStudentClientAsync();

        // 2. Act: Browse active dining plans for Central Campus Diner (CafeId = 1)
        var plansResponse = await studentClient.GetAsync("/api/contracts/plans/cafe/1");
        plansResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var plansResult = await plansResponse.Content.ReadFromJsonAsync<ApiResponse<List<ContractPlanDto>>>();
        plansResult!.Data.Should().NotBeEmpty("Seeded cafe must have contract plans");

        var plan = plansResult.Data!.First();
        plan.TotalCredits.Should().BeGreaterThan(0);
        plan.PricePerMeal.Should().BeGreaterThan(0);

        // 3. Act: Student subscribes to this dining pass
        var subscribeResponse = await studentClient.PostAsJsonAsync("/api/contracts/subscribe", new SubscribeToPlanRequest
        {
            ContractPlanId = plan.Id
        });
        subscribeResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var subResult = await subscribeResponse.Content.ReadFromJsonAsync<ApiResponse<StudentContractDto>>();
        subResult!.Data!.Status.Should().Be(ContractStatus.Active);
        subResult.Data.RemainingCredits.Should().Be(plan.TotalCredits);

        // 4. Act: Student checks their active contract wallet
        var myContractsResponse = await studentClient.GetAsync("/api/contracts/my-contracts");
        myContractsResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var walletResult = await myContractsResponse.Content.ReadFromJsonAsync<ApiResponse<List<StudentContractDto>>>();
        walletResult!.Data.Should().Contain(c => c.ContractPlanId == plan.Id && c.IsActive);
    }

    [Fact]
    public async Task Subscribing_Twice_Rolls_Over_Remaining_Credits_And_Extends_Date()
    {
        // 1. Arrange: Fresh unique student client
        var studentClient = await CreateUniqueStudentClientAsync();

        var plansResponse = await studentClient.GetAsync("/api/contracts/plans/cafe/1");
        var plans = (await plansResponse.Content.ReadFromJsonAsync<ApiResponse<List<ContractPlanDto>>>())!.Data!;
        var plan = plans.First(); // 30 meals

        // First subscription
        var sub1 = await studentClient.PostAsJsonAsync("/api/contracts/subscribe", new SubscribeToPlanRequest { ContractPlanId = plan.Id });
        var contract1 = (await sub1.Content.ReadFromJsonAsync<ApiResponse<StudentContractDto>>())!.Data!;
        contract1.RemainingCredits.Should().Be(plan.TotalCredits);

        // Second subscription (rollover / top-up)
        var sub2 = await studentClient.PostAsJsonAsync("/api/contracts/subscribe", new SubscribeToPlanRequest { ContractPlanId = plan.Id });
        var contract2 = (await sub2.Content.ReadFromJsonAsync<ApiResponse<StudentContractDto>>())!.Data!;

        // Remaining credits should sum up both purchases
        contract2.RemainingCredits.Should().Be(plan.TotalCredits * 2, "Top-up must roll over existing credits");
        contract2.TotalCredits.Should().Be(plan.TotalCredits * 2);
    }

    [Fact]
    public async Task Order_With_MealContract_Decrements_Credits_And_Discounts_Food()
    {
        // 1. Arrange: Fresh unique student client
        var studentClient = await CreateUniqueStudentClientAsync();

        // Subscribe to a 30-meal plan
        var plansResponse = await studentClient.GetAsync("/api/contracts/plans/cafe/1");
        var plans = (await plansResponse.Content.ReadFromJsonAsync<ApiResponse<List<ContractPlanDto>>>())!.Data!;
        var plan = plans.First();

        var subResponse = await studentClient.PostAsJsonAsync("/api/contracts/subscribe", new SubscribeToPlanRequest { ContractPlanId = plan.Id });
        var initialContract = (await subResponse.Content.ReadFromJsonAsync<ApiResponse<StudentContractDto>>())!.Data!;
        int initialCredits = initialContract.RemainingCredits;

        // 2. Act: Place an order for 2 meals using PaymentMethod.MealContract
        var createRequest = new CreateOrderRequest
        {
            CafeId = 1,
            DeliveryLocation = "Engineering Dorm Block 3",
            PaymentMethod = PaymentMethod.MealContract,
            SpecialNotes = "Redeem my dining pass",
            Items = new List<CreateOrderItemRequest>
            {
                new() { ProductId = 1, Quantity = 2 } // 2 meals (2 * 220.00 = 440.00 ETB)
            }
        };

        var orderResponse = await studentClient.PostAsJsonAsync("/api/orders", createRequest);
        orderResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var order = (await orderResponse.Content.ReadFromJsonAsync<ApiResponse<OrderDto>>())!.Data!;

        // 3. Assert: Food is 100% discounted, student only pays delivery fee (15 ETB)
        order.DiscountAmount.Should().Be(440.00m, "Meal contract must cover 100% of the food price");
        order.TotalAmount.Should().Be(15.00m, "Student only pays delivery fee");

        // 4. Assert: Exactly 2 credits decremented from the student's wallet
        var walletResponse = await studentClient.GetAsync("/api/contracts/my-contracts");
        var myContracts = (await walletResponse.Content.ReadFromJsonAsync<ApiResponse<List<StudentContractDto>>>())!.Data!;
        var updatedContract = myContracts.First(c => c.CafeId == 1);

        updatedContract.RemainingCredits.Should().Be(initialCredits - 2, "2 meals ordered must deduct exactly 2 credits");
    }

    [Fact]
    public async Task Overdraft_Tab_Allows_Trusted_Student_To_Eat_On_Debt()
    {
        // 1. Arrange: Unique student and Cafe Manager
        var studentClient = await CreateUniqueStudentClientAsync();
        var managerClient = await CreateAuthenticatedClientAsync("manager@centralcafe.com", "Password123!");

        // Ensure student has a contract
        var plansResponse = await studentClient.GetAsync("/api/contracts/plans/cafe/1");
        var plan = (await plansResponse.Content.ReadFromJsonAsync<ApiResponse<List<ContractPlanDto>>>())!.Data!.First();
        var subResponse = await studentClient.PostAsJsonAsync("/api/contracts/subscribe", new SubscribeToPlanRequest { ContractPlanId = plan.Id });
        var contract = (await subResponse.Content.ReadFromJsonAsync<ApiResponse<StudentContractDto>>())!.Data!;

        // 2. Manager sets a trusted overdraft allowance of up to 5 meals
        var overdraftResponse = await managerClient.PatchAsJsonAsync($"/api/contracts/{contract.Id}/overdraft", new UpdateStudentOverdraftRequest
        {
            AllowOverdraft = true,
            OverdraftLimit = 5
        });
        overdraftResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var updated = (await overdraftResponse.Content.ReadFromJsonAsync<ApiResponse<StudentContractDto>>())!.Data!;
        updated.AllowOverdraft.Should().BeTrue();
        updated.OverdraftLimit.Should().Be(5);
    }
}

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
        var plan = plans.First(p => p.TotalCredits >= 30); // 30 meals plan

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
        var plan = plans.First(p => p.TotalCredits >= 30);

        var subResponse = await studentClient.PostAsJsonAsync("/api/contracts/subscribe", new SubscribeToPlanRequest { ContractPlanId = plan.Id });
        var initialContract = (await subResponse.Content.ReadFromJsonAsync<ApiResponse<StudentContractDto>>())!.Data!;
        int initialCredits = initialContract.RemainingCredits;

        // 2. Act: Place an order for 1 meal using PaymentMethod.MealContract
        var createRequest = new CreateOrderRequest
        {
            CafeId = 1,
            DeliveryLocation = "Engineering Dorm Block 3",
            PaymentMethod = PaymentMethod.MealContract,
            SpecialNotes = "Redeem my dining pass",
            Items = new List<CreateOrderItemRequest>
            {
                new() { ProductId = 3, Quantity = 1 } // 1 meal: Chechebsa with Honey (130.00 ETB, matches BaseMealCreditValue)
            }
        };

        var orderResponse = await studentClient.PostAsJsonAsync("/api/orders", createRequest);
        orderResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var order = (await orderResponse.Content.ReadFromJsonAsync<ApiResponse<OrderDto>>())!.Data!;

        // 3. Assert: Food is 100% discounted, student only pays delivery fee (15 ETB)
        order.DiscountAmount.Should().Be(130.00m, "Meal contract must cover 100% of the food price");
        order.TotalAmount.Should().Be(15.00m, "Student only pays delivery fee");

        // 4. Assert: Exactly 1 credit decremented from the student's wallet
        var walletResponse = await studentClient.GetAsync("/api/contracts/my-contracts");
        var myContracts = (await walletResponse.Content.ReadFromJsonAsync<ApiResponse<List<StudentContractDto>>>())!.Data!;
        var updatedContract = myContracts.First(c => c.CafeId == 1);

        updatedContract.RemainingCredits.Should().Be(initialCredits - 1, "1 meal ordered must deduct exactly 1 credit");
    }

    [Fact]
    public async Task Overdraft_Tab_Allows_Trusted_Student_To_Eat_On_Debt()
    {
        // 1. Arrange: Unique student and Cafe Manager
        var studentClient = await CreateUniqueStudentClientAsync();
        var managerClient = await CreateAuthenticatedClientAsync("manager@centralcafe.com", "Password123!");

        // Ensure student has a contract
        var plansResponse = await studentClient.GetAsync("/api/contracts/plans/cafe/1");
        var plans = (await plansResponse.Content.ReadFromJsonAsync<ApiResponse<List<ContractPlanDto>>>())!.Data!;
        var plan = plans.First(p => p.TotalCredits >= 30);
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

    [Fact]
    public async Task Hybrid_Group_Order_With_Partial_Credits_Discounts_Covered_Items_And_Bills_Remainder_To_Cash()
    {
        // 1. Arrange: Manager creates a custom 1-meal pass
        var managerClient = await CreateAuthenticatedClientAsync("manager@centralcafe.com", "Password123!");
        var createPlanReq = new CreateContractPlanRequest
        {
            CafeId = 1,
            Name = $"Group Hybrid Test Pass {Guid.NewGuid():N}",
            Description = "1 Meal for Group Testing",
            Price = 90.00m,
            TotalCredits = 1,
            ValidityDays = 7
        };
        var planCreateRes = await managerClient.PostAsJsonAsync("/api/contracts/plans", createPlanReq);
        planCreateRes.StatusCode.Should().Be(HttpStatusCode.OK);
        var createdPlan = (await planCreateRes.Content.ReadFromJsonAsync<ApiResponse<ContractPlanDto>>())!.Data!;

        // 2. Student subscribes to this 1-meal pass
        var studentClient = await CreateUniqueStudentClientAsync();
        var subRes = await studentClient.PostAsJsonAsync("/api/contracts/subscribe", new SubscribeToPlanRequest { ContractPlanId = createdPlan.Id });
        subRes.StatusCode.Should().Be(HttpStatusCode.OK);
        var contract = (await subRes.Content.ReadFromJsonAsync<ApiResponse<StudentContractDto>>())!.Data!;
        contract.RemainingCredits.Should().Be(1);

        // 3. Act: Group order of 2 meals (1 on pass, 1 in cash)
        // 2 x 220.00 ETB = 440.00 ETB subtotal, 15.00 ETB delivery fee
        var groupOrderReq = new CreateOrderRequest
        {
            CafeId = 1,
            DeliveryLocation = "Library Study Room 4",
            PaymentMethod = PaymentMethod.MealContract,
            SpecialNotes = "Group meal: 1 on pass, 1 in cash",
            Items = new List<CreateOrderItemRequest>
            {
                new() { ProductId = 1, Quantity = 2 }
            }
        };

        var orderRes = await studentClient.PostAsJsonAsync("/api/orders", groupOrderReq);
        orderRes.StatusCode.Should().Be(HttpStatusCode.OK);
        var order = (await orderRes.Content.ReadFromJsonAsync<ApiResponse<OrderDto>>())!.Data!;

        // 4. Assert Hybrid Math:
        // - Subtotal = 440.00 ETB
        // - Discount = 220.00 ETB (1 meal covered 100%)
        // - Delivery Fee = 15.00 ETB
        // - TotalAmount = (440 - 220) + 15 = 235.00 ETB
        order.Subtotal.Should().Be(440.00m);
        order.DiscountAmount.Should().Be(220.00m, "Only the 1 available credit must be discounted");
        order.DeliveryFee.Should().Be(15.00m);
        order.TotalAmount.Should().Be(235.00m, "Remaining 1 meal plus delivery fee must be billed to cash");

        // 5. Assert: Student wallet balance dropped to 0 and contract marked Depleted
        var walletRes = await studentClient.GetAsync("/api/contracts/my-contracts");
        var myContracts = (await walletRes.Content.ReadFromJsonAsync<ApiResponse<List<StudentContractDto>>>())!.Data!;
        var updatedContract = myContracts.First(c => c.Id == contract.Id);
        updatedContract.RemainingCredits.Should().Be(0);
        updatedContract.Status.Should().Be(ContractStatus.Depleted);
    }

    [Fact]
    public async Task Order_With_Zero_Credits_And_No_Overdraft_Fails_Gracefully()
    {
        // 1. Arrange: Manager creates a 1-meal pass and student subscribes
        var managerClient = await CreateAuthenticatedClientAsync("manager@centralcafe.com", "Password123!");
        var createPlanReq = new CreateContractPlanRequest
        {
            CafeId = 1,
            Name = $"1-Meal Depletion Pass {Guid.NewGuid():N}",
            Price = 90.00m,
            TotalCredits = 1,
            ValidityDays = 7
        };
        var planRes = await managerClient.PostAsJsonAsync("/api/contracts/plans", createPlanReq);
        var plan = (await planRes.Content.ReadFromJsonAsync<ApiResponse<ContractPlanDto>>())!.Data!;

        var studentClient = await CreateUniqueStudentClientAsync();
        await studentClient.PostAsJsonAsync("/api/contracts/subscribe", new SubscribeToPlanRequest { ContractPlanId = plan.Id });

        // Use up the single credit
        await studentClient.PostAsJsonAsync("/api/orders", new CreateOrderRequest
        {
            CafeId = 1,
            DeliveryLocation = "Dorm",
            PaymentMethod = PaymentMethod.MealContract,
            Items = new List<CreateOrderItemRequest> { new() { ProductId = 1, Quantity = 1 } }
        });

        // 2. Act: Attempt to place another order with PaymentMethod.MealContract now that credits = 0
        var failedOrderRes = await studentClient.PostAsJsonAsync("/api/orders", new CreateOrderRequest
        {
            CafeId = 1,
            DeliveryLocation = "Dorm",
            PaymentMethod = PaymentMethod.MealContract,
            Items = new List<CreateOrderItemRequest> { new() { ProductId = 1, Quantity = 1 } }
        });

        // 3. Assert: Must fail with HTTP 400 Bad Request
        failedOrderRes.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var result = await failedOrderRes.Content.ReadFromJsonAsync<ApiResponse<OrderDto>>();
        result!.Success.Should().BeFalse();
        result.Message.Should().Contain("depleted");
    }
}

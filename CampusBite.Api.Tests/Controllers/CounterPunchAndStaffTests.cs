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

public class CounterPunchAndStaffTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public CounterPunchAndStaffTests(CustomWebApplicationFactory factory)
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

    private async Task<(HttpClient Client, string Email, string Username, string Password)> CreateUniqueUserAsync(UserRole role = UserRole.Student, string prefix = "user")
    {
        var uniqueId = Guid.NewGuid().ToString("N")[..6];
        var email = $"{prefix}_{uniqueId}@campusbite.com";
        var username = $"{prefix}_{uniqueId}";
        var password = "Password123!";

        var client = _factory.CreateClient();
        var regResponse = await client.PostAsJsonAsync("/api/auth/register", new RegisterRequest
        {
            FullName = $"{prefix} {uniqueId}",
            Username = username,
            Email = email,
            Password = password,
            Role = role,
            UniversityId = 1
        });
        regResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var authClient = await CreateAuthenticatedClientAsync(email, password);
        return (authClient, email, username, password);
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
    public async Task Cashier_Can_Counter_Punch_Pass_For_WalkIn_Student_Without_Phone()
    {
        // 1. Arrange: Student has active pass
        var (studentClient, _, studentUsername, _) = await CreateUniqueUserAsync(UserRole.Student, "walkin");
        await SubscribeStudentToPlanAsync(studentClient);

        // Cafe Manager client
        var managerClient = await CreateAuthenticatedClientAsync("manager@centralcafe.com", "Password123!");

        var punchRequest = new CounterPunchRequest
        {
            Username = studentUsername,
            ProductId = 3, // Chechebsa (130 ETB)
            Notes = "Student forgot phone at dorm - counter verified"
        };

        // 2. Act: Cashier punches pass
        var punchResponse = await managerClient.PostAsJsonAsync("/api/cafes/1/counter-punch", punchRequest);
        punchResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await punchResponse.Content.ReadFromJsonAsync<ApiResponse<OrderDto>>();
        result!.Success.Should().BeTrue();
        var order = result.Data!;

        // 3. Assert: WLK ticket created and credits decremented
        order.OrderNumber.Should().StartWith("WLK-");
        order.DeliveryLocation.Should().Be("Counter (Walk-in)");
        order.Subtotal.Should().Be(130.00m);
        order.TotalAmount.Should().Be(0.00m); // 130 covered by base credit

        var walletResponse = await studentClient.GetAsync("/api/contracts/my-contracts");
        var wallet = await walletResponse.Content.ReadFromJsonAsync<ApiResponse<List<StudentContractDto>>>();
        wallet!.Data!.First(c => c.CafeId == 1 && c.IsActive).RemainingCredits.Should().Be(29);
    }

    [Fact]
    public async Task Counter_Punch_Fails_If_Student_Has_No_Active_Pass()
    {
        // 1. Arrange: Student without pass
        var (_, _, studentWithoutPass, _) = await CreateUniqueUserAsync(UserRole.Student, "nopass_walkin");
        var managerClient = await CreateAuthenticatedClientAsync("manager@centralcafe.com", "Password123!");

        var punchRequest = new CounterPunchRequest
        {
            Username = studentWithoutPass,
            ProductId = 3,
            Notes = "Walk-in"
        };

        // 2. Act
        var punchResponse = await managerClient.PostAsJsonAsync("/api/cafes/1/counter-punch", punchRequest);

        // 3. Assert
        punchResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var result = await punchResponse.Content.ReadFromJsonAsync<ApiResponse<OrderDto>>();
        result!.Success.Should().BeFalse();
        result.Message.Should().Contain("active pass");
    }

    [Fact]
    public async Task Cafe_Owner_Can_Onboard_Kitchen_Staff_Successfully()
    {
        // 1. Arrange: Create a user to be onboarded as cook
        var (_, _, cookUsername, cookPassword) = await CreateUniqueUserAsync(UserRole.Student, "future_cook");
        var managerClient = await CreateAuthenticatedClientAsync("manager@centralcafe.com", "Password123!");

        // 2. Act: Owner assigns staff
        var addStaffResponse = await managerClient.PostAsJsonAsync("/api/cafes/1/staff", cookUsername);
        addStaffResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var addResult = await addStaffResponse.Content.ReadFromJsonAsync<ApiResponse<UserDto>>();
        addResult!.Success.Should().BeTrue();
        addResult.Data!.Role.Should().Be(UserRole.CafeStaff);
        addResult.Data.EmployedAtCafeId.Should().Be(1);

        // 3. Assert: Staff member is present in staff roster
        var getStaffResponse = await managerClient.GetAsync("/api/cafes/1/staff");
        getStaffResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var staffRoster = await getStaffResponse.Content.ReadFromJsonAsync<ApiResponse<List<UserDto>>>();
        staffRoster!.Data.Should().Contain(u => u.Username == cookUsername && u.Role == UserRole.CafeStaff);
    }

    [Fact]
    public async Task Non_Owner_Is_Forbidden_From_Adding_Staff()
    {
        // 1. Arrange: Regular student tries to add staff
        var (studentClient, _, _, _) = await CreateUniqueUserAsync(UserRole.Student, "intruder");

        // 2. Act: Student attempts to POST to staff endpoint
        var response = await studentClient.PostAsJsonAsync("/api/cafes/1/staff", "some_user");

        // 3. Assert: 403 Forbidden
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Kitchen_Staff_Can_Execute_Counter_Punch_Successfully()
    {
        // 1. Arrange: Onboard a staff member
        var (_, cookEmail, cookUsername, cookPassword) = await CreateUniqueUserAsync(UserRole.Student, "hired_cook");
        var managerClient = await CreateAuthenticatedClientAsync("manager@centralcafe.com", "Password123!");
        await managerClient.PostAsJsonAsync("/api/cafes/1/staff", cookUsername);

        // Student with active pass
        var (studentClient, _, studentUsername, _) = await CreateUniqueUserAsync(UserRole.Student, "customer");
        await SubscribeStudentToPlanAsync(studentClient);

        // 2. Act: Cook logs in with their credentials
        var cookClient = await CreateAuthenticatedClientAsync(cookEmail, cookPassword);

        var punchRequest = new CounterPunchRequest
        {
            Username = studentUsername,
            ProductId = 3,
            Notes = "Punched by kitchen cook"
        };

        var punchResponse = await cookClient.PostAsJsonAsync("/api/cafes/1/counter-punch", punchRequest);

        // 3. Assert: Cook is authorized to punch passes
        punchResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await punchResponse.Content.ReadFromJsonAsync<ApiResponse<OrderDto>>();
        result!.Success.Should().BeTrue();
        result.Data!.OrderNumber.Should().StartWith("WLK-");
    }
}

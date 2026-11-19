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

public class CafeManagementTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public CafeManagementTests(CustomWebApplicationFactory factory)
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

    private async Task<HttpClient> CreateUniqueUserClientAsync(UserRole role)
    {
        var email = $"{role.ToString().ToLower()}_{Guid.NewGuid():N}@campusbite.com";
        var client = _factory.CreateClient();
        var regResponse = await client.PostAsJsonAsync("/api/auth/register", new RegisterRequest
        {
            FullName = $"Test {role}",
            Email = email,
            Password = "Password123!",
            Role = role,
            UniversityId = 1
        });
        regResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        return await CreateAuthenticatedClientAsync(email, "Password123!");
    }

    [Fact]
    public async Task Owner_Can_Update_Announcement_And_Viewers_See_It()
    {
        // Arrange
        var ownerClient = await CreateAuthenticatedClientAsync("manager@centralcafe.com", "Password123!");
        var announcementText = "Special Tibs is 20% off for the next 30 minutes! 📢";

        // Act - Owner updates announcement
        var patchResponse = await ownerClient.PatchAsJsonAsync("/api/cafes/1/announcement", new UpdateCafeAnnouncementRequest
        {
            Announcement = announcementText
        });

        // Assert PATCH response
        patchResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var patchResult = await patchResponse.Content.ReadFromJsonAsync<ApiResponse<CafeDto>>();
        patchResult!.Success.Should().BeTrue();
        patchResult.Data!.Announcement.Should().Be(announcementText);

        // Assert Public GET response reflects the announcement
        var publicClient = _factory.CreateClient();
        var getResponse = await publicClient.GetAsync("/api/cafes/1");
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var getResult = await getResponse.Content.ReadFromJsonAsync<ApiResponse<CafeDto>>();
        getResult!.Data!.Announcement.Should().Be(announcementText);
    }

    [Fact]
    public async Task Owner_Can_Clear_Announcement_Successfully()
    {
        // Arrange
        var ownerClient = await CreateAuthenticatedClientAsync("manager@centralcafe.com", "Password123!");

        // Act - Clear announcement by sending empty string
        var patchResponse = await ownerClient.PatchAsJsonAsync("/api/cafes/1/announcement", new UpdateCafeAnnouncementRequest
        {
            Announcement = ""
        });

        // Assert
        patchResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var patchResult = await patchResponse.Content.ReadFromJsonAsync<ApiResponse<CafeDto>>();
        patchResult!.Success.Should().BeTrue();
        patchResult.Data!.Announcement.Should().BeNull();
    }

    [Fact]
    public async Task Owner_Can_Toggle_Cafe_Open_And_Closed_Status()
    {
        // Arrange
        var ownerClient = await CreateAuthenticatedClientAsync("manager@centralcafe.com", "Password123!");

        // Act 1: Close the cafe
        var closeResponse = await ownerClient.PatchAsJsonAsync("/api/cafes/1/status", new ToggleCafeStatusRequest
        {
            IsOpen = false
        });

        // Assert 1: Closed
        closeResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var closeResult = await closeResponse.Content.ReadFromJsonAsync<ApiResponse<CafeDto>>();
        closeResult!.Data!.IsActive.Should().BeFalse();

        // Act 2: Reopen the cafe
        var openResponse = await ownerClient.PatchAsJsonAsync("/api/cafes/1/status", new ToggleCafeStatusRequest
        {
            IsOpen = true
        });

        // Assert 2: Reopened
        openResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var openResult = await openResponse.Content.ReadFromJsonAsync<ApiResponse<CafeDto>>();
        openResult!.Data!.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task Student_Cannot_Update_Cafe_Announcement_Returns_403Forbidden()
    {
        // Arrange
        var studentClient = await CreateUniqueUserClientAsync(UserRole.Student);

        // Act
        var response = await studentClient.PatchAsJsonAsync("/api/cafes/1/announcement", new UpdateCafeAnnouncementRequest
        {
            Announcement = "Hacked announcement by a student!"
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Courier_Cannot_Toggle_Cafe_Status_Returns_403Forbidden()
    {
        // Arrange
        var courierClient = await CreateUniqueUserClientAsync(UserRole.Courier);

        // Act
        var response = await courierClient.PatchAsJsonAsync("/api/cafes/1/status", new ToggleCafeStatusRequest
        {
            IsOpen = false
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Another_CafeOwner_Cannot_Modify_Competitor_Cafe_Announcement()
    {
        // Arrange: Create a second cafe owner who does NOT own Cafe #1
        var competitorOwnerClient = await CreateUniqueUserClientAsync(UserRole.CafeOwner);

        // Act: Attempt to modify Cafe #1's announcement
        var response = await competitorOwnerClient.PatchAsJsonAsync("/api/cafes/1/announcement", new UpdateCafeAnnouncementRequest
        {
            Announcement = "Malicious takeover attempt!"
        });

        // Assert: Must be rejected with 400 Bad Request / failure message
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<CafeDto>>();
        result!.Success.Should().BeFalse();
        result.Message.Should().Contain("permission");
    }

    [Fact]
    public async Task Owner_Can_Update_Takeaway_Packaging_Fee_And_Public_Sees_It()
    {
        // Arrange
        var ownerClient = await CreateAuthenticatedClientAsync("manager@centralcafe.com", "Password123!");

        // Act - Owner updates takeaway packaging fee to 35 ETB
        var patchResponse = await ownerClient.PatchAsJsonAsync("/api/cafes/1/packaging-fee", new UpdatePackagingFeeRequest
        {
            TakeawayPackagingFee = 35.00m
        });

        // Assert PATCH response
        patchResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var patchResult = await patchResponse.Content.ReadFromJsonAsync<ApiResponse<CafeDto>>();
        patchResult!.Success.Should().BeTrue();
        patchResult.Data!.TakeawayPackagingFee.Should().Be(35.00m);

        // Assert Public GET response reflects the updated fee
        var publicClient = _factory.CreateClient();
        var getResponse = await publicClient.GetAsync("/api/cafes/1");
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var getResult = await getResponse.Content.ReadFromJsonAsync<ApiResponse<CafeDto>>();
        getResult!.Data!.TakeawayPackagingFee.Should().Be(35.00m);
    }

    [Fact]
    public async Task Owner_Can_Set_Packaging_Fee_To_Zero_For_Free_Festal_Only()
    {
        // Arrange
        var ownerClient = await CreateAuthenticatedClientAsync("manager@centralcafe.com", "Password123!");

        // Act - Owner resets fee to 0 (all takeaway is free festal/paper)
        var patchResponse = await ownerClient.PatchAsJsonAsync("/api/cafes/1/packaging-fee", new UpdatePackagingFeeRequest
        {
            TakeawayPackagingFee = 0.00m
        });

        // Assert
        patchResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var patchResult = await patchResponse.Content.ReadFromJsonAsync<ApiResponse<CafeDto>>();
        patchResult!.Data!.TakeawayPackagingFee.Should().Be(0.00m);
    }

    [Fact]
    public async Task Student_Cannot_Update_Packaging_Fee_Returns_403Forbidden()
    {
        // Arrange
        var studentClient = await CreateUniqueUserClientAsync(UserRole.Student);

        // Act
        var response = await studentClient.PatchAsJsonAsync("/api/cafes/1/packaging-fee", new UpdatePackagingFeeRequest
        {
            TakeawayPackagingFee = 0.00m
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Another_CafeOwner_Cannot_Modify_Competitor_Packaging_Fee()
    {
        // Arrange: Competitor owner who doesn't own Cafe 1
        var competitorOwnerClient = await CreateUniqueUserClientAsync(UserRole.CafeOwner);

        // Act
        var response = await competitorOwnerClient.PatchAsJsonAsync("/api/cafes/1/packaging-fee", new UpdatePackagingFeeRequest
        {
            TakeawayPackagingFee = 50.00m
        });

        // Assert: 400 Bad Request
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<CafeDto>>();
        result!.Success.Should().BeFalse();
        result.Message.Should().Contain("permission");
    }
}

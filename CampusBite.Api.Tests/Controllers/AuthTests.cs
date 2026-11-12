using System.Net;
using System.Net.Http.Json;
using CampusBite.Api.Tests.Infrastructure;
using CampusBite.Shared.Enums;
using CampusBite.Shared.Requests;
using CampusBite.Shared.Responses;
using FluentAssertions;
using Xunit;

namespace CampusBite.Api.Tests.Controllers;

public class AuthTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public AuthTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Login_WithValidStudentCredentials_ReturnsSuccessAndJwtToken()
    {
        // 1. Arrange: Prepare valid seeded student credentials
        var request = new LoginRequest
        {
            Email = "student@aau.edu.et",
            Password = "Password123!"
        };

        // 2. Act: Send POST /api/auth/login
        var response = await _client.PostAsJsonAsync("/api/auth/login", request);

        // 3. Assert: Verify HTTP 200 OK and presence of JWT token + user details
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<ApiResponse<AuthResponse>>();
        result.Should().NotBeNull();
        result!.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.Token.Should().NotBeNullOrWhiteSpace("JWT access token must be generated");
        result.Data.RefreshToken.Should().NotBeNullOrWhiteSpace("Refresh token must be generated");
        result.Data.User.Email.Should().Be("student@aau.edu.et");
        result.Data.User.Role.Should().Be(UserRole.Student);
    }

    [Fact]
    public async Task Login_WithIncorrectPassword_ReturnsBadRequest()
    {
        // 1. Arrange: Existing user with wrong password
        var request = new LoginRequest
        {
            Email = "student@aau.edu.et",
            Password = "IncorrectPassword999!"
        };

        // 2. Act: Send POST /api/auth/login
        var response = await _client.PostAsJsonAsync("/api/auth/login", request);

        // 3. Assert: Must be rejected with Bad Request
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var result = await response.Content.ReadFromJsonAsync<ApiResponse<AuthResponse>>();
        result.Should().NotBeNull();
        result!.Success.Should().BeFalse();
        result.Message.Should().Contain("Invalid email or password");
    }

    [Fact]
    public async Task Register_WithValidStudentData_ReturnsSuccessAndJwtToken()
    {
        // 1. Arrange: A unique student account to register
        var uniqueEmail = $"student_{Guid.NewGuid():N}@aau.edu.et";
        var request = new RegisterRequest
        {
            FullName = "Freshman Student",
            Email = uniqueEmail,
            Password = "Password123!",
            PhoneNumber = "+251911223344",
            Role = UserRole.Student,
            UniversityId = 1
        };

        // 2. Act: Send POST /api/auth/register
        var response = await _client.PostAsJsonAsync("/api/auth/register", request);

        // 3. Assert: Verify account is created and automatically logged in
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<ApiResponse<AuthResponse>>();
        result.Should().NotBeNull();
        result!.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.Token.Should().NotBeNullOrWhiteSpace();
        result.Data.User.Email.Should().Be(uniqueEmail);
        result.Data.User.Role.Should().Be(UserRole.Student);
    }

    [Fact]
    public async Task Register_WithExistingEmail_ReturnsBadRequest()
    {
        // 1. Arrange: Attempt to register with the seeded student's email
        var request = new RegisterRequest
        {
            FullName = "Duplicate User",
            Email = "student@aau.edu.et",
            Password = "Password123!",
            Role = UserRole.Student
        };

        // 2. Act: Send POST /api/auth/register
        var response = await _client.PostAsJsonAsync("/api/auth/register", request);

        // 3. Assert: Must be rejected because email is already registered
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var result = await response.Content.ReadFromJsonAsync<ApiResponse<AuthResponse>>();
        result.Should().NotBeNull();
        result!.Success.Should().BeFalse();
        result.Message.Should().Contain("already in use");
    }
}

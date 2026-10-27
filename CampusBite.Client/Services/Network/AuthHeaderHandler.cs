using CampusBite.Services.Interfaces;
using CampusBite.Shared.Requests;
using CampusBite.Shared.Responses;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace CampusBite.Services.Network;

public class AuthHeaderHandler : DelegatingHandler
{
    private readonly IAuthTokenStorage _tokenStorage;
    private static readonly SemaphoreSlim _refreshLock = new(1, 1);

    public AuthHeaderHandler(IAuthTokenStorage tokenStorage)
    {
        _tokenStorage = tokenStorage;
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        // 1. Attach current access token if available
        var token = await _tokenStorage.GetAccessTokenAsync();
        if (!string.IsNullOrWhiteSpace(token) && request.Headers.Authorization == null)
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        // 2. Execute original request
        var response = await base.SendAsync(request, cancellationToken);

        // 3. If response is 401 Unauthorized, attempt transparent token refresh
        var requestPath = request.RequestUri?.AbsolutePath ?? string.Empty;
        var isAuthEndpoint = requestPath.Contains(ApiConstants.AuthLogin, StringComparison.OrdinalIgnoreCase) ||
                             requestPath.Contains(ApiConstants.AuthRefreshToken, StringComparison.OrdinalIgnoreCase);

        if (response.StatusCode == HttpStatusCode.Unauthorized && !isAuthEndpoint)
        {
            var refreshed = await TryRefreshTokenAsync(cancellationToken);
            if (refreshed)
            {
                // Clone request for retry (original request content stream may have been consumed)
                using var retryRequest = await CloneHttpRequestMessageAsync(request);
                var newToken = await _tokenStorage.GetAccessTokenAsync();
                retryRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", newToken);

                // Dispose old response before retry
                response.Dispose();
                return await base.SendAsync(retryRequest, cancellationToken);
            }
            else
            {
                // Refresh failed or revoked; clear credentials
                await _tokenStorage.ClearAuthDataAsync();
            }
        }

        return response;
    }

    private async Task<bool> TryRefreshTokenAsync(CancellationToken cancellationToken)
    {
        await _refreshLock.WaitAsync(cancellationToken);
        try
        {
            var refreshToken = await _tokenStorage.GetRefreshTokenAsync();
            if (string.IsNullOrWhiteSpace(refreshToken))
            {
                return false;
            }

            // Use an un-intercepted HttpClient to avoid handler recursion
            using var rawClient = new HttpClient
            {
                BaseAddress = new Uri(ApiConstants.BaseUrl),
                Timeout = TimeSpan.FromSeconds(15)
            };

            var refreshPayload = new RefreshTokenRequest { RefreshToken = refreshToken };
            var response = await rawClient.PostAsJsonAsync(ApiConstants.AuthRefreshToken, refreshPayload, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                return false;
            }

            var jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var result = await response.Content.ReadFromJsonAsync<ApiResponse<AuthResponse>>(jsonOptions, cancellationToken);

            if (result != null && result.Success && result.Data != null)
            {
                await _tokenStorage.UpdateTokensAsync(result.Data.Token, result.Data.RefreshToken);
                return true;
            }

            return false;
        }
        catch
        {
            return false;
        }
        finally
        {
            _refreshLock.Release();
        }
    }

    private static async Task<HttpRequestMessage> CloneHttpRequestMessageAsync(HttpRequestMessage req)
    {
        var clone = new HttpRequestMessage(req.Method, req.RequestUri)
        {
            Version = req.Version
        };

        if (req.Content != null)
        {
            var ms = new MemoryStream();
            await req.Content.CopyToAsync(ms);
            ms.Position = 0;
            clone.Content = new StreamContent(ms);

            if (req.Content.Headers != null)
            {
                foreach (var h in req.Content.Headers)
                {
                    clone.Content.Headers.TryAddWithoutValidation(h.Key, h.Value);
                }
            }
        }

        foreach (var header in req.Headers)
        {
            clone.Headers.TryAddWithoutValidation(header.Key, header.Value);
        }

        return clone;
    }
}

using CampusBite.Services.Interfaces;
using CampusBite.Shared.Responses;
using System.Net.Http.Json;
using System.Text.Json;

namespace CampusBite.Services.Implementations;

public class ApiClient : IApiClient
{
    private readonly HttpClient _httpClient;
    private readonly JsonSerializerOptions _jsonOptions;

    public ApiClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };
    }

    public async Task<ApiResponse<T>?> GetAsync<T>(string endpoint)
    {
        try
        {
            var response = await _httpClient.GetAsync(endpoint);
            return await response.Content.ReadFromJsonAsync<ApiResponse<T>>(_jsonOptions);
        }
        catch (HttpRequestException ex)
        {
            return ApiResponse<T>.FailureResult($"Network connection error: {ex.Message}");
        }
        catch (Exception ex)
        {
            return ApiResponse<T>.FailureResult($"An unexpected error occurred: {ex.Message}");
        }
    }

    public async Task<ApiResponse<TResponse>?> PostAsync<TRequest, TResponse>(string endpoint, TRequest request)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync(endpoint, request, _jsonOptions);
            return await response.Content.ReadFromJsonAsync<ApiResponse<TResponse>>(_jsonOptions);
        }
        catch (HttpRequestException ex)
        {
            return ApiResponse<TResponse>.FailureResult($"Network connection error: {ex.Message}");
        }
        catch (Exception ex)
        {
            return ApiResponse<TResponse>.FailureResult($"An unexpected error occurred: {ex.Message}");
        }
    }

    public async Task<ApiResponse<TResponse>?> PatchAsync<TRequest, TResponse>(string endpoint, TRequest request)
    {
        try
        {
            var response = await _httpClient.PatchAsJsonAsync(endpoint, request, _jsonOptions);
            return await response.Content.ReadFromJsonAsync<ApiResponse<TResponse>>(_jsonOptions);
        }
        catch (HttpRequestException ex)
        {
            return ApiResponse<TResponse>.FailureResult($"Network connection error: {ex.Message}");
        }
        catch (Exception ex)
        {
            return ApiResponse<TResponse>.FailureResult($"An unexpected error occurred: {ex.Message}");
        }
    }

    public async Task<ApiResponse<TResponse>?> DeleteAsync<TResponse>(string endpoint)
    {
        try
        {
            var response = await _httpClient.DeleteAsync(endpoint);
            return await response.Content.ReadFromJsonAsync<ApiResponse<TResponse>>(_jsonOptions);
        }
        catch (HttpRequestException ex)
        {
            return ApiResponse<TResponse>.FailureResult($"Network connection error: {ex.Message}");
        }
        catch (Exception ex)
        {
            return ApiResponse<TResponse>.FailureResult($"An unexpected error occurred: {ex.Message}");
        }
    }
}

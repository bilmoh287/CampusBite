using CampusBite.Shared.Responses;

namespace CampusBite.Services.Interfaces;

public interface IApiClient
{
    Task<ApiResponse<T>?> GetAsync<T>(string endpoint);
    Task<ApiResponse<TResponse>?> PostAsync<TRequest, TResponse>(string endpoint, TRequest request);
    Task<ApiResponse<TResponse>?> PatchAsync<TRequest, TResponse>(string endpoint, TRequest request);
    Task<ApiResponse<TResponse>?> DeleteAsync<TResponse>(string endpoint);
}

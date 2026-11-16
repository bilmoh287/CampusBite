using CampusBite.Services.Interfaces;
using CampusBite.Services.Network;
using CampusBite.Shared.DTOs;
using CampusBite.Shared.Responses;

namespace CampusBite.Services.Implementations;

public class BuddyService : IBuddyService
{
    private readonly IApiClient _apiClient;

    public BuddyService(IApiClient apiClient)
    {
        _apiClient = apiClient;
    }

    public async Task<ApiResponse<List<BuddyDto>>?> GetBuddiesAsync()
    {
        return await _apiClient.GetAsync<List<BuddyDto>>(ApiConstants.Buddies);
    }

    public async Task<ApiResponse<List<UserSearchResultDto>>?> SearchStudentsAsync(string query, int? cafeId = null)
    {
        var endpoint = $"{ApiConstants.Users}/search?term={Uri.EscapeDataString(query)}";
        if (cafeId.HasValue)
        {
            endpoint += $"&cafeId={cafeId.Value}";
        }

        return await _apiClient.GetAsync<List<UserSearchResultDto>>(endpoint);
    }

    public async Task<ApiResponse<BuddyDto>?> AddBuddyAsync(string username)
    {
        var cleanUsername = username.Trim().TrimStart('@');
        return await _apiClient.PostAsync<object, BuddyDto>($"{ApiConstants.Buddies}/{Uri.EscapeDataString(cleanUsername)}", new { });
    }

    public async Task<ApiResponse<bool>?> RemoveBuddyAsync(int buddyId)
    {
        return await _apiClient.DeleteAsync<bool>($"{ApiConstants.Buddies}/{buddyId}");
    }
}

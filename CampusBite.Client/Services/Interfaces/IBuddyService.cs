using CampusBite.Shared.DTOs;
using CampusBite.Shared.Responses;

namespace CampusBite.Services.Interfaces;

public interface IBuddyService
{
    Task<ApiResponse<List<BuddyDto>>?> GetBuddiesAsync();
    Task<ApiResponse<List<UserSearchResultDto>>?> SearchStudentsAsync(string query, int? cafeId = null);
    Task<ApiResponse<BuddyDto>?> AddBuddyAsync(string username);
    Task<ApiResponse<bool>?> RemoveBuddyAsync(int buddyId);
}

using CampusBite.Shared.DTOs;
using CampusBite.Shared.Responses;

namespace CampusBite.Api.Services.Interfaces;

public interface IBuddyService
{
    Task<ApiResponse<List<BuddyDto>>> GetBuddiesAsync(int currentUserId);
    Task<ApiResponse<BuddyDto>> AddBuddyAsync(int currentUserId, string username);
    Task<ApiResponse<bool>> RemoveBuddyAsync(int currentUserId, int buddyId);
}

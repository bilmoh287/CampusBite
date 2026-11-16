using CampusBite.Shared.DTOs;
using CampusBite.Shared.Responses;

namespace CampusBite.Api.Services.Interfaces;

public interface IUserService
{
    Task<ApiResponse<List<UserSearchResultDto>>> SearchStudentsAsync(string searchTerm, int? currentUserId, int? universityId, int? cafeId);
    Task<ApiResponse<UserDto>> GetUserProfileAsync(int userId);
}

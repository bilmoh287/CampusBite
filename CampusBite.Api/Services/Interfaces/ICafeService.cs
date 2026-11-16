using CampusBite.Api.Domain.Entities;
using CampusBite.Shared.DTOs;
using CampusBite.Shared.Responses;

namespace CampusBite.Api.Services.Interfaces;

public interface ICafeService
{
    Task<ApiResponse<List<CafeDto>>> GetCafesAsync(int? universityId, string? searchTerm);
    Task<ApiResponse<CafeDto>> GetCafeByIdAsync(int id);
    Task<Cafe?> GetCafeEntityByIdAsync(int id);
    Task<ApiResponse<List<CafeDto>>> GetCafesByOwnerAsync(int ownerId);
    Task<ApiResponse<CafeDto>> UpdateAnnouncementAsync(int cafeId, string? announcement, int currentUserId);
    Task<ApiResponse<CafeDto>> ToggleStatusAsync(int cafeId, bool isOpen, int currentUserId);
    Task<ApiResponse<OrderDto>> CounterPunchPassAsync(int cafeId, CampusBite.Shared.Requests.CounterPunchRequest request, int cashierUserId);
    Task<ApiResponse<UserDto>> AddStaffMemberAsync(int cafeId, string staffUsername, int ownerUserId);
    Task<ApiResponse<List<UserDto>>> GetStaffMembersAsync(int cafeId, int ownerUserId);
}

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
}

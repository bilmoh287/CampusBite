using CampusBite.Shared.DTOs;
using CampusBite.Shared.Responses;

namespace CampusBite.Api.Services.Interfaces;

public interface IUniversityService
{
    Task<ApiResponse<List<UniversityDto>>> GetActiveUniversitiesAsync();
}

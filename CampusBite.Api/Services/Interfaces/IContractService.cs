using CampusBite.Shared.DTOs;
using CampusBite.Shared.Requests;
using CampusBite.Shared.Responses;

namespace CampusBite.Api.Services.Interfaces;

public interface IContractService
{
    Task<ApiResponse<List<ContractPlanDto>>> GetPlansByCafeAsync(int cafeId);
    Task<ApiResponse<ContractPlanDto>> CreatePlanAsync(CreateContractPlanRequest request, int currentUserId);
    Task<ApiResponse<StudentContractDto>> SubscribeToPlanAsync(SubscribeToPlanRequest request, int studentId);
    Task<ApiResponse<List<StudentContractDto>>> GetMyContractsAsync(int studentId);
    Task<ApiResponse<List<StudentContractDto>>> GetCafeContractsAsync(int cafeId, int currentUserId);
    Task<ApiResponse<StudentContractDto>> UpdateStudentOverdraftAsync(int contractId, UpdateStudentOverdraftRequest request, int currentUserId);
}


using CampusBite.Shared.DTOs;

namespace CampusBite.Services.Interfaces;

public interface IContractService
{
    Task<List<ContractPlanDto>> GetPlansByCafeAsync(int cafeId);
    Task<StudentContractDto?> SubscribeToPlanAsync(int planId);
    Task<List<StudentContractDto>> GetMyContractsAsync();
    Task<StudentContractDto?> GetActiveContractForCafeAsync(int cafeId);
}


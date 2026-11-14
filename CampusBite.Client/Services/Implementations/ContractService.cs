using CampusBite.Services.Interfaces;
using CampusBite.Services.Network;
using CampusBite.Shared.DTOs;
using CampusBite.Shared.Requests;

namespace CampusBite.Services.Implementations;

public class ContractService : IContractService
{
    private readonly IApiClient _apiClient;

    public ContractService(IApiClient apiClient)
    {
        _apiClient = apiClient;
    }

    public async Task<List<ContractPlanDto>> GetPlansByCafeAsync(int cafeId)
    {
        var response = await _apiClient.GetAsync<List<ContractPlanDto>>($"{ApiConstants.Contracts}/plans/cafe/{cafeId}");
        if (response != null && response.Success && response.Data != null)
        {
            return response.Data;
        }

        return new List<ContractPlanDto>();
    }

    public async Task<StudentContractDto?> SubscribeToPlanAsync(int planId)
    {
        var request = new SubscribeToPlanRequest { ContractPlanId = planId };
        var response = await _apiClient.PostAsync<SubscribeToPlanRequest, StudentContractDto>($"{ApiConstants.Contracts}/subscribe", request);
        if (response != null && response.Success && response.Data != null)
        {
            return response.Data;
        }

        return null;
    }

    public async Task<List<StudentContractDto>> GetMyContractsAsync()
    {
        var response = await _apiClient.GetAsync<List<StudentContractDto>>($"{ApiConstants.Contracts}/my-contracts");
        if (response != null && response.Success && response.Data != null)
        {
            return response.Data;
        }

        return new List<StudentContractDto>();
    }

    public async Task<StudentContractDto?> GetActiveContractForCafeAsync(int cafeId)
    {
        var myContracts = await GetMyContractsAsync();
        return myContracts.FirstOrDefault(c => c.CafeId == cafeId && c.IsActive);
    }
}


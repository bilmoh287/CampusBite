using CampusBite.Api.Services.Interfaces;
using CampusBite.Shared.DTOs;
using CampusBite.Shared.Requests;
using CampusBite.Shared.Responses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CampusBite.Api.Controllers;

public class ContractsController : BaseApiController
{
    private readonly IContractService _contractService;

    public ContractsController(IContractService contractService)
    {
        _contractService = contractService;
    }

    /// <summary>
    /// Public: Browse active meal plans offered by a specific café.
    /// </summary>
    [HttpGet("plans/cafe/{cafeId}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<List<ContractPlanDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPlansByCafe(int cafeId)
    {
        var result = await _contractService.GetPlansByCafeAsync(cafeId);
        return HandleResult(result);
    }

    /// <summary>
    /// Café Owner / Admin: Create a new custom dining pass (15-day, 30-day, bulk rate).
    /// </summary>
    [HttpPost("plans")]
    [Authorize(Roles = "CafeOwner,Admin")]
    [ProducesResponseType(typeof(ApiResponse<ContractPlanDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<ContractPlanDto>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreatePlan([FromBody] CreateContractPlanRequest request)
    {
        if (!CurrentUserId.HasValue) return Unauthorized();

        var result = await _contractService.CreatePlanAsync(request, CurrentUserId.Value);
        return HandleResult(result);
    }

    /// <summary>
    /// Student: Subscribe to / activate a campus dining pass.
    /// </summary>
    [HttpPost("subscribe")]
    [Authorize(Roles = "Student")]
    [ProducesResponseType(typeof(ApiResponse<StudentContractDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<StudentContractDto>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Subscribe([FromBody] SubscribeToPlanRequest request)
    {
        if (!CurrentUserId.HasValue) return Unauthorized();

        var result = await _contractService.SubscribeToPlanAsync(request, CurrentUserId.Value);
        return HandleResult(result);
    }

    /// <summary>
    /// Student: View personal active meal subscription passes, remaining credits, and overdraft status.
    /// </summary>
    [HttpGet("my-contracts")]
    [Authorize(Roles = "Student")]
    [ProducesResponseType(typeof(ApiResponse<List<StudentContractDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMyContracts()
    {
        if (!CurrentUserId.HasValue) return Unauthorized();

        var result = await _contractService.GetMyContractsAsync(CurrentUserId.Value);
        return HandleResult(result);
    }

    /// <summary>
    /// Café Owner / Admin: View all student contract subscribers and see who is in debt / overdraft.
    /// </summary>
    [HttpGet("cafe/{cafeId}")]
    [Authorize(Roles = "CafeOwner,Admin")]
    [ProducesResponseType(typeof(ApiResponse<List<StudentContractDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<List<StudentContractDto>>), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetCafeContracts(int cafeId)
    {
        if (!CurrentUserId.HasValue) return Unauthorized();

        var result = await _contractService.GetCafeContractsAsync(cafeId, CurrentUserId.Value);
        return HandleResult(result);
    }

    /// <summary>
    /// Café Owner / Admin: Grant or adjust a trusted student customer's overdraft debt allowance.
    /// </summary>
    [HttpPatch("{contractId}/overdraft")]
    [Authorize(Roles = "CafeOwner,Admin")]
    [ProducesResponseType(typeof(ApiResponse<StudentContractDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<StudentContractDto>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UpdateOverdraft(int contractId, [FromBody] UpdateStudentOverdraftRequest request)
    {
        if (!CurrentUserId.HasValue) return Unauthorized();

        var result = await _contractService.UpdateStudentOverdraftAsync(contractId, request, CurrentUserId.Value);
        return HandleResult(result);
    }
}

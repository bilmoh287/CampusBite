using CampusBite.Api.Domain.Entities;
using CampusBite.Api.Infrastructure.Data;
using CampusBite.Api.Services.Interfaces;
using CampusBite.Shared.DTOs;
using CampusBite.Shared.Enums;
using CampusBite.Shared.Requests;
using CampusBite.Shared.Responses;
using Microsoft.EntityFrameworkCore;

namespace CampusBite.Api.Services.Implementations;

public class ContractService : IContractService
{
    private readonly CampusBiteDbContext _context;
    private readonly ILogger<ContractService> _logger;

    public ContractService(CampusBiteDbContext context, ILogger<ContractService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<ApiResponse<List<ContractPlanDto>>> GetPlansByCafeAsync(int cafeId)
    {
        var plans = await _context.ContractPlans
            .AsNoTracking()
            .Include(p => p.Cafe)
            .Where(p => p.CafeId == cafeId && p.IsActive)
            .OrderBy(p => p.Price)
            .Select(p => MapToPlanDto(p))
            .ToListAsync();

        return ApiResponse<List<ContractPlanDto>>.SuccessResult(plans);
    }

    public async Task<ApiResponse<ContractPlanDto>> CreatePlanAsync(CreateContractPlanRequest request, int currentUserId)
    {
        var cafe = await _context.Cafes.FirstOrDefaultAsync(c => c.Id == request.CafeId);
        if (cafe == null)
        {
            return ApiResponse<ContractPlanDto>.FailureResult("Café not found.");
        }

        var currentUser = await _context.Users.FindAsync(currentUserId);
        if (currentUser == null || (currentUser.Role != UserRole.Admin && cafe.OwnerId != currentUserId))
        {
            return ApiResponse<ContractPlanDto>.FailureResult("You do not have permission to create dining plans for this café.");
        }

        // Auto-calculate smart default for BaseMealCreditValue if not provided by owner
        decimal baseCreditValue;
        if (request.BaseMealCreditValue.HasValue && request.BaseMealCreditValue.Value > 0)
        {
            baseCreditValue = request.BaseMealCreditValue.Value;
        }
        else
        {
            // Default: Highest available meal price in this cafe, or average price per meal
            var maxMealPrice = await _context.Products
                .Where(p => p.CafeId == request.CafeId && p.IsAvailable)
                .MaxAsync(p => (decimal?)p.Price);

            baseCreditValue = maxMealPrice ?? (request.TotalCredits > 0 ? Math.Round(request.Price / request.TotalCredits, 2) : 130.00m);
        }

        var plan = new ContractPlan
        {
            CafeId = request.CafeId,
            Name = request.Name.Trim(),
            Description = request.Description?.Trim(),
            Price = request.Price,
            TotalCredits = request.TotalCredits,
            BaseMealCreditValue = baseCreditValue,
            ValidityDays = request.ValidityDays,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        await _context.ContractPlans.AddAsync(plan);
        await _context.SaveChangesAsync();

        _logger.LogInformation("New contract plan created: {PlanName} for Cafe ID {CafeId}", plan.Name, plan.CafeId);

        plan.Cafe = cafe;
        return ApiResponse<ContractPlanDto>.SuccessResult(MapToPlanDto(plan), "Dining plan created successfully.");
    }

    public async Task<ApiResponse<StudentContractDto>> SubscribeToPlanAsync(SubscribeToPlanRequest request, int studentId)
    {
        var plan = await _context.ContractPlans
            .Include(p => p.Cafe)
            .FirstOrDefaultAsync(p => p.Id == request.ContractPlanId && p.IsActive);

        if (plan == null)
        {
            return ApiResponse<StudentContractDto>.FailureResult("Dining plan not found or inactive.");
        }

        var student = await _context.Users.FindAsync(studentId);
        if (student == null)
        {
            return ApiResponse<StudentContractDto>.FailureResult("Student account not found.");
        }

        // Check if student already has an active contract with this cafe
        var existingContract = await _context.StudentContracts
            .Include(c => c.ContractPlan)
            .FirstOrDefaultAsync(c => c.StudentId == studentId &&
                                      c.ContractPlan.CafeId == plan.CafeId &&
                                      c.Status == ContractStatus.Active &&
                                      c.EndDate >= DateTime.UtcNow);

        StudentContract contract;

        if (existingContract != null)
        {
            // Top up existing contract: rollover remaining credits and extend end date!
            existingContract.RemainingCredits += plan.TotalCredits;
            existingContract.TotalCredits += plan.TotalCredits;
            existingContract.EndDate = existingContract.EndDate.AddDays(plan.ValidityDays);
            contract = existingContract;
            _logger.LogInformation("Student {StudentId} topped up contract {ContractId} with {Credits} credits", studentId, contract.Id, plan.TotalCredits);
        }
        else
        {
            // Create fresh new contract
            contract = new StudentContract
            {
                StudentId = studentId,
                ContractPlanId = plan.Id,
                TotalCredits = plan.TotalCredits,
                RemainingCredits = plan.TotalCredits,
                AllowOverdraft = false,
                OverdraftLimit = 0,
                Status = ContractStatus.Active,
                StartDate = DateTime.UtcNow,
                EndDate = DateTime.UtcNow.AddDays(plan.ValidityDays),
                CreatedAt = DateTime.UtcNow
            };

            await _context.StudentContracts.AddAsync(contract);
            _logger.LogInformation("Student {StudentId} subscribed to plan {PlanName} at Cafe {CafeName}", studentId, plan.Name, plan.Cafe.Name);
        }

        await _context.SaveChangesAsync();

        contract.Student = student;
        contract.ContractPlan = plan;

        return ApiResponse<StudentContractDto>.SuccessResult(MapToContractDto(contract), "Meal subscription activated successfully! Your credits are ready to use.");
    }

    public async Task<ApiResponse<List<StudentContractDto>>> GetMyContractsAsync(int studentId)
    {
        var contracts = await _context.StudentContracts
            .AsNoTracking()
            .Include(c => c.Student)
            .Include(c => c.ContractPlan)
                .ThenInclude(p => p.Cafe)
            .Where(c => c.StudentId == studentId)
            .OrderByDescending(c => c.Status == ContractStatus.Active)
            .ThenByDescending(c => c.EndDate)
            .Select(c => MapToContractDto(c))
            .ToListAsync();

        return ApiResponse<List<StudentContractDto>>.SuccessResult(contracts);
    }

    public async Task<ApiResponse<List<StudentContractDto>>> GetCafeContractsAsync(int cafeId, int currentUserId)
    {
        var cafe = await _context.Cafes.FirstOrDefaultAsync(c => c.Id == cafeId);
        if (cafe == null)
        {
            return ApiResponse<List<StudentContractDto>>.FailureResult("Café not found.");
        }

        var currentUser = await _context.Users.FindAsync(currentUserId);
        if (currentUser == null || (currentUser.Role != UserRole.Admin && cafe.OwnerId != currentUserId))
        {
            return ApiResponse<List<StudentContractDto>>.FailureResult("You do not have permission to view customer dining contracts for this café.");
        }

        var contracts = await _context.StudentContracts
            .AsNoTracking()
            .Include(c => c.Student)
            .Include(c => c.ContractPlan)
                .ThenInclude(p => p.Cafe)
            .Where(c => c.ContractPlan.CafeId == cafeId)
            .OrderByDescending(c => c.RemainingCredits < 0) // Debt accounts at top for manager review!
            .ThenByDescending(c => c.CreatedAt)
            .Select(c => MapToContractDto(c))
            .ToListAsync();

        return ApiResponse<List<StudentContractDto>>.SuccessResult(contracts);
    }

    public async Task<ApiResponse<StudentContractDto>> UpdateStudentOverdraftAsync(int contractId, UpdateStudentOverdraftRequest request, int currentUserId)
    {
        var contract = await _context.StudentContracts
            .Include(c => c.Student)
            .Include(c => c.ContractPlan)
                .ThenInclude(p => p.Cafe)
            .FirstOrDefaultAsync(c => c.Id == contractId);

        if (contract == null)
        {
            return ApiResponse<StudentContractDto>.FailureResult("Student contract not found.");
        }

        var currentUser = await _context.Users.FindAsync(currentUserId);
        if (currentUser == null || (currentUser.Role != UserRole.Admin && contract.ContractPlan.Cafe.OwnerId != currentUserId))
        {
            return ApiResponse<StudentContractDto>.FailureResult("You do not have permission to adjust customer debt/overdraft settings.");
        }

        contract.AllowOverdraft = request.AllowOverdraft;
        contract.OverdraftLimit = request.OverdraftLimit;

        // If contract was previously marked Depleted, but manager granted overdraft, reactivate it!
        if (contract.Status == ContractStatus.Depleted && contract.AllowOverdraft && contract.OverdraftLimit > 0)
        {
            contract.Status = ContractStatus.Active;
        }

        await _context.SaveChangesAsync();

        _logger.LogInformation("Cafe owner {UserId} updated overdraft for Contract {ContractId}: Allow={Allow}, Limit={Limit}",
            currentUserId, contractId, request.AllowOverdraft, request.OverdraftLimit);

        return ApiResponse<StudentContractDto>.SuccessResult(MapToContractDto(contract), "Student overdraft allowance updated.");
    }

    private static ContractPlanDto MapToPlanDto(ContractPlan plan)
    {
        return new ContractPlanDto
        {
            Id = plan.Id,
            CafeId = plan.CafeId,
            CafeName = plan.Cafe?.Name ?? string.Empty,
            Name = plan.Name,
            Description = plan.Description,
            Price = plan.Price,
            TotalCredits = plan.TotalCredits,
            BaseMealCreditValue = plan.BaseMealCreditValue,
            ValidityDays = plan.ValidityDays,
            IsActive = plan.IsActive
        };
    }

    private static StudentContractDto MapToContractDto(StudentContract contract)
    {
        return new StudentContractDto
        {
            Id = contract.Id,
            StudentId = contract.StudentId,
            StudentName = contract.Student?.FullName ?? string.Empty,
            ContractPlanId = contract.ContractPlanId,
            PlanName = contract.ContractPlan?.Name ?? string.Empty,
            CafeId = contract.ContractPlan?.CafeId ?? 0,
            CafeName = contract.ContractPlan?.Cafe?.Name ?? string.Empty,
            TotalCredits = contract.TotalCredits,
            RemainingCredits = contract.RemainingCredits,
            AllowOverdraft = contract.AllowOverdraft,
            OverdraftLimit = contract.OverdraftLimit,
            Status = contract.Status,
            StartDate = contract.StartDate,
            EndDate = contract.EndDate
        };
    }
}


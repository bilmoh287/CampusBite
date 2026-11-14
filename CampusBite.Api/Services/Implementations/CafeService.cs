using CampusBite.Api.Domain.Entities;
using CampusBite.Api.Hubs;
using CampusBite.Api.Infrastructure.Data;
using CampusBite.Api.Services.Interfaces;
using CampusBite.Shared.DTOs;
using CampusBite.Shared.Responses;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace CampusBite.Api.Services.Implementations;

public class CafeService : ICafeService
{
    private readonly CampusBiteDbContext _context;
    private readonly ILogger<CafeService> _logger;
    private readonly IHubContext<OrderHub> _hubContext;

    public CafeService(CampusBiteDbContext context, ILogger<CafeService> logger, IHubContext<OrderHub> hubContext)
    {
        _context = context;
        _logger = logger;
        _hubContext = hubContext;
    }

    public async Task<ApiResponse<List<CafeDto>>> GetCafesAsync(int? universityId, string? searchTerm)
    {
        var query = _context.Cafes
            .AsNoTracking()
            .Include(c => c.University)
            .Include(c => c.Owner)
            .Where(c => c.IsActive);

        if (universityId.HasValue && universityId.Value > 0)
        {
            query = query.Where(c => c.UniversityId == universityId.Value);
        }

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var search = searchTerm.Trim().ToLower();
            query = query.Where(c => c.Name.ToLower().Contains(search) || c.Location.ToLower().Contains(search));
        }

        var cafes = await query
            .OrderBy(c => c.Name)
            .Select(c => MapToDto(c, false))
            .ToListAsync();

        return ApiResponse<List<CafeDto>>.SuccessResult(cafes);
    }

    public async Task<ApiResponse<CafeDto>> GetCafeByIdAsync(int id)
    {
        var cafe = await _context.Cafes
            .AsNoTracking()
            .Include(c => c.University)
            .Include(c => c.Owner)
            .Include(c => c.Categories.OrderBy(cat => cat.DisplayOrder))
            .Include(c => c.Products.Where(p => p.IsAvailable))
                .ThenInclude(p => p.Category)
            .FirstOrDefaultAsync(c => c.Id == id);

        if (cafe == null)
        {
            return ApiResponse<CafeDto>.FailureResult("Café not found.");
        }

        return ApiResponse<CafeDto>.SuccessResult(MapToDto(cafe, true));
    }

    public async Task<Cafe?> GetCafeEntityByIdAsync(int id)
    {
        return await _context.Cafes
            .Include(c => c.Owner)
            .FirstOrDefaultAsync(c => c.Id == id);
    }

    public async Task<ApiResponse<List<CafeDto>>> GetCafesByOwnerAsync(int ownerId)
    {
        var cafes = await _context.Cafes
            .AsNoTracking()
            .Include(c => c.University)
            .Where(c => c.OwnerId == ownerId)
            .Select(c => MapToDto(c, false))
            .ToListAsync();

        return ApiResponse<List<CafeDto>>.SuccessResult(cafes);
    }

    public async Task<ApiResponse<CafeDto>> UpdateAnnouncementAsync(int cafeId, string? announcement, int currentUserId)
    {
        var cafe = await _context.Cafes.FirstOrDefaultAsync(c => c.Id == cafeId);
        if (cafe == null)
        {
            return ApiResponse<CafeDto>.FailureResult("Café not found.");
        }

        if (cafe.OwnerId != currentUserId)
        {
            return ApiResponse<CafeDto>.FailureResult("You do not have permission to manage this café.");
        }

        cafe.Announcement = string.IsNullOrWhiteSpace(announcement) ? null : announcement.Trim();
        await _context.SaveChangesAsync();

        _logger.LogInformation("Café {CafeId} announcement updated by Owner {OwnerId}: '{Announcement}'", cafeId, currentUserId, cafe.Announcement);

        // SignalR: Broadcast announcement update live to students & monitors viewing this cafe
        await _hubContext.Clients.Group($"cafe_{cafeId}").SendAsync("AnnouncementUpdated", cafeId, cafe.Announcement);

        return ApiResponse<CafeDto>.SuccessResult(MapToDto(cafe, false));
    }

    public async Task<ApiResponse<CafeDto>> ToggleStatusAsync(int cafeId, bool isOpen, int currentUserId)
    {
        var cafe = await _context.Cafes.FirstOrDefaultAsync(c => c.Id == cafeId);
        if (cafe == null)
        {
            return ApiResponse<CafeDto>.FailureResult("Café not found.");
        }

        if (cafe.OwnerId != currentUserId)
        {
            return ApiResponse<CafeDto>.FailureResult("You do not have permission to manage this café.");
        }

        cafe.IsActive = isOpen;
        await _context.SaveChangesAsync();

        _logger.LogInformation("Café {CafeId} active/open status changed to {IsOpen} by Owner {OwnerId}", cafeId, isOpen, currentUserId);

        // SignalR: Broadcast open/close status live to students & monitors viewing this cafe
        await _hubContext.Clients.Group($"cafe_{cafeId}").SendAsync("CafeStatusChanged", cafeId, isOpen);

        return ApiResponse<CafeDto>.SuccessResult(MapToDto(cafe, false));
    }

    private static CafeDto MapToDto(Cafe c, bool includeMenu)
    {
        var dto = new CafeDto
        {
            Id = c.Id,
            Name = c.Name,
            Description = c.Description,
            Location = c.Location,
            LogoUrl = c.LogoUrl,
            DeliveryFee = c.DeliveryFee,
            MinimumOrderAmount = c.MinimumOrderAmount,
            IsActive = c.IsActive,
            Announcement = c.Announcement,
            UniversityId = c.UniversityId,
            UniversityName = c.University?.Name,
            OwnerId = c.OwnerId,
            OwnerName = c.Owner?.FullName
        };

        if (includeMenu)
        {
            dto.Categories = c.Categories.Select(cat => new CategoryDto
            {
                Id = cat.Id,
                CafeId = cat.CafeId,
                Name = cat.Name,
                DisplayOrder = cat.DisplayOrder
            }).ToList();

            dto.Products = c.Products.Select(p => new ProductDto
            {
                Id = p.Id,
                CafeId = p.CafeId,
                CategoryId = p.CategoryId,
                CategoryName = p.Category?.Name ?? string.Empty,
                Name = p.Name,
                Description = p.Description,
                Price = p.Price,
                ImageUrl = p.ImageUrl,
                StockQuantity = p.StockQuantity,
                IsAvailable = p.IsAvailable
            }).ToList();
        }

        return dto;
    }
}

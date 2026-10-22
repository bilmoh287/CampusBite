using CampusBite.Api.Domain.Entities;
using CampusBite.Api.Infrastructure.Data;
using CampusBite.Api.Services.Interfaces;
using CampusBite.Shared.DTOs;
using CampusBite.Shared.Responses;
using Microsoft.EntityFrameworkCore;

namespace CampusBite.Api.Services.Implementations;

public class CafeService : ICafeService
{
    private readonly CampusBiteDbContext _context;
    private readonly ILogger<CafeService> _logger;

    public CafeService(CampusBiteDbContext context, ILogger<CafeService> logger)
    {
        _context = context;
        _logger = logger;
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

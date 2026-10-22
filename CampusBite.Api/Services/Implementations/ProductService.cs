using CampusBite.Api.Domain.Entities;
using CampusBite.Api.Infrastructure.Data;
using CampusBite.Api.Services.Interfaces;
using CampusBite.Shared.DTOs;
using CampusBite.Shared.Requests;
using CampusBite.Shared.Responses;
using Microsoft.EntityFrameworkCore;

namespace CampusBite.Api.Services.Implementations;

public class ProductService : IProductService
{
    private readonly CampusBiteDbContext _context;
    private readonly ILogger<ProductService> _logger;

    public ProductService(CampusBiteDbContext context, ILogger<ProductService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<ApiResponse<List<ProductDto>>> GetProductsByCafeAsync(int cafeId)
    {
        var products = await _context.Products
            .AsNoTracking()
            .Where(p => p.CafeId == cafeId && p.IsAvailable)
            .OrderBy(p => p.Name)
            .Select(p => new ProductDto
            {
                Id = p.Id,
                CafeId = p.CafeId,
                CategoryId = p.CategoryId,
                CategoryName = p.Category.Name,
                Name = p.Name,
                Description = p.Description,
                Price = p.Price,
                ImageUrl = p.ImageUrl,
                StockQuantity = p.StockQuantity,
                IsAvailable = p.IsAvailable
            })
            .ToListAsync();

        return ApiResponse<List<ProductDto>>.SuccessResult(products);
    }

    public async Task<ApiResponse<ProductDto>> GetProductByIdAsync(int id)
    {
        var product = await _context.Products
            .AsNoTracking()
            .Include(p => p.Category)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (product == null)
        {
            return ApiResponse<ProductDto>.FailureResult("Product not found.");
        }

        return ApiResponse<ProductDto>.SuccessResult(MapToDto(product));
    }

    public async Task<Product?> GetProductEntityByIdAsync(int id)
    {
        return await _context.Products
            .Include(p => p.Cafe)
            .FirstOrDefaultAsync(p => p.Id == id);
    }

    public async Task<ApiResponse<ProductDto>> CreateProductAsync(CreateProductRequest request)
    {
        var category = await _context.Categories.FindAsync(request.CategoryId);
        if (category == null || category.CafeId != request.CafeId)
        {
            return ApiResponse<ProductDto>.FailureResult("Invalid category for this café.");
        }

        var product = new Product
        {
            CafeId = request.CafeId,
            CategoryId = request.CategoryId,
            Name = request.Name.Trim(),
            Description = request.Description?.Trim(),
            Price = request.Price,
            ImageUrl = request.ImageUrl?.Trim(),
            StockQuantity = request.StockQuantity,
            IsAvailable = request.IsAvailable,
            CreatedAt = DateTime.UtcNow
        };

        await _context.Products.AddAsync(product);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Product created: {ProductName} (ID: {ProductId}) for Cafe {CafeId}", product.Name, product.Id, product.CafeId);
        return ApiResponse<ProductDto>.SuccessResult(MapToDto(product, category.Name), "Product created successfully.");
    }

    public async Task<ApiResponse<ProductDto>> UpdateProductAsync(int id, UpdateProductRequest request)
    {
        var product = await _context.Products.Include(p => p.Category).FirstOrDefaultAsync(p => p.Id == id);
        if (product == null)
        {
            return ApiResponse<ProductDto>.FailureResult("Product not found.");
        }

        var category = await _context.Categories.FindAsync(request.CategoryId);
        if (category == null || category.CafeId != product.CafeId)
        {
            return ApiResponse<ProductDto>.FailureResult("Invalid category for this café.");
        }

        product.CategoryId = request.CategoryId;
        product.Name = request.Name.Trim();
        product.Description = request.Description?.Trim();
        product.Price = request.Price;
        product.ImageUrl = request.ImageUrl?.Trim();
        product.StockQuantity = request.StockQuantity;
        product.IsAvailable = request.IsAvailable;

        await _context.SaveChangesAsync();
        _logger.LogInformation("Product updated: {ProductName} (ID: {ProductId})", product.Name, product.Id);

        return ApiResponse<ProductDto>.SuccessResult(MapToDto(product, category.Name), "Product updated successfully.");
    }

    public async Task<ApiResponse<bool>> DeleteProductAsync(int id)
    {
        var product = await _context.Products.FindAsync(id);
        if (product == null)
        {
            return ApiResponse<bool>.FailureResult("Product not found.");
        }

        _context.Products.Remove(product);
        await _context.SaveChangesAsync();
        _logger.LogInformation("Product deleted: {ProductName} (ID: {ProductId})", product.Name, id);

        return ApiResponse<bool>.SuccessResult(true, "Product deleted successfully.");
    }

    public async Task<ApiResponse<bool>> UpdateStockAsync(int id, int? newStock)
    {
        var product = await _context.Products.FindAsync(id);
        if (product == null)
        {
            return ApiResponse<bool>.FailureResult("Product not found.");
        }

        product.StockQuantity = newStock;
        await _context.SaveChangesAsync();
        _logger.LogInformation("Stock updated for Product ID {ProductId}: New stock = {Stock}", id, newStock);

        return ApiResponse<bool>.SuccessResult(true, "Stock updated successfully.");
    }

    private static ProductDto MapToDto(Product p, string? categoryName = null)
    {
        return new ProductDto
        {
            Id = p.Id,
            CafeId = p.CafeId,
            CategoryId = p.CategoryId,
            CategoryName = categoryName ?? p.Category?.Name ?? string.Empty,
            Name = p.Name,
            Description = p.Description,
            Price = p.Price,
            ImageUrl = p.ImageUrl,
            StockQuantity = p.StockQuantity,
            IsAvailable = p.IsAvailable
        };
    }
}

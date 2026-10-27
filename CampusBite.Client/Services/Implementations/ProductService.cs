using CampusBite.Models;
using CampusBite.Services.Interfaces;
using CampusBite.Services.Network;
using CampusBite.Shared.DTOs;

namespace CampusBite.Services.Implementations;

public class ProductService : IProductService
{
    private readonly IApiClient _apiClient;

    public ProductService(IApiClient apiClient)
    {
        _apiClient = apiClient;
    }

    public async Task<List<Product>> GetProductsByCafeIdAsync(int cafeId, string? category = null)
    {
        var response = await _apiClient.GetAsync<List<ProductDto>>($"{ApiConstants.Products}/cafe/{cafeId}");
        if (response != null && response.Success && response.Data != null)
        {
            var query = response.Data.AsEnumerable();

            if (!string.IsNullOrWhiteSpace(category) && !category.Equals("All", StringComparison.OrdinalIgnoreCase))
            {
                query = query.Where(p => string.Equals(p.CategoryName, category, StringComparison.OrdinalIgnoreCase));
            }

            return query.Select(MapDtoToModel).ToList();
        }

        return new List<Product>();
    }

    public async Task<List<string>> GetCategoriesByCafeIdAsync(int cafeId)
    {
        var response = await _apiClient.GetAsync<List<ProductDto>>($"{ApiConstants.Products}/cafe/{cafeId}");
        if (response != null && response.Success && response.Data != null)
        {
            var categories = response.Data
                .Select(p => p.CategoryName)
                .Where(c => !string.IsNullOrWhiteSpace(c))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            categories.Insert(0, "All");
            return categories;
        }

        return new List<string> { "All" };
    }

    private static Product MapDtoToModel(ProductDto dto)
    {
        return new Product
        {
            Id = dto.Id,
            CafeId = dto.CafeId,
            Name = dto.Name,
            Description = dto.Description ?? string.Empty,
            Price = dto.Price,
            Category = dto.CategoryName,
            IsAvailable = dto.IsAvailable,
            ImageUrl = dto.ImageUrl ?? string.Empty,
            StockQuantity = dto.StockQuantity
        };
    }
}

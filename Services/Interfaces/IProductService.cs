using CampusBite.Models;

namespace CampusBite.Services.Interfaces;

public interface IProductService
{
    Task<List<Product>> GetProductsByCafeIdAsync(int cafeId, string? category = null);
    Task<List<string>> GetCategoriesByCafeIdAsync(int cafeId);
}

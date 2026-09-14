using CampusBite.Data;
using CampusBite.Models;
using CampusBite.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace CampusBite.Services.Implementations;

public class ProductService : IProductService
{
    private readonly AppDbContext _context;

    public ProductService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<List<Product>> GetProductsByCafeIdAsync(int cafeId, string? category = null)
    {
        var query = _context.Products
            .Where(p => p.CafeId == cafeId)
            .AsNoTracking();

        if (!string.IsNullOrWhiteSpace(category) && !category.Equals("All", StringComparison.OrdinalIgnoreCase))
        {
            query = query.Where(p => p.Category == category);
        }

        return await query.ToListAsync();
    }

    public async Task<List<string>> GetCategoriesByCafeIdAsync(int cafeId)
    {
        var categories = await _context.Products
            .Where(p => p.CafeId == cafeId)
            .Select(p => p.Category)
            .Distinct()
            .AsNoTracking()
            .ToListAsync();

        categories.Insert(0, "All");
        return categories;
    }
}

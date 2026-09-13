using CampusBite.Data;
using CampusBite.Models;
using CampusBite.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace CampusBite.Services.Implementations;

public class CafeService : ICafeService
{
    private readonly AppDbContext _context;

    public CafeService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<List<Cafe>> GetCafesAsync()
    {
        return await _context.Cafes
            .Include(c => c.Products)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<Cafe?> GetCafeByIdAsync(int id)
    {
        return await _context.Cafes
            .Include(c => c.Products)
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == id);
    }
}

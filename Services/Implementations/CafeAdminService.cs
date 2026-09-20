using Microsoft.EntityFrameworkCore;

namespace CampusBite.Services.Implementations;

public class CafeAdminService : ICafeAdminService
{
    private readonly AppDbContext _dbContext;

    public CafeAdminService(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<List<Cafe>> GetCafesAsync()
    {
        return await _dbContext.Cafes
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<List<Order>> GetCafeOrdersAsync(int cafeId, string? statusFilter = null)
    {
        var query = _dbContext.Orders
            .Where(o => o.CafeId == cafeId)
            .Include(o => o.OrderItems)
            .ThenInclude(oi => oi.Product)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(statusFilter) && statusFilter != "All")
        {
            query = query.Where(o => o.Status.ToLower() == statusFilter.ToLower());
        }

        return await query
            .OrderByDescending(o => o.OrderDate)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<bool> UpdateOrderStatusAsync(int orderId, string newStatus)
    {
        var order = await _dbContext.Orders.FirstOrDefaultAsync(o => o.Id == orderId);
        if (order == null) return false;

        order.Status = newStatus;
        await _dbContext.SaveChangesAsync();
        return true;
    }

    public async Task<bool> ToggleProductAvailabilityAsync(int productId, bool isAvailable)
    {
        var product = await _dbContext.Products.FirstOrDefaultAsync(p => p.Id == productId);
        if (product == null) return false;

        product.IsAvailable = isAvailable;
        await _dbContext.SaveChangesAsync();
        return true;
    }
}

using Microsoft.EntityFrameworkCore;
using CampusBite.Data;
using CampusBite.Models;
using CampusBite.Services.Interfaces;

namespace CampusBite.Services.Implementations;

public class DeliveryService : IDeliveryService
{
    private readonly AppDbContext _dbContext;

    public DeliveryService(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<List<Order>> GetDeliveryOrdersAsync(string? statusFilter = null)
    {
        var query = _dbContext.Orders
            .Include(o => o.Cafe)
            .Include(o => o.OrderItems)
            .ThenInclude(oi => oi.Product)
            .Where(o => o.OrderType == "Delivery");

        if (!string.IsNullOrWhiteSpace(statusFilter) && !string.Equals(statusFilter, "All", StringComparison.OrdinalIgnoreCase))
        {
            query = query.Where(o => o.Status == statusFilter);
        }

        return await query
            .OrderByDescending(o => o.OrderDate)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<bool> UpdateDeliveryStatusAsync(int orderId, string newStatus)
    {
        var order = await _dbContext.Orders.FirstOrDefaultAsync(o => o.Id == orderId);
        if (order == null) return false;

        order.Status = newStatus;
        await _dbContext.SaveChangesAsync();
        return true;
    }

    public async Task<List<Cafe>> GetCafesOverviewAsync()
    {
        return await _dbContext.Cafes
            .AsNoTracking()
            .OrderBy(c => c.Name)
            .ToListAsync();
    }
}

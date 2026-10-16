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

    public async Task<List<Product>> GetCafeProductsAsync(int cafeId)
    {
        return await _dbContext.Products
            .Where(p => p.CafeId == cafeId)
            .OrderBy(p => p.Category)
            .ThenBy(p => p.Name)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<bool> ToggleProductAvailabilityAsync(int productId, bool isAvailable)
    {
        var product = await _dbContext.Products.FirstOrDefaultAsync(p => p.Id == productId);
        if (product == null) return false;

        product.IsAvailable = isAvailable;
        await _dbContext.SaveChangesAsync();
        return true;
    }

    public async Task<bool> UpdateProductPriceAsync(int productId, decimal newPrice)
    {
        var product = await _dbContext.Products.FirstOrDefaultAsync(p => p.Id == productId);
        if (product == null) return false;

        product.Price = newPrice;
        await _dbContext.SaveChangesAsync();
        return true;
    }

    public async Task<bool> UpdateProductStockAsync(int productId, int? stockQuantity)
    {
        var product = await _dbContext.Products.FirstOrDefaultAsync(p => p.Id == productId);
        if (product == null) return false;

        product.StockQuantity = stockQuantity;
        if (stockQuantity.HasValue)
        {
            if (stockQuantity.Value == 0)
                product.IsAvailable = false;
            else if (stockQuantity.Value > 0 && !product.IsAvailable)
                product.IsAvailable = true;
        }

        await _dbContext.SaveChangesAsync();
        return true;
    }

    public async Task<bool> ToggleCafeStatusAsync(int cafeId, bool isOpen)
    {
        var cafe = await _dbContext.Cafes.FirstOrDefaultAsync(c => c.Id == cafeId);
        if (cafe == null) return false;

        cafe.IsOpen = isOpen;
        await _dbContext.SaveChangesAsync();
        return true;
    }

    public async Task<bool> UpdateCafeAnnouncementAsync(int cafeId, string? announcement)
    {
        var cafe = await _dbContext.Cafes.FirstOrDefaultAsync(c => c.Id == cafeId);
        if (cafe == null) return false;

        cafe.Announcement = string.IsNullOrWhiteSpace(announcement) ? null : announcement.Trim();
        await _dbContext.SaveChangesAsync();
        return true;
    }

    public async Task<List<Expense>> GetCafeExpensesAsync(int cafeId)
    {
        return await _dbContext.Expenses
            .Where(e => e.CafeId == cafeId)
            .OrderByDescending(e => e.ExpenseDate)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<bool> AddExpenseAsync(Expense expense)
    {
        if (expense == null) return false;
        await _dbContext.Expenses.AddAsync(expense);
        await _dbContext.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteExpenseAsync(int expenseId)
    {
        var expense = await _dbContext.Expenses.FirstOrDefaultAsync(e => e.Id == expenseId);
        if (expense == null) return false;

        _dbContext.Expenses.Remove(expense);
        await _dbContext.SaveChangesAsync();
        return true;
    }
}

using CampusBite.Models;
using CampusBite.Shared.DTOs;
using CampusBite.Shared.Requests;
using CampusBite.Shared.Responses;

namespace CampusBite.Services.Interfaces;

public interface ICafeAdminService
{
    Task<List<Cafe>> GetCafesAsync();
    Task<List<Order>> GetCafeOrdersAsync(int cafeId, string? statusFilter = null);
    Task<bool> UpdateOrderStatusAsync(int orderId, string newStatus);
    Task<List<Product>> GetCafeProductsAsync(int cafeId);
    Task<bool> ToggleProductAvailabilityAsync(int productId, bool isAvailable);
    Task<bool> UpdateProductPriceAsync(int productId, decimal newPrice);
    Task<bool> UpdateProductStockAsync(int productId, int? stockQuantity);
    Task<bool> ToggleCafeStatusAsync(int cafeId, bool isOpen);
    Task<bool> UpdateCafeAnnouncementAsync(int cafeId, string? announcement);
    Task<bool> UpdatePackagingFeeAsync(int cafeId, decimal newFee);
    Task<List<Expense>> GetCafeExpensesAsync(int cafeId);
    Task<bool> AddExpenseAsync(Expense expense);
    Task<bool> DeleteExpenseAsync(int expenseId);
    Task<bool> AddProductAsync(Product product);
    Task<bool> DeleteProductAsync(int productId);

    // Counter Punch & Staff Operations
    Task<ApiResponse<OrderDto>?> CounterPunchPassAsync(int cafeId, CounterPunchRequest request);
    Task<ApiResponse<UserDto>?> AddStaffMemberAsync(int cafeId, string staffUsername);
    Task<ApiResponse<List<UserDto>>?> GetStaffMembersAsync(int cafeId);
}

namespace CampusBite.Services.Interfaces;

public interface ICafeAdminService
{
    Task<List<Cafe>> GetCafesAsync();
    Task<List<Order>> GetCafeOrdersAsync(int cafeId, string? statusFilter = null);
    Task<bool> UpdateOrderStatusAsync(int orderId, string newStatus);
    Task<bool> ToggleProductAvailabilityAsync(int productId, bool isAvailable);
}

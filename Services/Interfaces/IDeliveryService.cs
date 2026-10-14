namespace CampusBite.Services.Interfaces;

public interface IDeliveryService
{
    Task<List<Order>> GetDeliveryOrdersAsync(string? statusFilter = null);
    Task<bool> UpdateDeliveryStatusAsync(int orderId, string newStatus);
    Task<List<Cafe>> GetCafesOverviewAsync();
}

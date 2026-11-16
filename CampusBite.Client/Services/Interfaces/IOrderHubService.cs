using CampusBite.Shared.DTOs;

namespace CampusBite.Services.Interfaces;

public interface IOrderHubService
{
    event Action<OrderDto>? OrderStatusChanged;
    event Action<OrderDto>? OrderCreated;
    event Action<OrderDto>? DeliveryAvailable;
    event Action<OrderDto>? OrderUpdated;
    event Action<int, string?>? AnnouncementUpdated;
    event Action<int, bool>? CafeStatusChanged;
    event Action<PassDeductionAlertDto>? PassDeductionAlertReceived;

    Task EnsureConnectedAsync();
    Task DisconnectAsync();

    Task JoinOrderGroupAsync(int orderId);
    Task LeaveOrderGroupAsync(int orderId);

    Task JoinCafeGroupAsync(int cafeId);
    Task LeaveCafeGroupAsync(int cafeId);

    Task JoinCouriersGroupAsync();
    Task LeaveCouriersGroupAsync();

    bool IsConnected { get; }
}

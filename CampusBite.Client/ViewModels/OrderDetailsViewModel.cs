using CampusBite.Models;
using CampusBite.Services.Interfaces;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace CampusBite.ViewModels;

[QueryProperty(nameof(Order), "Order")]
[QueryProperty(nameof(OrderId), "OrderId")]
public partial class OrderDetailsViewModel : ObservableObject
{
    private readonly IOrderService _orderService;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ItemCount))]
    [NotifyPropertyChangedFor(nameof(FormattedDate))]
    [NotifyPropertyChangedFor(nameof(StepIndex))]
    [NotifyPropertyChangedFor(nameof(IsStep1Done))]
    [NotifyPropertyChangedFor(nameof(IsStep2Done))]
    [NotifyPropertyChangedFor(nameof(IsStep3Done))]
    [NotifyPropertyChangedFor(nameof(IsStep4Done))]
    [NotifyPropertyChangedFor(nameof(IsStep5Done))]
    [NotifyPropertyChangedFor(nameof(Step1Color))]
    [NotifyPropertyChangedFor(nameof(Step2Color))]
    [NotifyPropertyChangedFor(nameof(Step3Color))]
    [NotifyPropertyChangedFor(nameof(Step4Color))]
    [NotifyPropertyChangedFor(nameof(Step5Color))]
    [NotifyPropertyChangedFor(nameof(Line1Color))]
    [NotifyPropertyChangedFor(nameof(Line2Color))]
    [NotifyPropertyChangedFor(nameof(Line3Color))]
    [NotifyPropertyChangedFor(nameof(Line4Color))]
    [NotifyPropertyChangedFor(nameof(IsCancelled))]
    [NotifyPropertyChangedFor(nameof(IsNotCancelled))]
    [NotifyPropertyChangedFor(nameof(StatusHeadline))]
    [NotifyPropertyChangedFor(nameof(StatusSubtitle))]
    [NotifyPropertyChangedFor(nameof(StatusBadgeColor))]
    [NotifyPropertyChangedFor(nameof(StatusBadgeText))]
    [NotifyPropertyChangedFor(nameof(CanCancel))]
    [NotifyPropertyChangedFor(nameof(IsDelivery))]
    [NotifyPropertyChangedFor(nameof(Step4Icon))]
    [NotifyPropertyChangedFor(nameof(Step4Title))]
    private Order? _order;

    [ObservableProperty]
    private int _orderId;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsNotRefreshing))]
    private bool _isRefreshing;

    public bool IsNotRefreshing => !IsRefreshing;
    public int ItemCount => Order?.OrderItems?.Sum(i => i.Quantity) ?? 0;
    public string FormattedDate => Order?.OrderDate.ToString("f") ?? string.Empty;
    public bool IsDelivery => Order?.IsDelivery ?? (Order?.DeliveryLocation?.Contains("Pickup", StringComparison.OrdinalIgnoreCase) != true);

    public int StepIndex => Order?.Status?.ToLowerInvariant() switch
    {
        "pending" or "confirmed" => 1,
        "preparing" => 2,
        "readyforpickup" or "ready" => 3,
        "outfordelivery" or "out for delivery" => 4,
        "delivered" or "completed" => 5,
        "cancelled" => -1,
        _ => 1
    };

    public bool IsStep1Done => StepIndex >= 1;
    public bool IsStep2Done => StepIndex >= 2;
    public bool IsStep3Done => StepIndex >= 3;
    public bool IsStep4Done => StepIndex >= 4;
    public bool IsStep5Done => StepIndex >= 5;

    public string Step1Color => IsStep1Done ? "#4CAF50" : "#333333";
    public string Step2Color => IsStep2Done ? "#4CAF50" : "#333333";
    public string Step3Color => IsStep3Done ? "#4CAF50" : "#333333";
    public string Step4Color => IsStep4Done ? "#4CAF50" : "#333333";
    public string Step5Color => IsStep5Done ? "#4CAF50" : "#333333";

    public string Line1Color => IsStep2Done ? "#4CAF50" : "#2A2A2A";
    public string Line2Color => IsStep3Done ? "#4CAF50" : "#2A2A2A";
    public string Line3Color => IsStep4Done ? "#4CAF50" : "#2A2A2A";
    public string Line4Color => IsStep5Done ? "#4CAF50" : "#2A2A2A";

    public bool IsCancelled => StepIndex == -1;
    public bool IsNotCancelled => !IsCancelled;
    public bool CanCancel => StepIndex == 1; // Can only cancel if still in Pending/Confirmed

    public string Step4Icon => IsDelivery ? "🛵" : "🛎️";
    public string Step4Title => IsDelivery ? "Out for Delivery" : "Counter Handover";

    public string StatusBadgeText => Order?.Status?.ToLowerInvariant() switch
    {
        "pending" => "Order Placed 🕒",
        "preparing" => "Kitchen Preparing 🍳",
        "readyforpickup" or "ready" => IsDelivery ? "Ready for Courier 📦" : "Ready for Pickup! 🛎️",
        "outfordelivery" or "out for delivery" => "Out for Delivery 🛵",
        "delivered" or "completed" => "Delivered 🎉",
        "cancelled" => "Cancelled ❌",
        _ => Order?.Status ?? "Processing"
    };

    public string StatusBadgeColor => Order?.Status?.ToLowerInvariant() switch
    {
        "pending" => "#FF9800",
        "preparing" => "#2196F3",
        "readyforpickup" or "ready" => "#9C27B0",
        "outfordelivery" or "out for delivery" => "#00BCD4",
        "delivered" or "completed" => "#4CAF50",
        "cancelled" => "#F44336",
        _ => "#888888"
    };

    public string StatusHeadline => Order?.Status?.ToLowerInvariant() switch
    {
        "pending" => "Order Received by Kitchen",
        "preparing" => "Chef is Cooking Your Meal",
        "readyforpickup" or "ready" => IsDelivery ? "Order Packed & Awaiting Courier" : "Order is Ready at the Counter!",
        "outfordelivery" or "out for delivery" => "Courier is Heading Your Way",
        "delivered" or "completed" => "Order Complete. Enjoy Your Meal!",
        "cancelled" => "This order was cancelled",
        _ => "Tracking Your Order"
    };

    public string StatusSubtitle => Order?.Status?.ToLowerInvariant() switch
    {
        "pending" => "Your ticket has been sent to the kitchen display.",
        "preparing" => "Fresh ingredients are being prepared right now.",
        "readyforpickup" or "ready" => IsDelivery ? "A campus courier will claim and dispatch shortly." : "Please show your phone ticket at the counter.",
        "outfordelivery" or "out for delivery" => $"Drop-off destination: {Order?.DeliveryLocation ?? "Campus"}",
        "delivered" or "completed" => "Thank you for dining with CampusBite!",
        "cancelled" => "No charges were finalized or credits have been restored.",
        _ => "Updates appear as kitchen staff advances your ticket."
    };

    public OrderDetailsViewModel(IOrderService orderService)
    {
        _orderService = orderService;
    }

    partial void OnOrderIdChanged(int value)
    {
        if (value > 0)
        {
            _ = LoadOrderByIdAsync(value);
        }
    }

    [RelayCommand]
    public async Task RefreshOrderAsync()
    {
        int targetId = Order?.Id ?? OrderId;
        if (targetId <= 0) return;

        try
        {
            IsRefreshing = true;
            await LoadOrderByIdAsync(targetId);
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlertAsync("Refresh Error", $"Could not update status: {ex.Message}", "OK");
        }
        finally
        {
            IsRefreshing = false;
        }
    }

    [RelayCommand]
    public async Task CancelOrderAsync()
    {
        if (Order == null || !CanCancel) return;

        bool confirm = await Shell.Current.DisplayAlertAsync(
            "Cancel Order?",
            "Are you sure you want to cancel this order? Food preparation has not started yet.",
            "Yes, Cancel",
            "Keep Order");

        if (!confirm) return;

        try
        {
            IsRefreshing = true;
            bool success = await _orderService.CancelOrderAsync(Order.Id);
            if (success)
            {
                await Shell.Current.DisplayAlertAsync("Order Cancelled", "Your order has been cancelled and stock has been restored.", "OK");
                await LoadOrderByIdAsync(Order.Id);
            }
            else
            {
                await Shell.Current.DisplayAlertAsync("Error", "Unable to cancel order. The kitchen may have already started cooking.", "OK");
            }
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlertAsync("Error", $"Cancellation failed: {ex.Message}", "OK");
        }
        finally
        {
            IsRefreshing = false;
        }
    }

    private async Task LoadOrderByIdAsync(int id)
    {
        var updated = await _orderService.GetOrderByIdAsync(id);
        if (updated != null)
        {
            Order = updated;
        }
    }
}

using CampusBite.Models;
using CampusBite.Services.Interfaces;
using CampusBite.Services.Network;
using CampusBite.Shared.DTOs;
using CampusBite.Shared.Enums;
using CampusBite.Shared.Requests;

namespace CampusBite.Services.Implementations;

public class DeliveryService : IDeliveryService
{
    private readonly IApiClient _apiClient;

    public DeliveryService(IApiClient apiClient)
    {
        _apiClient = apiClient;
    }

    public async Task<List<Order>> GetDeliveryOrdersAsync(string? statusFilter = null)
    {
        // Fetch live orders available for delivery from central API
        var response = await _apiClient.GetAsync<List<OrderDto>>($"{ApiConstants.Orders}/available-for-delivery");
        if (response == null || !response.Success || response.Data == null)
        {
            return new List<Order>();
        }

        var orders = response.Data.Select(MapDtoToModel).ToList();

        if (!string.IsNullOrWhiteSpace(statusFilter) && !string.Equals(statusFilter, "All", StringComparison.OrdinalIgnoreCase))
        {
            orders = orders.Where(o => string.Equals(o.Status, statusFilter, StringComparison.OrdinalIgnoreCase)).ToList();
        }

        return orders;
    }

    public async Task<bool> UpdateDeliveryStatusAsync(int orderId, string newStatus)
    {
        // When courier picks up order: call POST /api/orders/{id}/claim
        if (string.Equals(newStatus, "Out for Delivery", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(newStatus, "OutForDelivery", StringComparison.OrdinalIgnoreCase))
        {
            var claimResponse = await _apiClient.PostAsync<object, OrderDto>($"{ApiConstants.Orders}/{orderId}/claim", new { });
            return claimResponse != null && claimResponse.Success;
        }

        // When courier drops off food: call PATCH /api/orders/{id}/status { newStatus: Delivered }
        var targetStatus = string.Equals(newStatus, "Delivered", StringComparison.OrdinalIgnoreCase)
            ? OrderStatus.Delivered
            : OrderStatus.OutForDelivery;

        var request = new UpdateOrderStatusRequest { NewStatus = targetStatus };
        var statusResponse = await _apiClient.PatchAsync<UpdateOrderStatusRequest, OrderDto>($"{ApiConstants.Orders}/{orderId}/status", request);
        return statusResponse != null && statusResponse.Success;
    }

    public async Task<List<Cafe>> GetCafesOverviewAsync()
    {
        var response = await _apiClient.GetAsync<List<CafeDto>>(ApiConstants.Cafes);
        if (response == null || !response.Success || response.Data == null)
        {
            return new List<Cafe>();
        }

        return response.Data.Select(dto => new Cafe
        {
            Id = dto.Id,
            Name = dto.Name,
            Description = dto.Description ?? string.Empty,
            Location = dto.Location,
            IsOpen = dto.IsActive,
            ImageUrl = dto.LogoUrl ?? string.Empty
        }).ToList();
    }

    private static Order MapDtoToModel(OrderDto dto)
    {
        var uiStatus = dto.Status switch
        {
            OrderStatus.ReadyForPickup => "Ready",
            OrderStatus.OutForDelivery => "Out for Delivery",
            OrderStatus.Delivered => "Delivered",
            _ => dto.Status.ToString()
        };

        var order = new Order
        {
            Id = dto.Id,
            CafeId = dto.CafeId,
            OrderDate = dto.CreatedAt.ToLocalTime(),
            TotalAmount = dto.TotalAmount,
            Status = uiStatus,
            OrderType = "Delivery",
            DeliveryLocation = dto.DeliveryLocation,
            Notes = dto.SpecialNotes,
            Cafe = new Cafe
            {
                Id = dto.CafeId,
                Name = dto.CafeName
            }
        };

        if (dto.Items != null)
        {
            foreach (var item in dto.Items)
            {
                order.OrderItems.Add(new OrderItem
                {
                    Id = item.Id,
                    OrderId = dto.Id,
                    ProductId = item.ProductId ?? 0,
                    Quantity = item.Quantity,
                    UnitPrice = item.UnitPrice,
                    Product = new Product
                    {
                        Id = item.ProductId ?? 0,
                        Name = item.ProductName,
                        Price = item.UnitPrice
                    }
                });
            }
        }

        return order;
    }
}

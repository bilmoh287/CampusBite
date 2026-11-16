using CampusBite.Models;
using CampusBite.Models.UI;
using CampusBite.Services.Interfaces;
using CampusBite.Services.Network;
using CampusBite.Shared.DTOs;
using CampusBite.Shared.Enums;
using CampusBite.Shared.Requests;

namespace CampusBite.Services.Implementations;

public class OrderService : IOrderService
{
    private readonly IApiClient _apiClient;

    public OrderService(IApiClient apiClient)
    {
        _apiClient = apiClient;
    }

    public async Task<Order> CreateOrderAsync(
        int cafeId, 
        string orderType, 
        string? deliveryLocation, 
        string? notes, 
        IEnumerable<CartItem> cartItems, 
        PaymentMethod paymentMethod = PaymentMethod.Cash,
        IEnumerable<string>? coDinerUsernames = null)
    {
        var itemsList = cartItems.ToList();
        if (!itemsList.Any())
        {
            throw new InvalidOperationException("Cannot create an order from an empty cart.");
        }

        var location = !string.IsNullOrWhiteSpace(deliveryLocation) 
            ? deliveryLocation.Trim() 
            : (string.Equals(orderType, "Delivery", StringComparison.OrdinalIgnoreCase) ? "Campus Dorm" : "Pickup Counter");

        var request = new CreateOrderRequest
        {
            CafeId = cafeId,
            DeliveryLocation = location,
            PaymentMethod = paymentMethod,
            SpecialNotes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim(),
            CoDinerUsernames = coDinerUsernames?.ToList() ?? new List<string>(),
            Items = itemsList.Select(c => new CreateOrderItemRequest
            {
                ProductId = c.Product.Id,
                Quantity = c.Quantity
            }).ToList()
        };

        var response = await _apiClient.PostAsync<CreateOrderRequest, OrderDto>(ApiConstants.Orders, request);

        if (response != null && response.Success && response.Data != null)
        {
            return MapDtoToModel(response.Data, orderType);
        }

        var errorMessage = response?.Message ?? "Failed to place order with backend API.";
        throw new InvalidOperationException(errorMessage);
    }

    public async Task<List<Order>> GetOrdersAsync()
    {
        var response = await _apiClient.GetAsync<List<OrderDto>>($"{ApiConstants.Orders}/my-orders");
        if (response != null && response.Success && response.Data != null)
        {
            return response.Data.Select(dto => MapDtoToModel(dto, null)).ToList();
        }

        return new List<Order>();
    }

    public async Task<Order?> GetOrderByIdAsync(int orderId)
    {
        var response = await _apiClient.GetAsync<OrderDto>($"{ApiConstants.Orders}/{orderId}");
        if (response != null && response.Success && response.Data != null)
        {
            return MapDtoToModel(response.Data, null);
        }

        return null;
    }

    public async Task<bool> CancelOrderAsync(int orderId)
    {
        var request = new UpdateOrderStatusRequest { NewStatus = OrderStatus.Cancelled };
        var response = await _apiClient.PatchAsync<UpdateOrderStatusRequest, OrderDto>($"{ApiConstants.Orders}/{orderId}/status", request);
        return response != null && response.Success;
    }

    private static Order MapDtoToModel(OrderDto dto, string? orderTypeFallback)
    {
        var order = new Order
        {
            Id = dto.Id,
            CafeId = dto.CafeId,
            OrderDate = dto.CreatedAt.ToLocalTime(),
            TotalAmount = dto.TotalAmount,
            Status = dto.Status.ToString(),
            OrderType = orderTypeFallback ?? (dto.DeliveryFee > 0 ? "Delivery" : "Pickup"),
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

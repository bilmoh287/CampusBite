using CampusBite.Api.Domain.Entities;
using CampusBite.Shared.DTOs;
using CampusBite.Shared.Enums;
using CampusBite.Shared.Requests;
using CampusBite.Shared.Responses;

namespace CampusBite.Api.Services.Interfaces;

public interface IOrderService
{
    Task<ApiResponse<OrderDto>> CreateOrderAsync(int customerId, CreateOrderRequest request);
    Task<ApiResponse<List<OrderDto>>> GetOrdersForCustomerAsync(int customerId);
    Task<ApiResponse<List<OrderDto>>> GetOrdersForCafeAsync(int cafeId);
    Task<ApiResponse<List<OrderDto>>> GetAvailableOrdersForCourierAsync();
    Task<ApiResponse<OrderDto>> GetOrderByIdAsync(int id);
    Task<Order?> GetOrderEntityByIdAsync(int id);
    Task<ApiResponse<OrderDto>> UpdateOrderStatusAsync(int orderId, OrderStatus newStatus, int currentUserId);
    Task<ApiResponse<OrderDto>> ClaimOrderAsync(int orderId, int courierId);
}

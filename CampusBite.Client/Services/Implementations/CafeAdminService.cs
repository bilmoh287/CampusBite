using CampusBite.Models;
using CampusBite.Services.Interfaces;
using CampusBite.Services.Network;
using CampusBite.Shared.DTOs;
using CampusBite.Shared.Enums;
using CampusBite.Shared.Requests;

namespace CampusBite.Services.Implementations;

public class CafeAdminService : ICafeAdminService
{
    private readonly IApiClient _apiClient;
    private static readonly List<Expense> _localExpenses = new();

    public CafeAdminService(IApiClient apiClient)
    {
        _apiClient = apiClient;
    }

    public async Task<List<Cafe>> GetCafesAsync()
    {
        // Try fetching owned cafes first
        var myCafesResponse = await _apiClient.GetAsync<List<CafeDto>>($"{ApiConstants.Cafes}/my-cafes");
        if (myCafesResponse != null && myCafesResponse.Success && myCafesResponse.Data?.Count > 0)
        {
            return myCafesResponse.Data.Select(MapDtoToCafe).ToList();
        }

        // Fallback to all cafes (e.g. for general inspection / admin view)
        var allCafesResponse = await _apiClient.GetAsync<List<CafeDto>>(ApiConstants.Cafes);
        if (allCafesResponse != null && allCafesResponse.Success && allCafesResponse.Data != null)
        {
            return allCafesResponse.Data.Select(MapDtoToCafe).ToList();
        }

        return new List<Cafe>();
    }

    public async Task<List<Order>> GetCafeOrdersAsync(int cafeId, string? statusFilter = null)
    {
        var response = await _apiClient.GetAsync<List<OrderDto>>($"{ApiConstants.Orders}/cafe/{cafeId}");
        if (response == null || !response.Success || response.Data == null)
        {
            return new List<Order>();
        }

        var orders = response.Data.Select(MapDtoToOrder).ToList();

        if (!string.IsNullOrWhiteSpace(statusFilter) && !string.Equals(statusFilter, "All", StringComparison.OrdinalIgnoreCase))
        {
            orders = orders.Where(o => string.Equals(o.Status, statusFilter, StringComparison.OrdinalIgnoreCase)).ToList();
        }

        return orders;
    }

    public async Task<bool> UpdateOrderStatusAsync(int orderId, string newStatus)
    {
        var targetStatus = newStatus.Trim().ToLowerInvariant() switch
        {
            "pending" => OrderStatus.Pending,
            "confirmed" => OrderStatus.Confirmed,
            "preparing" => OrderStatus.Preparing,
            "ready" or "readyforpickup" => OrderStatus.ReadyForPickup,
            "out for delivery" or "outfordelivery" => OrderStatus.OutForDelivery,
            "delivered" or "completed" => OrderStatus.Delivered,
            "cancelled" => OrderStatus.Cancelled,
            _ => OrderStatus.Confirmed
        };

        var request = new UpdateOrderStatusRequest { NewStatus = targetStatus };
        var response = await _apiClient.PatchAsync<UpdateOrderStatusRequest, OrderDto>($"{ApiConstants.Orders}/{orderId}/status", request);
        return response != null && response.Success;
    }

    public async Task<List<Product>> GetCafeProductsAsync(int cafeId)
    {
        var response = await _apiClient.GetAsync<List<ProductDto>>($"{ApiConstants.Products}/cafe/{cafeId}");
        if (response == null || !response.Success || response.Data == null)
        {
            return new List<Product>();
        }

        return response.Data.Select(MapDtoToProduct).ToList();
    }

    public async Task<bool> ToggleProductAvailabilityAsync(int productId, bool isAvailable)
    {
        var productDto = await GetProductDtoAsync(productId);
        if (productDto == null) return false;

        var updateRequest = new UpdateProductRequest
        {
            Name = productDto.Name,
            Description = productDto.Description,
            Price = productDto.Price,
            CategoryId = productDto.CategoryId > 0 ? productDto.CategoryId : 1,
            ImageUrl = productDto.ImageUrl,
            StockQuantity = productDto.StockQuantity,
            IsAvailable = isAvailable
        };

        var response = await _apiClient.PutAsync<UpdateProductRequest, ProductDto>($"{ApiConstants.Products}/{productId}", updateRequest);
        return response != null && response.Success;
    }

    public async Task<bool> UpdateProductPriceAsync(int productId, decimal newPrice)
    {
        var productDto = await GetProductDtoAsync(productId);
        if (productDto == null) return false;

        var updateRequest = new UpdateProductRequest
        {
            Name = productDto.Name,
            Description = productDto.Description,
            Price = newPrice,
            CategoryId = productDto.CategoryId > 0 ? productDto.CategoryId : 1,
            ImageUrl = productDto.ImageUrl,
            StockQuantity = productDto.StockQuantity,
            IsAvailable = productDto.IsAvailable
        };

        var response = await _apiClient.PutAsync<UpdateProductRequest, ProductDto>($"{ApiConstants.Products}/{productId}", updateRequest);
        return response != null && response.Success;
    }

    public async Task<bool> UpdateProductStockAsync(int productId, int? stockQuantity)
    {
        var response = await _apiClient.PatchAsync<int?, bool>($"{ApiConstants.Products}/{productId}/stock", stockQuantity);
        return response != null && response.Success;
    }

    public async Task<bool> ToggleCafeStatusAsync(int cafeId, bool isOpen)
    {
        var request = new ToggleCafeStatusRequest { IsOpen = isOpen };
        var response = await _apiClient.PatchAsync<ToggleCafeStatusRequest, CafeDto>($"{ApiConstants.Cafes}/{cafeId}/status", request);
        return response != null && response.Success;
    }

    public async Task<bool> UpdateCafeAnnouncementAsync(int cafeId, string? announcement)
    {
        var request = new UpdateCafeAnnouncementRequest { Announcement = announcement };
        var response = await _apiClient.PatchAsync<UpdateCafeAnnouncementRequest, CafeDto>($"{ApiConstants.Cafes}/{cafeId}/announcement", request);
        return response != null && response.Success;
    }

    public Task<List<Expense>> GetCafeExpensesAsync(int cafeId)
    {
        var list = _localExpenses.Where(e => e.CafeId == cafeId).OrderByDescending(e => e.ExpenseDate).ToList();
        return Task.FromResult(list);
    }

    public Task<bool> AddExpenseAsync(Expense expense)
    {
        if (expense == null) return Task.FromResult(false);
        expense.Id = _localExpenses.Count > 0 ? _localExpenses.Max(e => e.Id) + 1 : 1;
        _localExpenses.Add(expense);
        return Task.FromResult(true);
    }

    public Task<bool> DeleteExpenseAsync(int expenseId)
    {
        var item = _localExpenses.FirstOrDefault(e => e.Id == expenseId);
        if (item != null)
        {
            _localExpenses.Remove(item);
            return Task.FromResult(true);
        }
        return Task.FromResult(false);
    }

    public async Task<bool> AddProductAsync(Product product)
    {
        if (product == null) return false;

        var createRequest = new CreateProductRequest
        {
            CafeId = product.CafeId,
            CategoryId = 1, // Default category
            Name = product.Name,
            Description = product.Description,
            Price = product.Price,
            ImageUrl = product.ImageUrl,
            StockQuantity = product.StockQuantity,
            IsAvailable = product.IsAvailable
        };

        var response = await _apiClient.PostAsync<CreateProductRequest, ProductDto>(ApiConstants.Products, createRequest);
        return response != null && response.Success;
    }

    public async Task<bool> DeleteProductAsync(int productId)
    {
        var response = await _apiClient.DeleteAsync<bool>($"{ApiConstants.Products}/{productId}");
        return response != null && response.Success;
    }

    private async Task<ProductDto?> GetProductDtoAsync(int productId)
    {
        var response = await _apiClient.GetAsync<ProductDto>($"{ApiConstants.Products}/{productId}");
        return response?.Data;
    }

    private static Cafe MapDtoToCafe(CafeDto dto)
    {
        return new Cafe
        {
            Id = dto.Id,
            Name = dto.Name,
            Description = dto.Description ?? string.Empty,
            Location = dto.Location,
            IsOpen = dto.IsActive,
            ImageUrl = dto.LogoUrl ?? string.Empty,
            Announcement = null
        };
    }

    private static Product MapDtoToProduct(ProductDto dto)
    {
        return new Product
        {
            Id = dto.Id,
            CafeId = dto.CafeId,
            Name = dto.Name,
            Description = dto.Description ?? string.Empty,
            Price = dto.Price,
            Category = dto.CategoryName,
            IsAvailable = dto.IsAvailable,
            ImageUrl = dto.ImageUrl ?? string.Empty,
            StockQuantity = dto.StockQuantity
        };
    }

    private static Order MapDtoToOrder(OrderDto dto)
    {
        var order = new Order
        {
            Id = dto.Id,
            CafeId = dto.CafeId,
            OrderDate = dto.CreatedAt.ToLocalTime(),
            TotalAmount = dto.TotalAmount,
            Status = dto.Status.ToString(),
            OrderType = dto.DeliveryFee > 0 ? "Delivery" : "Pickup",
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

using CampusBite.Api.Domain.Entities;
using CampusBite.Shared.DTOs;
using CampusBite.Shared.Requests;
using CampusBite.Shared.Responses;

namespace CampusBite.Api.Services.Interfaces;

public interface IProductService
{
    Task<ApiResponse<List<ProductDto>>> GetProductsByCafeAsync(int cafeId);
    Task<ApiResponse<ProductDto>> GetProductByIdAsync(int id);
    Task<Product?> GetProductEntityByIdAsync(int id);
    Task<ApiResponse<ProductDto>> CreateProductAsync(CreateProductRequest request);
    Task<ApiResponse<ProductDto>> UpdateProductAsync(int id, UpdateProductRequest request);
    Task<ApiResponse<bool>> DeleteProductAsync(int id);
    Task<ApiResponse<bool>> UpdateStockAsync(int id, int? newStock);
}

using CampusBite.Models;
using CampusBite.Services.Interfaces;
using CampusBite.Services.Network;
using CampusBite.Shared.DTOs;

namespace CampusBite.Services.Implementations;

public class CafeService : ICafeService
{
    private readonly IApiClient _apiClient;

    public CafeService(IApiClient apiClient)
    {
        _apiClient = apiClient;
    }

    public async Task<List<Cafe>> GetCafesAsync()
    {
        var response = await _apiClient.GetAsync<List<CafeDto>>(ApiConstants.Cafes);
        if (response != null && response.Success && response.Data != null)
        {
            return response.Data.Select(MapDtoToModel).ToList();
        }

        return new List<Cafe>();
    }

    public async Task<Cafe?> GetCafeByIdAsync(int id)
    {
        var response = await _apiClient.GetAsync<CafeDto>($"{ApiConstants.Cafes}/{id}");
        if (response != null && response.Success && response.Data != null)
        {
            return MapDtoToModel(response.Data);
        }

        return null;
    }

    private static Cafe MapDtoToModel(CafeDto dto)
    {
        var cafe = new Cafe
        {
            Id = dto.Id,
            Name = dto.Name,
            Description = dto.Description ?? string.Empty,
            Location = dto.Location,
            IsOpen = dto.IsActive,
            ImageUrl = dto.LogoUrl ?? string.Empty,
            Announcement = dto.Announcement
        };

        if (dto.Products != null)
        {
            foreach (var p in dto.Products)
            {
                cafe.Products.Add(new Product
                {
                    Id = p.Id,
                    CafeId = p.CafeId,
                    Name = p.Name,
                    Description = p.Description ?? string.Empty,
                    Price = p.Price,
                    Category = p.CategoryName,
                    IsAvailable = p.IsAvailable,
                    ImageUrl = p.ImageUrl ?? string.Empty,
                    StockQuantity = p.StockQuantity,
                    Cafe = cafe
                });
            }
        }

        return cafe;
    }
}

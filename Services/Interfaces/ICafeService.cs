using CampusBite.Models;

namespace CampusBite.Services.Interfaces;

public interface ICafeService
{
    Task<List<Cafe>> GetCafesAsync();
    Task<Cafe?> GetCafeByIdAsync(int id);
}

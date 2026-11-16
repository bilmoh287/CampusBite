using CampusBite.Api.Domain.Entities;
using CampusBite.Api.Infrastructure.Data;
using CampusBite.Api.Services.Interfaces;
using CampusBite.Shared.DTOs;
using CampusBite.Shared.Responses;
using Microsoft.EntityFrameworkCore;

namespace CampusBite.Api.Services.Implementations;

public class BuddyService : IBuddyService
{
    private readonly CampusBiteDbContext _context;
    private readonly ILogger<BuddyService> _logger;

    public BuddyService(CampusBiteDbContext context, ILogger<BuddyService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<ApiResponse<List<BuddyDto>>> GetBuddiesAsync(int currentUserId)
    {
        var buddies = await _context.BuddyConnections
            .AsNoTracking()
            .Include(bc => bc.Buddy)
            .Where(bc => bc.RequesterId == currentUserId && bc.IsConfirmed)
            .OrderBy(bc => bc.Buddy.FullName)
            .Select(bc => new BuddyDto
            {
                ConnectionId = bc.Id,
                BuddyId = bc.BuddyId,
                Username = bc.Buddy.Username,
                FullName = bc.Buddy.FullName,
                Email = bc.Buddy.Email,
                CreatedAt = bc.CreatedAt
            })
            .ToListAsync();

        return ApiResponse<List<BuddyDto>>.SuccessResult(buddies);
    }

    public async Task<ApiResponse<BuddyDto>> AddBuddyAsync(int currentUserId, string username)
    {
        var normalizedUsername = username.Trim().ToLowerInvariant();
        var buddy = await _context.Users
            .FirstOrDefaultAsync(u => u.Username.ToLower() == normalizedUsername);

        if (buddy == null)
        {
            return ApiResponse<BuddyDto>.FailureResult($"Student '@{username}' was not found.");
        }

        if (buddy.Id == currentUserId)
        {
            return ApiResponse<BuddyDto>.FailureResult("You cannot add yourself as a dining buddy.");
        }

        var existing = await _context.BuddyConnections
            .FirstOrDefaultAsync(bc => bc.RequesterId == currentUserId && bc.BuddyId == buddy.Id);

        if (existing != null)
        {
            return ApiResponse<BuddyDto>.FailureResult($"@{buddy.Username} is already in your dining circle.");
        }

        // Mutual connection so both friends can see and order together
        var connection = new BuddyConnection
        {
            RequesterId = currentUserId,
            BuddyId = buddy.Id,
            IsConfirmed = true,
            CreatedAt = DateTime.UtcNow
        };

        var reverseConnection = new BuddyConnection
        {
            RequesterId = buddy.Id,
            BuddyId = currentUserId,
            IsConfirmed = true,
            CreatedAt = DateTime.UtcNow
        };

        await _context.BuddyConnections.AddRangeAsync(connection, reverseConnection);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Student {UserId} added {BuddyUsername} to dining circle", currentUserId, buddy.Username);

        var resultDto = new BuddyDto
        {
            ConnectionId = connection.Id,
            BuddyId = buddy.Id,
            Username = buddy.Username,
            FullName = buddy.FullName,
            Email = buddy.Email,
            CreatedAt = connection.CreatedAt
        };

        return ApiResponse<BuddyDto>.SuccessResult(resultDto, $"@{buddy.Username} has been added to your dining circle.");
    }

    public async Task<ApiResponse<bool>> RemoveBuddyAsync(int currentUserId, int buddyId)
    {
        var connections = await _context.BuddyConnections
            .Where(bc => (bc.RequesterId == currentUserId && bc.BuddyId == buddyId) ||
                         (bc.RequesterId == buddyId && bc.BuddyId == currentUserId))
            .ToListAsync();

        if (connections.Any())
        {
            _context.BuddyConnections.RemoveRange(connections);
            await _context.SaveChangesAsync();
        }

        _logger.LogInformation("Student {UserId} removed buddy {BuddyId} from dining circle", currentUserId, buddyId);
        return ApiResponse<bool>.SuccessResult(true, "Buddy removed from your dining circle.");
    }
}

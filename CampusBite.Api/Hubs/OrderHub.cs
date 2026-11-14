using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace CampusBite.Api.Hubs;

[Authorize]
public class OrderHub : Hub
{
    private readonly ILogger<OrderHub> _logger;

    public OrderHub(ILogger<OrderHub> logger)
    {
        _logger = logger;
    }

    public async Task JoinOrder(int orderId)
    {
        var groupName = $"order_{orderId}";
        await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
        _logger.LogInformation("Connection {ConnectionId} joined Order Group {GroupName}", Context.ConnectionId, groupName);
    }

    public async Task LeaveOrder(int orderId)
    {
        var groupName = $"order_{orderId}";
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);
        _logger.LogInformation("Connection {ConnectionId} left Order Group {GroupName}", Context.ConnectionId, groupName);
    }

    public async Task JoinCafe(int cafeId)
    {
        var groupName = $"cafe_{cafeId}";
        await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
        _logger.LogInformation("Connection {ConnectionId} joined Cafe Group {GroupName}", Context.ConnectionId, groupName);
    }

    public async Task LeaveCafe(int cafeId)
    {
        var groupName = $"cafe_{cafeId}";
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);
        _logger.LogInformation("Connection {ConnectionId} left Cafe Group {GroupName}", Context.ConnectionId, groupName);
    }

    public async Task JoinCouriers()
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, "couriers");
        _logger.LogInformation("Connection {ConnectionId} joined Couriers Group", Context.ConnectionId);
    }

    public async Task LeaveCouriers()
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, "couriers");
        _logger.LogInformation("Connection {ConnectionId} left Couriers Group", Context.ConnectionId);
    }

    public override async Task OnConnectedAsync()
    {
        _logger.LogInformation("Client connected to OrderHub: {ConnectionId}, User: {User}", 
            Context.ConnectionId, Context.User?.Identity?.Name ?? "Anonymous");
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        _logger.LogInformation("Client disconnected from OrderHub: {ConnectionId}", Context.ConnectionId);
        await base.OnDisconnectedAsync(exception);
    }
}

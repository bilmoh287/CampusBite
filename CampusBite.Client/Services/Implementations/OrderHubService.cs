using CampusBite.Services.Interfaces;
using CampusBite.Services.Network;
using CampusBite.Shared.DTOs;
using Microsoft.AspNetCore.SignalR.Client;

namespace CampusBite.Services.Implementations;

public class OrderHubService : IOrderHubService, IAsyncDisposable
{
    private readonly IAuthTokenStorage _tokenStorage;
    private HubConnection? _connection;
    private readonly SemaphoreSlim _connectionLock = new(1, 1);

    private readonly HashSet<int> _activeOrderGroups = new();
    private readonly HashSet<int> _activeCafeGroups = new();
    private bool _couriersGroupJoined;

    public event Action<OrderDto>? OrderStatusChanged;
    public event Action<OrderDto>? OrderCreated;
    public event Action<OrderDto>? DeliveryAvailable;
    public event Action<OrderDto>? OrderUpdated;
    public event Action<int, string?>? AnnouncementUpdated;
    public event Action<int, bool>? CafeStatusChanged;
    public event Action<PassDeductionAlertDto>? PassDeductionAlertReceived;

    public bool IsConnected => _connection?.State == HubConnectionState.Connected;

    public OrderHubService(IAuthTokenStorage tokenStorage)
    {
        _tokenStorage = tokenStorage;
    }

    public async Task EnsureConnectedAsync()
    {
        if (_connection != null && _connection.State == HubConnectionState.Connected)
        {
            return;
        }

        await _connectionLock.WaitAsync();
        try
        {
            if (_connection != null && _connection.State == HubConnectionState.Connected)
            {
                return;
            }

            if (_connection == null)
            {
                var hubUrl = $"{ApiConstants.BaseUrl.TrimEnd('/')}{ApiConstants.OrderHubUrl}";
                _connection = new HubConnectionBuilder()
                    .WithUrl(hubUrl, options =>
                    {
                        options.AccessTokenProvider = () => _tokenStorage.GetAccessTokenAsync();
                    })
                    .WithAutomaticReconnect(new[] 
                    { 
                        TimeSpan.Zero, 
                        TimeSpan.FromSeconds(2), 
                        TimeSpan.FromSeconds(5), 
                        TimeSpan.FromSeconds(10) 
                    })
                    .Build();

                RegisterHubHandlers();
            }

            if (_connection.State == HubConnectionState.Disconnected)
            {
                await _connection.StartAsync();

                // Re-join active groups after initial start or manual reconnect
                await RejoinActiveGroupsAsync();
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[SignalR] Connection start failed: {ex.Message}");
        }
        finally
        {
            _connectionLock.Release();
        }
    }

    private void RegisterHubHandlers()
    {
        if (_connection == null) return;

        _connection.On<OrderDto>("OrderStatusChanged", (order) =>
        {
            MainThread.BeginInvokeOnMainThread(() => OrderStatusChanged?.Invoke(order));
        });

        _connection.On<OrderDto>("OrderCreated", (order) =>
        {
            MainThread.BeginInvokeOnMainThread(() => OrderCreated?.Invoke(order));
        });

        _connection.On<OrderDto>("DeliveryAvailable", (order) =>
        {
            MainThread.BeginInvokeOnMainThread(() => DeliveryAvailable?.Invoke(order));
        });

        _connection.On<OrderDto>("OrderUpdated", (order) =>
        {
            MainThread.BeginInvokeOnMainThread(() => OrderUpdated?.Invoke(order));
        });

        _connection.On<int, string?>("AnnouncementUpdated", (cafeId, announcement) =>
        {
            MainThread.BeginInvokeOnMainThread(() => AnnouncementUpdated?.Invoke(cafeId, announcement));
        });

        _connection.On<int, bool>("CafeStatusChanged", (cafeId, isOpen) =>
        {
            MainThread.BeginInvokeOnMainThread(() => CafeStatusChanged?.Invoke(cafeId, isOpen));
        });

        _connection.On<PassDeductionAlertDto>("PassDeductedForGroupOrder", (alert) =>
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                PassDeductionAlertReceived?.Invoke(alert);
                _ = Shell.Current?.DisplayAlertAsync(
                    "🎟️ Meal Pass Used!",
                    $"{alert.HostName ?? "A friend"} tagged you for group dining at {alert.CafeName}.\n1 meal pass credit deducted (Ticket: #{alert.OrderNumber}).\nRemaining meals: {alert.RemainingCredits}",
                    "OK");
            });
        });

        _connection.On<PassDeductionAlertDto>("PassDeductedForWalkIn", (alert) =>
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                PassDeductionAlertReceived?.Invoke(alert);
                _ = Shell.Current?.DisplayAlertAsync(
                    "🎟️ Counter Pass Punched!",
                    $"1 meal credit was punched at {alert.CafeName} counter for {alert.MealName ?? "Meal"}.\nRemaining meals: {alert.RemainingCredits}",
                    "OK");
            });
        });

        _connection.Reconnected += async (connectionId) =>
        {
            System.Diagnostics.Debug.WriteLine($"[SignalR] Reconnected: {connectionId}");
            await RejoinActiveGroupsAsync();
        };

        _connection.Closed += async (error) =>
        {
            System.Diagnostics.Debug.WriteLine($"[SignalR] Connection Closed: {error?.Message}");
        };
    }

    private async Task RejoinActiveGroupsAsync()
    {
        if (!IsConnected || _connection == null) return;

        try
        {
            foreach (var orderId in _activeOrderGroups.ToList())
            {
                await _connection.InvokeAsync("JoinOrder", orderId);
            }

            foreach (var cafeId in _activeCafeGroups.ToList())
            {
                await _connection.InvokeAsync("JoinCafe", cafeId);
            }

            if (_couriersGroupJoined)
            {
                await _connection.InvokeAsync("JoinCouriers");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[SignalR] Error re-joining groups: {ex.Message}");
        }
    }

    public async Task JoinOrderGroupAsync(int orderId)
    {
        _activeOrderGroups.Add(orderId);
        await EnsureConnectedAsync();
        if (IsConnected && _connection != null)
        {
            try
            {
                await _connection.InvokeAsync("JoinOrder", orderId);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[SignalR] JoinOrder failed: {ex.Message}");
            }
        }
    }

    public async Task LeaveOrderGroupAsync(int orderId)
    {
        _activeOrderGroups.Remove(orderId);
        if (IsConnected && _connection != null)
        {
            try
            {
                await _connection.InvokeAsync("LeaveOrder", orderId);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[SignalR] LeaveOrder failed: {ex.Message}");
            }
        }
    }

    public async Task JoinCafeGroupAsync(int cafeId)
    {
        _activeCafeGroups.Add(cafeId);
        await EnsureConnectedAsync();
        if (IsConnected && _connection != null)
        {
            try
            {
                await _connection.InvokeAsync("JoinCafe", cafeId);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[SignalR] JoinCafe failed: {ex.Message}");
            }
        }
    }

    public async Task LeaveCafeGroupAsync(int cafeId)
    {
        _activeCafeGroups.Remove(cafeId);
        if (IsConnected && _connection != null)
        {
            try
            {
                await _connection.InvokeAsync("LeaveCafe", cafeId);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[SignalR] LeaveCafe failed: {ex.Message}");
            }
        }
    }

    public async Task JoinCouriersGroupAsync()
    {
        _couriersGroupJoined = true;
        await EnsureConnectedAsync();
        if (IsConnected && _connection != null)
        {
            try
            {
                await _connection.InvokeAsync("JoinCouriers");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[SignalR] JoinCouriers failed: {ex.Message}");
            }
        }
    }

    public async Task LeaveCouriersGroupAsync()
    {
        _couriersGroupJoined = false;
        if (IsConnected && _connection != null)
        {
            try
            {
                await _connection.InvokeAsync("LeaveCouriers");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[SignalR] LeaveCouriers failed: {ex.Message}");
            }
        }
    }

    public async Task DisconnectAsync()
    {
        if (_connection != null)
        {
            try
            {
                await _connection.StopAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[SignalR] StopAsync error: {ex.Message}");
            }
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_connection != null)
        {
            await _connection.DisposeAsync();
            _connection = null;
        }
        _connectionLock.Dispose();
    }
}

using CampusBite.Api.Services.Interfaces;
using CampusBite.Shared.DTOs;
using CampusBite.Shared.Requests;
using CampusBite.Shared.Responses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CampusBite.Api.Controllers;

public class OrdersController : BaseApiController
{
    private readonly IOrderService _orderService;
    private readonly ICafeService _cafeService;
    private readonly IAuthorizationService _authorizationService;

    public OrdersController(
        IOrderService orderService,
        ICafeService cafeService,
        IAuthorizationService authorizationService)
    {
        _orderService = orderService;
        _cafeService = cafeService;
        _authorizationService = authorizationService;
    }

    [HttpPost]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<OrderDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<OrderDto>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateOrder([FromBody] CreateOrderRequest request)
    {
        if (!CurrentUserId.HasValue)
        {
            return Unauthorized();
        }

        var result = await _orderService.CreateOrderAsync(CurrentUserId.Value, request);
        return HandleResult(result);
    }

    [HttpGet("my-orders")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<List<OrderDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMyOrders()
    {
        if (!CurrentUserId.HasValue)
        {
            return Unauthorized();
        }

        var result = await _orderService.GetOrdersForCustomerAsync(CurrentUserId.Value);
        return HandleResult(result);
    }

    [HttpGet("cafe/{cafeId}")]
    [Authorize(Roles = "CafeOwner,Admin")]
    [ProducesResponseType(typeof(ApiResponse<List<OrderDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetCafeOrders(int cafeId)
    {
        var cafe = await _cafeService.GetCafeEntityByIdAsync(cafeId);
        if (cafe == null)
        {
            return NotFound(ApiResponse<List<OrderDto>>.FailureResult("Café not found."));
        }

        // Ownership Policy Check: Only this café's owner or an Admin can view incoming orders!
        var authResult = await _authorizationService.AuthorizeAsync(User, cafe, "CafeOwnerOrAdmin");
        if (!authResult.Succeeded)
        {
            return Forbid(); // HTTP 403
        }

        var result = await _orderService.GetOrdersForCafeAsync(cafeId);
        return HandleResult(result);
    }

    [HttpGet("available-for-delivery")]
    [Authorize(Roles = "Courier,Admin")]
    [ProducesResponseType(typeof(ApiResponse<List<OrderDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAvailableOrdersForCourier()
    {
        var result = await _orderService.GetAvailableOrdersForCourierAsync();
        return HandleResult(result);
    }

    [HttpGet("{id}")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<OrderDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetOrderById(int id)
    {
        var order = await _orderService.GetOrderEntityByIdAsync(id);
        if (order == null)
        {
            return NotFound(ApiResponse<OrderDto>.FailureResult("Order not found."));
        }

        // Resource-Based Ownership Policy Check (from the course!)
        // Ensures Student A CANNOT view Student B's order ticket!
        var authResult = await _authorizationService.AuthorizeAsync(User, order, "OrderOwnerOrAdmin");
        if (!authResult.Succeeded)
        {
            return Forbid(); // HTTP 403 Forbidden!
        }

        var result = await _orderService.GetOrderByIdAsync(id);
        return HandleResult(result);
    }

    [HttpPatch("{id}/status")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<OrderDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> UpdateOrderStatus(int id, [FromBody] UpdateOrderStatusRequest request)
    {
        if (!CurrentUserId.HasValue)
        {
            return Unauthorized();
        }

        var order = await _orderService.GetOrderEntityByIdAsync(id);
        if (order == null)
        {
            return NotFound(ApiResponse<OrderDto>.FailureResult("Order not found."));
        }

        // Resource-Based Ownership Policy Check
        var authResult = await _authorizationService.AuthorizeAsync(User, order, "OrderOwnerOrAdmin");
        if (!authResult.Succeeded)
        {
            return Forbid(); // HTTP 403 Forbidden!
        }

        var result = await _orderService.UpdateOrderStatusAsync(id, request.NewStatus, CurrentUserId.Value);
        return HandleResult(result);
    }

    [HttpPost("{id}/claim")]
    [Authorize(Roles = "Courier,Admin")]
    [ProducesResponseType(typeof(ApiResponse<OrderDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<OrderDto>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ClaimOrder(int id)
    {
        if (!CurrentUserId.HasValue)
        {
            return Unauthorized();
        }

        var result = await _orderService.ClaimOrderAsync(id, CurrentUserId.Value);
        return HandleResult(result);
    }
}

using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using CampusBite.Api.Tests.Infrastructure;
using CampusBite.Shared.DTOs;
using CampusBite.Shared.Enums;
using CampusBite.Shared.Requests;
using CampusBite.Shared.Responses;
using FluentAssertions;
using Xunit;

namespace CampusBite.Api.Tests.Controllers;

public class OrderLifecycleTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public OrderLifecycleTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    /// <summary>
    /// Helper: logs in with credentials and returns an authenticated HttpClient with Bearer token header.
    /// </summary>
    private async Task<HttpClient> CreateAuthenticatedClientAsync(string email, string password)
    {
        var client = _factory.CreateClient();
        var loginResponse = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest
        {
            Email = email,
            Password = password
        });

        loginResponse.StatusCode.Should().Be(HttpStatusCode.OK, "Authentication helper must succeed");

        var authResult = await loginResponse.Content.ReadFromJsonAsync<ApiResponse<AuthResponse>>();
        authResult.Should().NotBeNull();
        authResult!.Data.Should().NotBeNull();

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", authResult.Data!.Token);
        return client;
    }

    [Fact]
    public async Task Complete_Order_Lifecycle_Student_To_Kitchen_To_Courier_Succeeds()
    {
        // 1. Arrange: Create authenticated clients for all 3 campus roles
        var studentClient = await CreateAuthenticatedClientAsync("student@aau.edu.et", "Password123!");
        var managerClient = await CreateAuthenticatedClientAsync("manager@centralcafe.com", "Password123!");
        var courierClient = await CreateAuthenticatedClientAsync("courier@campusbite.com", "Password123!");

        // -------------------------------------------------------------
        // STEP 1: Student places an order for 2 Double Cheeseburgers
        // -------------------------------------------------------------
        var createRequest = new CreateOrderRequest
        {
            CafeId = 1,
            DeliveryLocation = "Library 2nd Floor, Study Room B",
            PaymentMethod = PaymentMethod.Cash,
            SpecialNotes = "Extra napkins please",
            Items = new List<CreateOrderItemRequest>
            {
                new() { ProductId = 1, Quantity = 2 }
            }
        };

        var orderResponse = await studentClient.PostAsJsonAsync("/api/orders", createRequest);
        orderResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var orderResult = await orderResponse.Content.ReadFromJsonAsync<ApiResponse<OrderDto>>();
        orderResult.Should().NotBeNull();
        orderResult!.Success.Should().BeTrue();

        var order = orderResult.Data!;
        order.Id.Should().BeGreaterThan(0);
        order.Status.Should().Be(OrderStatus.Pending);
        order.TotalAmount.Should().Be(455.00m); // 2 * 220.00 ETB (Special Tibs) + 15.00 ETB delivery fee
        int orderId = order.Id;

        // -------------------------------------------------------------
        // STEP 2: Kitchen Manager sees incoming order and prepares it
        // -------------------------------------------------------------
        var cafeOrdersResponse = await managerClient.GetAsync("/api/orders/cafe/1");
        cafeOrdersResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var cafeOrdersResult = await cafeOrdersResponse.Content.ReadFromJsonAsync<ApiResponse<List<OrderDto>>>();
        cafeOrdersResult!.Data.Should().Contain(o => o.Id == orderId, "New order must appear on manager's KDS board");

        // Manager marks "Preparing"
        var preparingResponse = await managerClient.PatchAsJsonAsync($"/api/orders/{orderId}/status", new UpdateOrderStatusRequest
        {
            NewStatus = OrderStatus.Preparing
        });
        preparingResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // Manager marks "Ready for Pickup"
        var readyResponse = await managerClient.PatchAsJsonAsync($"/api/orders/{orderId}/status", new UpdateOrderStatusRequest
        {
            NewStatus = OrderStatus.ReadyForPickup
        });
        readyResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // -------------------------------------------------------------
        // STEP 3: Courier views available delivery pool and claims ticket
        // -------------------------------------------------------------
        var poolResponse = await courierClient.GetAsync("/api/orders/available-for-delivery");
        poolResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var poolResult = await poolResponse.Content.ReadFromJsonAsync<ApiResponse<List<OrderDto>>>();
        poolResult!.Data.Should().Contain(o => o.Id == orderId, "Order must enter courier dispatch pool when Ready");

        // Courier claims the order
        var claimResponse = await courierClient.PostAsync($"/api/orders/{orderId}/claim", null);
        claimResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // -------------------------------------------------------------
        // STEP 4: Courier transitions to OutForDelivery and Delivered
        // -------------------------------------------------------------
        var outResponse = await courierClient.PatchAsJsonAsync($"/api/orders/{orderId}/status", new UpdateOrderStatusRequest
        {
            NewStatus = OrderStatus.OutForDelivery
        });
        outResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var deliveredResponse = await courierClient.PatchAsJsonAsync($"/api/orders/{orderId}/status", new UpdateOrderStatusRequest
        {
            NewStatus = OrderStatus.Delivered
        });
        deliveredResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // -------------------------------------------------------------
        // STEP 5: Student tracks their order and verifies it is Delivered
        // -------------------------------------------------------------
        var studentTrackResponse = await studentClient.GetAsync($"/api/orders/{orderId}");
        studentTrackResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var finalOrderResult = await studentTrackResponse.Content.ReadFromJsonAsync<ApiResponse<OrderDto>>();
        finalOrderResult!.Data!.Status.Should().Be(OrderStatus.Delivered);
    }

    [Fact]
    public async Task Complete_CounterPickup_Order_Lifecycle_Without_Courier_Succeeds()
    {
        // 1. Arrange: Student and Kitchen Manager clients
        var studentClient = await CreateAuthenticatedClientAsync("student@aau.edu.et", "Password123!");
        var managerClient = await CreateAuthenticatedClientAsync("manager@centralcafe.com", "Password123!");

        // -------------------------------------------------------------
        // STEP 1: Student places a Counter Pickup order (Self-Pickup)
        // -------------------------------------------------------------
        var createRequest = new CreateOrderRequest
        {
            CafeId = 1,
            DeliveryLocation = "Pickup at Counter (Self-Pickup)",
            PaymentMethod = PaymentMethod.Cash,
            SpecialNotes = "I am waiting in the cafeteria lobby",
            Items = new List<CreateOrderItemRequest>
            {
                new() { ProductId = 2, Quantity = 1 } // Veggie Roll
            }
        };

        var orderResponse = await studentClient.PostAsJsonAsync("/api/orders", createRequest);
        orderResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var orderResult = await orderResponse.Content.ReadFromJsonAsync<ApiResponse<OrderDto>>();
        orderResult!.Success.Should().BeTrue();
        int orderId = orderResult.Data!.Id;

        // -------------------------------------------------------------
        // STEP 2: Kitchen Manager prepares the food and marks Ready
        // -------------------------------------------------------------
        var preparingResponse = await managerClient.PatchAsJsonAsync($"/api/orders/{orderId}/status", new UpdateOrderStatusRequest
        {
            NewStatus = OrderStatus.Preparing
        });
        preparingResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var readyResponse = await managerClient.PatchAsJsonAsync($"/api/orders/{orderId}/status", new UpdateOrderStatusRequest
        {
            NewStatus = OrderStatus.ReadyForPickup
        });
        readyResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // -------------------------------------------------------------
        // STEP 3: Student arrives at counter; Manager hands meal directly
        // -------------------------------------------------------------
        var deliveredResponse = await managerClient.PatchAsJsonAsync($"/api/orders/{orderId}/status", new UpdateOrderStatusRequest
        {
            NewStatus = OrderStatus.Delivered
        });
        deliveredResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // -------------------------------------------------------------
        // STEP 4: Verify Student sees order completed with NO courier
        // -------------------------------------------------------------
        var studentOrderResponse = await studentClient.GetAsync($"/api/orders/{orderId}");
        studentOrderResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var finalOrder = (await studentOrderResponse.Content.ReadFromJsonAsync<ApiResponse<OrderDto>>())!.Data!;
        finalOrder.Status.Should().Be(OrderStatus.Delivered);
        finalOrder.CourierId.Should().BeNull("Pickup orders are handed over directly at the counter with no courier assigned");
    }

    [Fact]
    public async Task Security_Student_Cannot_Access_Cafe_Admin_Orders_ReturnsForbidden403()
    {
        // 1. Arrange: Student authenticated client
        var studentClient = await CreateAuthenticatedClientAsync("student@aau.edu.et", "Password123!");

        // 2. Act: Student attempts to access manager's cafe orders endpoint
        var response = await studentClient.GetAsync("/api/orders/cafe/1");

        // 3. Assert: Role-based policy must strictly block the student with HTTP 403 Forbidden
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Security_StudentA_Cannot_View_StudentB_Order_ReturnsForbidden403()
    {
        // 1. Arrange: Two distinct student accounts
        var studentA = await CreateAuthenticatedClientAsync("student@aau.edu.et", "Password123!");

        // Register a second distinct student
        var studentBEmail = $"student_b_{Guid.NewGuid():N}@aau.edu.et";
        var client = _factory.CreateClient();
        await client.PostAsJsonAsync("/api/auth/register", new RegisterRequest
        {
            FullName = "Student B",
            Email = studentBEmail,
            Password = "Password123!",
            Role = UserRole.Student,
            UniversityId = 1
        });
        var studentB = await CreateAuthenticatedClientAsync(studentBEmail, "Password123!");

        // 2. Student A places an order
        var orderResponse = await studentA.PostAsJsonAsync("/api/orders", new CreateOrderRequest
        {
            CafeId = 1,
            DeliveryLocation = "Dorm Block 5",
            Items = new List<CreateOrderItemRequest> { new() { ProductId = 1, Quantity = 1 } }
        });
        var order = (await orderResponse.Content.ReadFromJsonAsync<ApiResponse<OrderDto>>())!.Data!;

        // 3. Act: Student B attempts to fetch Student A's order ticket by ID
        var response = await studentB.GetAsync($"/api/orders/{order.Id}");

        // 4. Assert: Resource-based ownership handler must return HTTP 403 Forbidden (preventing IDOR)
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Validation_Order_Without_Items_ReturnsBadRequest()
    {
        // 1. Arrange: Authenticated student
        var studentClient = await CreateAuthenticatedClientAsync("student@aau.edu.et", "Password123!");

        // 2. Act: Send an invalid order with 0 items
        var invalidRequest = new CreateOrderRequest
        {
            CafeId = 1,
            DeliveryLocation = "Library",
            Items = new List<CreateOrderItemRequest>()
        };

        var response = await studentClient.PostAsJsonAsync("/api/orders", invalidRequest);

        // 3. Assert: Must fail validation with HTTP 400 Bad Request
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Cancelling_Pending_Order_Restores_Product_Inventory_Stock()
    {
        // 1. Arrange: Check initial stock quantity of product 1
        var client = _factory.CreateClient();
        var initialProdRes = await client.GetAsync("/api/products/1");
        initialProdRes.StatusCode.Should().Be(HttpStatusCode.OK);
        var initialProd = (await initialProdRes.Content.ReadFromJsonAsync<ApiResponse<ProductDto>>())!.Data!;
        int initialStock = initialProd.StockQuantity ?? 0;
        initialStock.Should().BeGreaterThan(3, "Test requires initial stock > 3");

        // 2. Student places an order for 3 portions
        var studentClient = await CreateAuthenticatedClientAsync("student@aau.edu.et", "Password123!");
        var orderRes = await studentClient.PostAsJsonAsync("/api/orders", new CreateOrderRequest
        {
            CafeId = 1,
            DeliveryLocation = "Science Faculty Block B",
            PaymentMethod = PaymentMethod.Cash,
            Items = new List<CreateOrderItemRequest>
            {
                new() { ProductId = 1, Quantity = 3 }
            }
        });
        orderRes.StatusCode.Should().Be(HttpStatusCode.OK);
        var order = (await orderRes.Content.ReadFromJsonAsync<ApiResponse<OrderDto>>())!.Data!;

        // 3. Verify stock was deducted by 3
        var midProdRes = await client.GetAsync("/api/products/1");
        var midProd = (await midProdRes.Content.ReadFromJsonAsync<ApiResponse<ProductDto>>())!.Data!;
        midProd.StockQuantity.Should().Be(initialStock - 3, "Stock must decrement upon order placement");

        // 4. Act: Student cancels the pending order
        var cancelRes = await studentClient.PatchAsJsonAsync($"/api/orders/{order.Id}/status", new UpdateOrderStatusRequest
        {
            NewStatus = OrderStatus.Cancelled
        });
        cancelRes.StatusCode.Should().Be(HttpStatusCode.OK);

        // 5. Assert: Stock is restored back to the initial quantity!
        var restoredProdRes = await client.GetAsync("/api/products/1");
        var restoredProd = (await restoredProdRes.Content.ReadFromJsonAsync<ApiResponse<ProductDto>>())!.Data!;
        restoredProd.StockQuantity.Should().Be(initialStock, "Stock must be fully restored when pending order is cancelled");
    }

    [Fact]
    public async Task DineIn_Order_Charges_Zero_Packaging_And_Delivery_Fees()
    {
        var studentClient = await CreateAuthenticatedClientAsync("student@aau.edu.et", "Password123!");
        var orderRes = await studentClient.PostAsJsonAsync("/api/orders", new CreateOrderRequest
        {
            CafeId = 1,
            OrderType = "DineIn",
            DeliveryLocation = "Table 12, Ground Floor",
            PaymentMethod = PaymentMethod.Cash,
            PackagingOption = PackagingOption.StandardFree,
            Items = new List<CreateOrderItemRequest> { new() { ProductId = 3, Quantity = 1 } } // 130 ETB
        });

        orderRes.StatusCode.Should().Be(HttpStatusCode.OK);
        var order = (await orderRes.Content.ReadFromJsonAsync<ApiResponse<OrderDto>>())!.Data!;
        order.OrderType.Should().Be("DineIn");
        order.PackagingOption.Should().Be(PackagingOption.StandardFree);
        order.PackagingFee.Should().Be(0.00m, "Dine-in served on ceramic plates incurs zero packaging fee");
        order.DeliveryFee.Should().Be(0.00m, "Dine-in incurs zero delivery fee");
        order.TotalAmount.Should().Be(130.00m);
    }

    [Fact]
    public async Task Pickup_With_Festal_Charges_Zero_Packaging_And_Zero_Delivery()
    {
        var studentClient = await CreateAuthenticatedClientAsync("student@aau.edu.et", "Password123!");
        var orderRes = await studentClient.PostAsJsonAsync("/api/orders", new CreateOrderRequest
        {
            CafeId = 1,
            OrderType = "Pickup",
            DeliveryLocation = "Pickup Counter",
            PaymentMethod = PaymentMethod.Cash,
            PackagingOption = PackagingOption.StandardFree,
            Items = new List<CreateOrderItemRequest> { new() { ProductId = 3, Quantity = 1 } } // 130 ETB
        });

        orderRes.StatusCode.Should().Be(HttpStatusCode.OK);
        var order = (await orderRes.Content.ReadFromJsonAsync<ApiResponse<OrderDto>>())!.Data!;
        order.OrderType.Should().Be("Pickup");
        order.PackagingOption.Should().Be(PackagingOption.StandardFree);
        order.PackagingFee.Should().Be(0.00m, "Standard festal/wrap is free of charge");
        order.DeliveryFee.Should().Be(0.00m);
        order.TotalAmount.Should().Be(130.00m);
    }

    [Fact]
    public async Task Pickup_With_TakeawayBox_Charges_Packaging_Fee_Only()
    {
        var studentClient = await CreateAuthenticatedClientAsync("student@aau.edu.et", "Password123!");
        var orderRes = await studentClient.PostAsJsonAsync("/api/orders", new CreateOrderRequest
        {
            CafeId = 1,
            OrderType = "Pickup",
            DeliveryLocation = "Pickup Counter",
            PaymentMethod = PaymentMethod.Cash,
            PackagingOption = PackagingOption.TakeawayBox,
            Items = new List<CreateOrderItemRequest> { new() { ProductId = 3, Quantity = 1 } } // 130 ETB
        });

        orderRes.StatusCode.Should().Be(HttpStatusCode.OK);
        var order = (await orderRes.Content.ReadFromJsonAsync<ApiResponse<OrderDto>>())!.Data!;
        order.OrderType.Should().Be("Pickup");
        order.PackagingOption.Should().Be(PackagingOption.TakeawayBox);
        order.PackagingFee.Should().Be(25.00m, "Takeaway plastic box costs 25 ETB configured by cafe");
        order.DeliveryFee.Should().Be(0.00m);
        order.TotalAmount.Should().Be(155.00m); // 130 + 25 = 155 ETB
    }

    [Fact]
    public async Task Delivery_With_Festal_Charges_Courier_Fee_Without_Packaging_Fee()
    {
        var studentClient = await CreateAuthenticatedClientAsync("student@aau.edu.et", "Password123!");
        var orderRes = await studentClient.PostAsJsonAsync("/api/orders", new CreateOrderRequest
        {
            CafeId = 1,
            OrderType = "Delivery",
            DeliveryLocation = "Dorm Block 5, Room 102",
            PaymentMethod = PaymentMethod.Cash,
            PackagingOption = PackagingOption.StandardFree, // Student walking courier in festal
            Items = new List<CreateOrderItemRequest> { new() { ProductId = 3, Quantity = 1 } } // 130 ETB
        });

        orderRes.StatusCode.Should().Be(HttpStatusCode.OK);
        var order = (await orderRes.Content.ReadFromJsonAsync<ApiResponse<OrderDto>>())!.Data!;
        order.OrderType.Should().Be("Delivery");
        order.PackagingOption.Should().Be(PackagingOption.StandardFree);
        order.PackagingFee.Should().Be(0.00m);
        order.DeliveryFee.Should().Be(15.00m);
        order.TotalAmount.Should().Be(145.00m); // 130 + 15 = 145 ETB
    }

    [Fact]
    public async Task Delivery_With_TakeawayBox_Charges_Both_Delivery_And_Packaging_Fee()
    {
        var studentClient = await CreateAuthenticatedClientAsync("student@aau.edu.et", "Password123!");
        var orderRes = await studentClient.PostAsJsonAsync("/api/orders", new CreateOrderRequest
        {
            CafeId = 1,
            OrderType = "Delivery",
            DeliveryLocation = "Dorm Block 5, Room 102",
            PaymentMethod = PaymentMethod.Cash,
            PackagingOption = PackagingOption.TakeawayBox,
            Items = new List<CreateOrderItemRequest> { new() { ProductId = 3, Quantity = 1 } } // 130 ETB
        });

        orderRes.StatusCode.Should().Be(HttpStatusCode.OK);
        var order = (await orderRes.Content.ReadFromJsonAsync<ApiResponse<OrderDto>>())!.Data!;
        order.OrderType.Should().Be("Delivery");
        order.PackagingOption.Should().Be(PackagingOption.TakeawayBox);
        order.PackagingFee.Should().Be(25.00m, "Sturdy container fee");
        order.DeliveryFee.Should().Be(15.00m, "Courier fee");
        order.TotalAmount.Should().Be(170.00m); // 130 + 15 + 25 = 170 ETB
    }
}


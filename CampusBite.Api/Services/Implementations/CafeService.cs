using CampusBite.Api.Domain.Entities;
using CampusBite.Api.Hubs;
using CampusBite.Api.Infrastructure.Data;
using CampusBite.Api.Services.Interfaces;
using CampusBite.Shared.DTOs;
using CampusBite.Shared.Enums;
using CampusBite.Shared.Responses;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace CampusBite.Api.Services.Implementations;

public class CafeService : ICafeService
{
    private readonly CampusBiteDbContext _context;
    private readonly ILogger<CafeService> _logger;
    private readonly IHubContext<OrderHub> _hubContext;

    public CafeService(CampusBiteDbContext context, ILogger<CafeService> logger, IHubContext<OrderHub> hubContext)
    {
        _context = context;
        _logger = logger;
        _hubContext = hubContext;
    }

    public async Task<ApiResponse<List<CafeDto>>> GetCafesAsync(int? universityId, string? searchTerm)
    {
        var query = _context.Cafes
            .AsNoTracking()
            .Include(c => c.University)
            .Include(c => c.Owner)
            .Where(c => c.IsActive);

        if (universityId.HasValue && universityId.Value > 0)
        {
            query = query.Where(c => c.UniversityId == universityId.Value);
        }

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var search = searchTerm.Trim().ToLower();
            query = query.Where(c => c.Name.ToLower().Contains(search) || c.Location.ToLower().Contains(search));
        }

        var cafes = await query
            .OrderBy(c => c.Name)
            .Select(c => MapToDto(c, false))
            .ToListAsync();

        return ApiResponse<List<CafeDto>>.SuccessResult(cafes);
    }

    public async Task<ApiResponse<CafeDto>> GetCafeByIdAsync(int id)
    {
        var cafe = await _context.Cafes
            .AsNoTracking()
            .Include(c => c.University)
            .Include(c => c.Owner)
            .Include(c => c.Categories.OrderBy(cat => cat.DisplayOrder))
            .Include(c => c.Products.Where(p => p.IsAvailable))
                .ThenInclude(p => p.Category)
            .FirstOrDefaultAsync(c => c.Id == id);

        if (cafe == null)
        {
            return ApiResponse<CafeDto>.FailureResult("Café not found.");
        }

        return ApiResponse<CafeDto>.SuccessResult(MapToDto(cafe, true));
    }

    public async Task<Cafe?> GetCafeEntityByIdAsync(int id)
    {
        return await _context.Cafes
            .Include(c => c.Owner)
            .FirstOrDefaultAsync(c => c.Id == id);
    }

    public async Task<ApiResponse<List<CafeDto>>> GetCafesByOwnerAsync(int ownerId)
    {
        var cafes = await _context.Cafes
            .AsNoTracking()
            .Include(c => c.University)
            .Where(c => c.OwnerId == ownerId)
            .Select(c => MapToDto(c, false))
            .ToListAsync();

        return ApiResponse<List<CafeDto>>.SuccessResult(cafes);
    }

    public async Task<ApiResponse<CafeDto>> UpdateAnnouncementAsync(int cafeId, string? announcement, int currentUserId)
    {
        var cafe = await _context.Cafes.FirstOrDefaultAsync(c => c.Id == cafeId);
        if (cafe == null)
        {
            return ApiResponse<CafeDto>.FailureResult("Café not found.");
        }

        if (cafe.OwnerId != currentUserId)
        {
            return ApiResponse<CafeDto>.FailureResult("You do not have permission to manage this café.");
        }

        cafe.Announcement = string.IsNullOrWhiteSpace(announcement) ? null : announcement.Trim();
        await _context.SaveChangesAsync();

        _logger.LogInformation("Café {CafeId} announcement updated by Owner {OwnerId}: '{Announcement}'", cafeId, currentUserId, cafe.Announcement);

        // SignalR: Broadcast announcement update live to students & monitors viewing this cafe
        await _hubContext.Clients.Group($"cafe_{cafeId}").SendAsync("AnnouncementUpdated", cafeId, cafe.Announcement);

        return ApiResponse<CafeDto>.SuccessResult(MapToDto(cafe, false));
    }

    public async Task<ApiResponse<CafeDto>> ToggleStatusAsync(int cafeId, bool isOpen, int currentUserId)
    {
        var cafe = await _context.Cafes.FirstOrDefaultAsync(c => c.Id == cafeId);
        if (cafe == null)
        {
            return ApiResponse<CafeDto>.FailureResult("Café not found.");
        }

        if (cafe.OwnerId != currentUserId)
        {
            return ApiResponse<CafeDto>.FailureResult("You do not have permission to manage this café.");
        }

        cafe.IsActive = isOpen;
        await _context.SaveChangesAsync();

        _logger.LogInformation("Café {CafeId} active/open status changed to {IsOpen} by Owner {OwnerId}", cafeId, isOpen, currentUserId);

        // SignalR: Broadcast open/close status live to students & monitors viewing this cafe
        await _hubContext.Clients.Group($"cafe_{cafeId}").SendAsync("CafeStatusChanged", cafeId, isOpen);

        return ApiResponse<CafeDto>.SuccessResult(MapToDto(cafe, false));
    }

    public async Task<ApiResponse<OrderDto>> CounterPunchPassAsync(int cafeId, CampusBite.Shared.Requests.CounterPunchRequest request, int cashierUserId)
    {
        var cafe = await _context.Cafes
            .Include(c => c.StaffMembers)
            .FirstOrDefaultAsync(c => c.Id == cafeId);

        if (cafe == null)
        {
            return ApiResponse<OrderDto>.FailureResult("Café was not found.");
        }

        // Verify cashier is the owner, a staff member of this cafe, or admin
        var user = await _context.Users.FindAsync(cashierUserId);
        bool isAuthorized = cafe.OwnerId == cashierUserId || 
                            cafe.StaffMembers.Any(s => s.Id == cashierUserId) || 
                            (user != null && user.Role == UserRole.Admin);

        if (!isAuthorized)
        {
            return ApiResponse<OrderDto>.FailureResult("You do not have permission to punch passes for this café.");
        }

        var student = await _context.Users
            .FirstOrDefaultAsync(u => u.Username.ToLower() == request.Username.Trim().ToLowerInvariant());

        if (student == null)
        {
            return ApiResponse<OrderDto>.FailureResult($"Student '@{request.Username}' was not found.");
        }

        var contract = await _context.StudentContracts
            .Include(sc => sc.ContractPlan)
            .Where(sc => sc.StudentId == student.Id && 
                         sc.ContractPlan.CafeId == cafeId && 
                         sc.EndDate >= DateTime.UtcNow)
            .OrderByDescending(sc => sc.Status == ContractStatus.Active)
            .ThenBy(sc => sc.EndDate)
            .FirstOrDefaultAsync();

        if (contract == null)
        {
            return ApiResponse<OrderDto>.FailureResult($"Student '@{student.Username}' does not have an active pass at this café.");
        }

        int availableCredits = contract.RemainingCredits + (contract.AllowOverdraft ? contract.OverdraftLimit : 0);
        if (availableCredits < 1)
        {
            return ApiResponse<OrderDto>.FailureResult($"Student '@{student.Username}' has completely exhausted all pass credits.");
        }

        // Determine product & upgrade math
        string productName = "Counter Walk-in Standard Meal";
        decimal itemPrice = contract.ContractPlan.BaseMealCreditValue;
        decimal baseValue = contract.ContractPlan.BaseMealCreditValue;
        int? productId = null;

        if (request.ProductId.HasValue)
        {
            var product = await _context.Products.FirstOrDefaultAsync(p => p.Id == request.ProductId.Value && p.CafeId == cafeId);
            if (product != null)
            {
                productId = product.Id;
                productName = product.Name;
                itemPrice = product.Price;
            }
        }

        decimal discount = Math.Min(itemPrice, baseValue);
        decimal upgradeCashDue = itemPrice - discount;

        // Decrement 1 credit
        contract.RemainingCredits -= 1;
        if (contract.RemainingCredits <= 0 && !contract.AllowOverdraft)
        {
            contract.Status = ContractStatus.Depleted;
        }

        var orderNumber = $"WLK-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..4].ToUpper()}";
        var order = new Order
        {
            OrderNumber = orderNumber,
            CustomerId = student.Id,
            CafeId = cafeId,
            Status = OrderStatus.Delivered,
            PaymentMethod = PaymentMethod.MealContract,
            Subtotal = itemPrice,
            DeliveryFee = 0.00m,
            DiscountAmount = discount,
            TotalAmount = upgradeCashDue,
            DeliveryLocation = "Counter (Walk-in)",
            SpecialNotes = request.Notes ?? "Walk-in pass deduction by cashier",
            CreatedAt = DateTime.UtcNow,
            OrderItems = new List<OrderItem>
            {
                new()
                {
                    ProductId = productId ?? 0,
                    ProductName = productName,
                    UnitPrice = itemPrice,
                    Quantity = 1,
                    ItemTotal = itemPrice
                }
            }
        };

        await _context.Orders.AddAsync(order);
        await _context.SaveChangesAsync();

        var usageLog = new ContractUsageLog
        {
            StudentContractId = contract.Id,
            OrderId = order.Id,
            CreditsDeducted = 1,
            RemainingCreditsAfter = contract.RemainingCredits,
            UsedAt = DateTime.UtcNow
        };
        await _context.ContractUsageLogs.AddAsync(usageLog);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Counter punch order {OrderNumber} created for student {StudentUsername} at Cafe {CafeId}", order.OrderNumber, student.Username, cafeId);

        // Instant SignalR push alert to the student's phone
        await _hubContext.Clients.Group($"user_{student.Id}").SendAsync("PassDeductedForWalkIn", new
        {
            OrderNumber = order.OrderNumber,
            CafeName = cafe.Name,
            MealName = productName,
            UpgradeCashPortion = upgradeCashDue,
            RemainingCredits = contract.RemainingCredits
        });

        var orderDto = new OrderDto
        {
            Id = order.Id,
            OrderNumber = order.OrderNumber,
            CustomerId = student.Id,
            CustomerName = student.FullName,
            CafeId = cafe.Id,
            CafeName = cafe.Name,
            Status = order.Status,
            PaymentMethod = order.PaymentMethod,
            Subtotal = order.Subtotal,
            DiscountAmount = order.DiscountAmount,
            TotalAmount = order.TotalAmount,
            DeliveryLocation = order.DeliveryLocation,
            SpecialNotes = order.SpecialNotes,
            CreatedAt = order.CreatedAt,
            Items = order.OrderItems.Select(oi => new OrderItemDto
            {
                Id = oi.Id,
                ProductId = oi.ProductId,
                ProductName = oi.ProductName,
                UnitPrice = oi.UnitPrice,
                Quantity = oi.Quantity,
                ItemTotal = oi.ItemTotal
            }).ToList()
        };

        await _hubContext.Clients.Group($"cafe_{cafeId}").SendAsync("OrderCreated", orderDto);

        return ApiResponse<OrderDto>.SuccessResult(orderDto, $"Successfully punched 1 meal credit for @{student.Username}.");
    }

    public async Task<ApiResponse<UserDto>> AddStaffMemberAsync(int cafeId, string staffUsername, int ownerUserId)
    {
        var cafe = await _context.Cafes.FirstOrDefaultAsync(c => c.Id == cafeId);
        if (cafe == null)
        {
            return ApiResponse<UserDto>.FailureResult("Café was not found.");
        }

        if (cafe.OwnerId != ownerUserId)
        {
            return ApiResponse<UserDto>.FailureResult("Only the café owner can add staff members.");
        }

        var staffUser = await _context.Users.FirstOrDefaultAsync(u => u.Username.ToLower() == staffUsername.Trim().ToLowerInvariant());
        if (staffUser == null)
        {
            return ApiResponse<UserDto>.FailureResult($"User '@{staffUsername}' was not found.");
        }

        staffUser.Role = UserRole.CafeStaff;
        staffUser.EmployedAtCafeId = cafeId;
        await _context.SaveChangesAsync();

        return ApiResponse<UserDto>.SuccessResult(new UserDto
        {
            Id = staffUser.Id,
            Username = staffUser.Username,
            FullName = staffUser.FullName,
            Email = staffUser.Email,
            Role = staffUser.Role,
            EmployedAtCafeId = staffUser.EmployedAtCafeId,
            CreatedAt = staffUser.CreatedAt
        }, $"@{staffUser.Username} has been added as kitchen staff to {cafe.Name}.");
    }

    public async Task<ApiResponse<List<UserDto>>> GetStaffMembersAsync(int cafeId, int ownerUserId)
    {
        var cafe = await _context.Cafes.FirstOrDefaultAsync(c => c.Id == cafeId);
        if (cafe == null)
        {
            return ApiResponse<List<UserDto>>.FailureResult("Café was not found.");
        }

        if (cafe.OwnerId != ownerUserId)
        {
            return ApiResponse<List<UserDto>>.FailureResult("Only the café owner can view staff members.");
        }

        var staff = await _context.Users
            .AsNoTracking()
            .Where(u => u.EmployedAtCafeId == cafeId)
            .Select(u => new UserDto
            {
                Id = u.Id,
                Username = u.Username,
                FullName = u.FullName,
                Email = u.Email,
                Role = u.Role,
                EmployedAtCafeId = u.EmployedAtCafeId,
                CreatedAt = u.CreatedAt
            })
            .ToListAsync();

        return ApiResponse<List<UserDto>>.SuccessResult(staff);
    }

    private static CafeDto MapToDto(Cafe c, bool includeMenu)
    {
        var dto = new CafeDto
        {
            Id = c.Id,
            Name = c.Name,
            Description = c.Description,
            Location = c.Location,
            LogoUrl = c.LogoUrl,
            DeliveryFee = c.DeliveryFee,
            MinimumOrderAmount = c.MinimumOrderAmount,
            IsActive = c.IsActive,
            Announcement = c.Announcement,
            UniversityId = c.UniversityId,
            UniversityName = c.University?.Name,
            OwnerId = c.OwnerId,
            OwnerName = c.Owner?.FullName
        };

        if (includeMenu)
        {
            dto.Categories = c.Categories.Select(cat => new CategoryDto
            {
                Id = cat.Id,
                CafeId = cat.CafeId,
                Name = cat.Name,
                DisplayOrder = cat.DisplayOrder
            }).ToList();

            dto.Products = c.Products.Select(p => new ProductDto
            {
                Id = p.Id,
                CafeId = p.CafeId,
                CategoryId = p.CategoryId,
                CategoryName = p.Category?.Name ?? string.Empty,
                Name = p.Name,
                Description = p.Description,
                Price = p.Price,
                ImageUrl = p.ImageUrl,
                StockQuantity = p.StockQuantity,
                IsAvailable = p.IsAvailable
            }).ToList();
        }

        return dto;
    }
}

using CampusBite.Api.Services.Interfaces;
using CampusBite.Shared.DTOs;
using CampusBite.Shared.Requests;
using CampusBite.Shared.Responses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CampusBite.Api.Controllers;

public class ProductsController : BaseApiController
{
    private readonly IProductService _productService;
    private readonly ICafeService _cafeService;
    private readonly IAuthorizationService _authorizationService;

    public ProductsController(
        IProductService productService,
        ICafeService cafeService,
        IAuthorizationService authorizationService)
    {
        _productService = productService;
        _cafeService = cafeService;
        _authorizationService = authorizationService;
    }

    [HttpGet("cafe/{cafeId}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<List<ProductDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetProductsByCafe(int cafeId)
    {
        var result = await _productService.GetProductsByCafeAsync(cafeId);
        return HandleResult(result);
    }

    [HttpGet("{id}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<ProductDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<ProductDto>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetProductById(int id)
    {
        var result = await _productService.GetProductByIdAsync(id);
        return HandleResult(result);
    }

    [HttpPost]
    [Authorize(Roles = "CafeOwner,Admin")]
    [ProducesResponseType(typeof(ApiResponse<ProductDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> CreateProduct([FromBody] CreateProductRequest request)
    {
        // 1. Fetch the target Café to evaluate ownership
        var cafe = await _cafeService.GetCafeEntityByIdAsync(request.CafeId);
        if (cafe == null)
        {
            return NotFound(ApiResponse<ProductDto>.FailureResult("Target café was not found."));
        }

        // 2. Resource-Based Ownership Policy Check (from security course!)
        var authResult = await _authorizationService.AuthorizeAsync(User, cafe, "CafeOwnerOrAdmin");
        if (!authResult.Succeeded)
        {
            return Forbid(); // HTTP 403: You cannot add products to another owner's café!
        }

        var result = await _productService.CreateProductAsync(request);
        return HandleResult(result);
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "CafeOwner,Admin")]
    [ProducesResponseType(typeof(ApiResponse<ProductDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> UpdateProduct(int id, [FromBody] UpdateProductRequest request)
    {
        var product = await _productService.GetProductEntityByIdAsync(id);
        if (product == null)
        {
            return NotFound(ApiResponse<ProductDto>.FailureResult("Product not found."));
        }

        // Resource-Based Ownership Policy Check on the product's café
        var authResult = await _authorizationService.AuthorizeAsync(User, product.Cafe, "CafeOwnerOrAdmin");
        if (!authResult.Succeeded)
        {
            return Forbid(); // HTTP 403
        }

        var result = await _productService.UpdateProductAsync(id, request);
        return HandleResult(result);
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "CafeOwner,Admin")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> DeleteProduct(int id)
    {
        var product = await _productService.GetProductEntityByIdAsync(id);
        if (product == null)
        {
            return NotFound(ApiResponse<bool>.FailureResult("Product not found."));
        }

        // Resource-Based Ownership Policy Check
        var authResult = await _authorizationService.AuthorizeAsync(User, product.Cafe, "CafeOwnerOrAdmin");
        if (!authResult.Succeeded)
        {
            return Forbid(); // HTTP 403
        }

        var result = await _productService.DeleteProductAsync(id);
        return HandleResult(result);
    }

    [HttpPatch("{id}/stock")]
    [Authorize(Roles = "CafeOwner,Admin")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> UpdateStock(int id, [FromBody] int? newStock)
    {
        var product = await _productService.GetProductEntityByIdAsync(id);
        if (product == null)
        {
            return NotFound(ApiResponse<bool>.FailureResult("Product not found."));
        }

        // Resource-Based Ownership Policy Check
        var authResult = await _authorizationService.AuthorizeAsync(User, product.Cafe, "CafeOwnerOrAdmin");
        if (!authResult.Succeeded)
        {
            return Forbid(); // HTTP 403
        }

        var result = await _productService.UpdateStockAsync(id, newStock);
        return HandleResult(result);
    }
}

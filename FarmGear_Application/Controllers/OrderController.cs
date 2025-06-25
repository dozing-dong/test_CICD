using FarmGear_Application.DTOs;
using FarmGear_Application.DTOs.Orders;
using FarmGear_Application.Models;
using FarmGear_Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FarmGear_Application.Controllers;

[ApiController]
[Route("api/[controller]")]
public class OrderController : ControllerBase
{
  private readonly IOrderService _orderService;

  public OrderController(IOrderService orderService)
  {
    _orderService = orderService;
  }
  //这个方法用于创建订单
  [HttpPost]
  [Authorize]
  public async Task<ApiResponse<OrderViewDto>> CreateOrder([FromBody] CreateOrderRequest request)
  {
    var renterId = User.FindFirst("sub")?.Value;
    if (string.IsNullOrEmpty(renterId))
    {
      return new ApiResponse<OrderViewDto> { Success = false, Message = "Failed to get user information" };
    }
    return await _orderService.CreateOrderAsync(request, renterId);
  }
  //这个方法用于获取订单列表
  [HttpGet]
  [Authorize]
  public async Task<ApiResponse<PaginatedList<OrderViewDto>>> GetOrders([FromQuery] OrderQueryParameters parameters)
  {
    var userId = User.FindFirst("sub")?.Value;
    if (string.IsNullOrEmpty(userId))
    {
      return new ApiResponse<PaginatedList<OrderViewDto>> { Success = false, Message = "Failed to get user information" };
    }
    var isAdmin = User.IsInRole("Admin");
    return await _orderService.GetOrdersAsync(parameters, userId, isAdmin);
  }
  //这个方法用于获取订单详情
  [HttpGet("{id}")]
  [Authorize]
  public async Task<ApiResponse<OrderViewDto>> GetOrderById(string id)
  {
    var userId = User.FindFirst("sub")?.Value;
    if (string.IsNullOrEmpty(userId))
    {
      return new ApiResponse<OrderViewDto> { Success = false, Message = "Failed to get user information" };
    }
    var isAdmin = User.IsInRole("Admin");
    return await _orderService.GetOrderByIdAsync(id, userId, isAdmin);
  }
  //这个方法用于更新订单状态
  [HttpPut("{id}/status")]
  [Authorize(Roles = "Provider,Official,Admin")]
  public async Task<ApiResponse<OrderViewDto>> UpdateOrderStatus(string id, [FromBody] OrderStatus status)
  {
    var userId = User.FindFirst("sub")?.Value;
    if (string.IsNullOrEmpty(userId))
    {
      return new ApiResponse<OrderViewDto> { Success = false, Message = "Failed to get user information" };
    }
    var isAdmin = User.IsInRole("Admin");
    return await _orderService.UpdateOrderStatusAsync(id, status, userId, isAdmin);
  }
  //这个方法用于取消订单
  [HttpPut("{id}/cancel")]
  [Authorize]
  public async Task<ApiResponse<OrderViewDto>> CancelOrder(string id)
  {
    var userId = User.FindFirst("sub")?.Value;
    if (string.IsNullOrEmpty(userId))
    {
      return new ApiResponse<OrderViewDto> { Success = false, Message = "Failed to get user information" };
    }
    var isAdmin = User.IsInRole("Admin");
    return await _orderService.CancelOrderAsync(id, userId, isAdmin);
  }
}
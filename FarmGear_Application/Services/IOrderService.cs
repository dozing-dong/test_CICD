using FarmGear_Application.DTOs;
using FarmGear_Application.DTOs.Orders;
using FarmGear_Application.Models;

namespace FarmGear_Application.Services;

/// <summary>
/// 订单服务接口
/// </summary>
public interface IOrderService
{
  /// <summary>
  /// 创建订单
  /// </summary>
  /// <param name="request">创建订单请求</param>
  /// <param name="renterId">租客ID</param>
  /// <returns>创建的订单</returns>
  Task<ApiResponse<OrderViewDto>> CreateOrderAsync(CreateOrderRequest request, string renterId);

  /// <summary>
  /// 获取订单详情
  /// </summary>
  /// <param name="id">订单ID</param>
  /// <param name="userId">用户ID</param>
  /// <param name="isAdmin">是否为管理员</param>
  /// <returns>订单详情</returns>
  Task<ApiResponse<OrderViewDto>> GetOrderByIdAsync(string id, string userId, bool isAdmin);

  /// <summary>
  /// 获取订单列表
  /// </summary>
  /// <param name="parameters">查询参数</param>
  /// <param name="userId">用户ID</param>
  /// <param name="isAdmin">是否为管理员</param>
  /// <returns>分页订单列表</returns>
  Task<ApiResponse<PaginatedList<OrderViewDto>>> GetOrdersAsync(OrderQueryParameters parameters, string userId, bool isAdmin);

  /// <summary>
  /// 更新订单状态
  /// </summary>
  /// <param name="id">订单ID</param>
  /// <param name="status">新状态</param>
  /// <param name="userId">用户ID</param>
  /// <param name="isAdmin">是否为管理员</param>
  /// <returns>更新后的订单</returns>
  Task<ApiResponse<OrderViewDto>> UpdateOrderStatusAsync(string id, OrderStatus status, string userId, bool isAdmin);

  /// <summary>
  /// 取消订单
  /// </summary>
  /// <param name="id">订单ID</param>
  /// <param name="userId">用户ID</param>
  /// <param name="isAdmin">是否为管理员</param>
  /// <returns>更新后的订单</returns>
  Task<ApiResponse<OrderViewDto>> CancelOrderAsync(string id, string userId, bool isAdmin);

  /// <summary>
  /// 检查设备在指定时间段是否可用
  /// </summary>
  /// <param name="equipmentId">设备ID</param>
  /// <param name="startDate">开始日期</param>
  /// <param name="endDate">结束日期</param>
  /// <returns>是否可用</returns>
  Task<bool> IsEquipmentAvailableAsync(string equipmentId, DateTime startDate, DateTime endDate);
}
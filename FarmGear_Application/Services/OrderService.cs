using FarmGear_Application.Data;
using FarmGear_Application.DTOs;
using FarmGear_Application.DTOs.Orders;
using FarmGear_Application.Models;
using Microsoft.EntityFrameworkCore;

namespace FarmGear_Application.Services;

/// <summary>
/// 订单服务实现
/// </summary>
public class OrderService : IOrderService
{
  private readonly ApplicationDbContext _context;
  private readonly ILogger<OrderService> _logger;

  public OrderService(ApplicationDbContext context, ILogger<OrderService> logger)
  {
    _context = context;
    _logger = logger;
  }

  /// <inheritdoc/>
  public async Task<ApiResponse<OrderViewDto>> CreateOrderAsync(CreateOrderRequest request, string renterId)
  {
    using var transaction = await _context.Database.BeginTransactionAsync();
    try
    {
      // 1. 验证设备是否存在且可用
      var equipment = await _context.Equipment
          .FirstOrDefaultAsync(e => e.Id == request.EquipmentId);
      if (equipment == null)
      {
        return new ApiResponse<OrderViewDto> { Success = false, Message = "Equipment not found" };
      }
      if (equipment.Status != EquipmentStatus.Available)
      {
        return new ApiResponse<OrderViewDto> { Success = false, Message = "Equipment is not available" };
      }

      // 2. 验证日期范围
      if (request.StartDate < DateTime.UtcNow.Date)
      {
        return new ApiResponse<OrderViewDto> { Success = false, Message = "Start date cannot be in the past" };
      }
      if (request.EndDate <= request.StartDate)
      {
        return new ApiResponse<OrderViewDto> { Success = false, Message = "End date must be after start date" };
      }

      // 3. 检查设备在指定时间段是否可用
      var isAvailable = await IsEquipmentAvailableAsync(request.EquipmentId, request.StartDate, request.EndDate);
      if (!isAvailable)
      {
        return new ApiResponse<OrderViewDto> { Success = false, Message = "Equipment is not available for the selected dates" };
      }

      // 4. 计算总金额
      var days = (request.EndDate - request.StartDate).Days;
      var totalAmount = equipment.DailyPrice * days;

      // 5. 创建订单
      var order = new Order
      {
        EquipmentId = request.EquipmentId,
        RenterId = renterId,
        StartDate = request.StartDate,
        EndDate = request.EndDate,
        TotalAmount = totalAmount,
        Status = OrderStatus.Pending,
        CreatedAt = DateTime.UtcNow
      };

      _context.Orders.Add(order);
      await _context.SaveChangesAsync();

      // 6. 更新设备状态
      equipment.Status = EquipmentStatus.Rented;
      await _context.SaveChangesAsync();

      await transaction.CommitAsync();

      // 7. 返回订单视图
      return await GetOrderByIdAsync(order.Id, renterId, false);
    }
    catch (Exception ex)
    {
      await transaction.RollbackAsync();
      _logger.LogError(ex, "Error creating order for equipment {EquipmentId}", request.EquipmentId);
      return new ApiResponse<OrderViewDto> { Success = false, Message = "An error occurred while creating the order" };
    }
  }

  /// <inheritdoc/>
  /// 这个方法用于根据订单ID获取订单详情,需要提供订单ID,用户ID,是否为管理员
  public async Task<ApiResponse<OrderViewDto>> GetOrderByIdAsync(string id, string userId, bool isAdmin)
  {
    try
    {
      var order = await _context.Orders
          .Include(o => o.Equipment)
          .Include(o => o.Renter)
          .FirstOrDefaultAsync(o => o.Id == id);

      if (order == null)
      {
        return new ApiResponse<OrderViewDto> { Success = false, Message = "Order not found" };
      }

      // 检查权限：只有订单的租客、设备所有者或管理员可以查看订单
      if (!isAdmin && order.RenterId != userId && order.Equipment?.OwnerId != userId)
      {
        return new ApiResponse<OrderViewDto> { Success = false, Message = "You are not authorized to view this order" };
      }

      if (order.Equipment == null || order.Renter == null)
      {
        _logger.LogError("Order {OrderId} data is incomplete", id);
        return new ApiResponse<OrderViewDto> { Success = false, Message = "Order data is incomplete" };
      }

      var orderDto = new OrderViewDto
      {
        Id = order.Id,
        EquipmentId = order.EquipmentId,
        EquipmentName = order.Equipment.Name,
        RenterId = order.RenterId,
        RenterName = order.Renter.UserName ?? string.Empty,
        StartDate = order.StartDate,
        EndDate = order.EndDate,
        TotalAmount = order.TotalAmount,
        Status = order.Status,
        CreatedAt = order.CreatedAt,
        UpdatedAt = order.UpdatedAt
      };

      return new ApiResponse<OrderViewDto> { Success = true, Data = orderDto };
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Error getting order {OrderId}", id);
      return new ApiResponse<OrderViewDto> { Success = false, Message = "An error occurred while getting the order" };
    }
  }

  /// <inheritdoc/>
  /// 这个方法用于获取订单列表,需要提供查询参数,用户ID,是否为管理员
  public async Task<ApiResponse<PaginatedList<OrderViewDto>>> GetOrdersAsync(OrderQueryParameters parameters, string userId, bool isAdmin)
  {
    try
    {
      // 构建基础查询
      var query = _context.Orders
          .Include(o => o.Equipment)
          .Include(o => o.Renter)
          .AsQueryable();

      // 如果不是管理员，只能查看自己的订单（作为租客或设备所有者）
      if (!isAdmin)
      {
        query = query.Where(o => o.RenterId == userId || o.Equipment!.OwnerId == userId);
      }

      // 往查询表达式树"追加条件"
      query = ApplyFilters(query, parameters);

      // 往查询表达式树"追加排序"
      query = ApplySorting(query, parameters);

      // 正式执行查询,携带之前追加的条件和排序.返回总记录数,比如 100 条
      var totalCount = await query.CountAsync();

      // 正式执行查询,携带之前追加的条件和排序.返回分页数据,比如 10 条
      var items = await query
          .Skip((parameters.PageNumber - 1) * parameters.PageSize)
          .Take(parameters.PageSize)
          //将查询结果映射为 OrderViewDto 列表
          .Select(o => new OrderViewDto
          {
            Id = o.Id,
            EquipmentId = o.EquipmentId,
            EquipmentName = o.Equipment!.Name,
            RenterId = o.RenterId,
            RenterName = o.Renter!.UserName ?? string.Empty,
            StartDate = o.StartDate,
            EndDate = o.EndDate,
            TotalAmount = o.TotalAmount,
            Status = o.Status,
            CreatedAt = o.CreatedAt,
            UpdatedAt = o.UpdatedAt
          })
          .ToListAsync();//这句是真正执行查询,返回一个 List<OrderViewDto>

      //将查询结果封装为 PaginatedList<OrderViewDto> 对象,把集合封装到items,总记录数封装到totalCount,当前页码封装到PageNumber,每页记录数封装到PageSize,然后再封装成一个大的
      //PaginatedList<OrderViewDto> 对象,返回给前端,item是一个包括了许多dto的实例对象的集合,PaginatedList<OrderViewDto>也是一个
      var result = new PaginatedList<OrderViewDto>(items, totalCount, parameters.PageNumber, parameters.PageSize);
      return new ApiResponse<PaginatedList<OrderViewDto>> { Success = true, Data = result };
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Error getting orders for user {UserId}", userId);
      return new ApiResponse<PaginatedList<OrderViewDto>> { Success = false, Message = "An error occurred while getting the orders" };
    }
  }

  /// <inheritdoc/>
  /// 这个方法用于更新订单状态,需要提供订单ID,新的状态,用户ID,是否为管理员
  public async Task<ApiResponse<OrderViewDto>> UpdateOrderStatusAsync(string id, OrderStatus status, string userId, bool isAdmin)
  {
    using var transaction = await _context.Database.BeginTransactionAsync();
    try
    {
      var order = await _context.Orders
      //这个Include是关联查询,查询订单的同时查询设备信息
          .Include(o => o.Equipment)
          //这个FirstOrDefaultAsync是查询单个订单,根据订单ID查询,经过这两步查询,order就包含了订单信息和设备信息
          .FirstOrDefaultAsync(o => o.Id == id);

      if (order == null)
      {
        return new ApiResponse<OrderViewDto> { Success = false, Message = "Order not found" };
      }
      //检查订单是否包含设备信息,如果订单不包含设备信息,则返回错误信息,这一步其实可以省略,因为前面已经关联查询了
      if (order.Equipment == null)
      {
        _logger.LogError("Order {OrderId} data is incomplete", id);
        return new ApiResponse<OrderViewDto> { Success = false, Message = "Order data is incomplete" };
      }

      // 检查权限：只有设备所有者或管理员可以更新订单状态
      if (!isAdmin && order.Equipment.OwnerId != userId)
      {
        return new ApiResponse<OrderViewDto> { Success = false, Message = "You are not authorized to update this order" };
      }

      // 验证状态转换是否合法,一个状态只能合法地转换为特定的"下一状态"，不能跳跃到任意状态。
      try
      {
        ValidateStatusTransition(order.Status, status);
      }
      catch (InvalidOperationException ex)
      {
        return new ApiResponse<OrderViewDto> { Success = false, Message = ex.Message };
      }

      // 状态机检测完成,更新订单的状态
      order.Status = status;
      order.UpdatedAt = DateTime.UtcNow;

      // 继续更新设备的状态
      UpdateEquipmentStatus(order, status);

      await _context.SaveChangesAsync();
      await transaction.CommitAsync();

      // 更新订单状态后,返回更新后的订单详情
      return await GetOrderByIdAsync(order.Id, userId, isAdmin);
    }
    catch (Exception ex)
    {
      await transaction.RollbackAsync();
      _logger.LogError(ex, "Error updating order {OrderId} status", id);
      return new ApiResponse<OrderViewDto> { Success = false, Message = "An error occurred while updating the order status" };
    }
  }

  /// <inheritdoc/>
  public async Task<ApiResponse<OrderViewDto>> CancelOrderAsync(string id, string userId, bool isAdmin)
  {
    using var transaction = await _context.Database.BeginTransactionAsync();
    try
    {
      var order = await _context.Orders
          .Include(o => o.Equipment)
          .FirstOrDefaultAsync(o => o.Id == id);

      if (order == null)
      {
        return new ApiResponse<OrderViewDto> { Success = false, Message = "Order not found" };
      }

      if (order.Equipment == null)
      {
        _logger.LogError("Order {OrderId} data is incomplete", id);
        return new ApiResponse<OrderViewDto> { Success = false, Message = "Order data is incomplete" };
      }

      // 检查权限：只有订单的租客、设备所有者或管理员可以取消订单
      if (!isAdmin && order.RenterId != userId && order.Equipment.OwnerId != userId)
      {
        return new ApiResponse<OrderViewDto> { Success = false, Message = "You are not authorized to cancel this order" };
      }

      // 只有待处理或已接受的订单可以取消
      if (order.Status != OrderStatus.Pending && order.Status != OrderStatus.Accepted)
      {
        return new ApiResponse<OrderViewDto> { Success = false, Message = "Only pending or accepted orders can be cancelled" };
      }

      // 检查原订单状态，如果是已接受，需要更新设备状态
      bool wasAccepted = order.Status == OrderStatus.Accepted;

      // 更新订单状态
      order.Status = OrderStatus.Cancelled;
      order.UpdatedAt = DateTime.UtcNow;

      // 如果原订单状态是已接受，更新设备状态为可用
      if (wasAccepted)
      {
        order.Equipment.Status = EquipmentStatus.Available;
      }

      await _context.SaveChangesAsync();
      await transaction.CommitAsync();

      return await GetOrderByIdAsync(order.Id, userId, isAdmin);
    }
    catch (Exception ex)
    {
      await transaction.RollbackAsync();
      _logger.LogError(ex, "Error cancelling order {OrderId}", id);
      return new ApiResponse<OrderViewDto> { Success = false, Message = "An error occurred while cancelling the order" };
    }
  }

  /// <inheritdoc/>
  public async Task<bool> IsEquipmentAvailableAsync(string equipmentId, DateTime startDate, DateTime endDate)
  {
    try
    {
      // 检查设备是否存在且可用
      var equipment = await _context.Equipment
          .FirstOrDefaultAsync(e => e.Id == equipmentId);
      if (equipment == null || equipment.Status != EquipmentStatus.Available)
      {
        return false;
      }

      // 检查指定时间段内是否有其他已接受的订单
      var hasOverlappingOrder = await _context.Orders
          .AnyAsync(o => o.EquipmentId == equipmentId
              && o.Status == OrderStatus.Accepted
              && ((o.StartDate <= startDate && o.EndDate > startDate)
                  || (o.StartDate < endDate && o.EndDate >= endDate)
                  || (o.StartDate >= startDate && o.EndDate <= endDate)));

      return !hasOverlappingOrder;
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Error checking equipment {EquipmentId} availability", equipmentId);
      return false;
    }
  }

  /// <summary>
  /// 应用过滤条件
  /// </summary>
  /// 封装了一系列对 query 的表达式树"追加条件"的步骤，每次 .Where(...) 都只是"挂一个节点"，并没有真正执行数据库查询。
  private static IQueryable<Order> ApplyFilters(IQueryable<Order> query, OrderQueryParameters parameters)
  {
    if (parameters.Status.HasValue)
    {
      query = query.Where(o => o.Status == parameters.Status.Value);
    }
    if (parameters.StartDateFrom.HasValue)
    {
      query = query.Where(o => o.StartDate >= parameters.StartDateFrom.Value);
    }
    if (parameters.StartDateTo.HasValue)
    {
      query = query.Where(o => o.StartDate <= parameters.StartDateTo.Value);
    }
    if (parameters.EndDateFrom.HasValue)
    {
      query = query.Where(o => o.EndDate >= parameters.EndDateFrom.Value);
    }
    if (parameters.EndDateTo.HasValue)
    {
      query = query.Where(o => o.EndDate <= parameters.EndDateTo.Value);
    }
    if (parameters.MinTotalAmount.HasValue)
    {
      query = query.Where(o => o.TotalAmount >= parameters.MinTotalAmount.Value);
    }
    if (parameters.MaxTotalAmount.HasValue)
    {
      query = query.Where(o => o.TotalAmount <= parameters.MaxTotalAmount.Value);
    }
    return query;
  }

  /// <summary>
  /// 应用排序
  /// </summary>
  private static IQueryable<Order> ApplySorting(IQueryable<Order> query, OrderQueryParameters parameters)
  {
    return parameters.SortBy?.ToLower() switch
    {
      "createdat" => parameters.IsAscending
          ? query.OrderBy(o => o.CreatedAt)
          : query.OrderByDescending(o => o.CreatedAt),
      "startdate" => parameters.IsAscending
          ? query.OrderBy(o => o.StartDate)
          : query.OrderByDescending(o => o.StartDate),
      "enddate" => parameters.IsAscending
          ? query.OrderBy(o => o.EndDate)
          : query.OrderByDescending(o => o.EndDate),
      "totalamount" => parameters.IsAscending
          ? query.OrderBy(o => o.TotalAmount)
          : query.OrderByDescending(o => o.TotalAmount),
      "status" => parameters.IsAscending
          ? query.OrderBy(o => o.Status)
          : query.OrderByDescending(o => o.Status),
      _ => query.OrderByDescending(o => o.CreatedAt)
    };
  }

  /// <summary>
  /// 更新设备状态
  /// </summary>
  private static void UpdateEquipmentStatus(Order order, OrderStatus newStatus)
  {
    if (order.Equipment == null) return;

    if (newStatus == OrderStatus.Accepted)
    {
      order.Equipment.Status = EquipmentStatus.Rented;
    }
    else if ((newStatus == OrderStatus.Rejected || newStatus == OrderStatus.Cancelled)
        && order.Equipment.Status == EquipmentStatus.Rented)
    {
      order.Equipment.Status = EquipmentStatus.Available;
    }
  }

  /// <summary>
  /// 验证订单状态转换是否合法
  /// </summary>
  private static void ValidateStatusTransition(OrderStatus currentStatus, OrderStatus newStatus)
  {
    var isValid = (currentStatus, newStatus) switch
    {
      // 待处理订单可以转换为已接受或已拒绝
      (OrderStatus.Pending, OrderStatus.Accepted) => true,
      (OrderStatus.Pending, OrderStatus.Rejected) => true,
      // 已接受订单可以转换为已完成或已取消
      (OrderStatus.Accepted, OrderStatus.Completed) => true,
      (OrderStatus.Accepted, OrderStatus.Cancelled) => true,
      // 其他状态转换都是非法的
      _ => false
    };

    if (!isValid)
    {
      throw new InvalidOperationException($"Invalid status transition from {currentStatus} to {newStatus}");
    }
  }
}
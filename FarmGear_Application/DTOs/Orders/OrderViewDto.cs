using FarmGear_Application.Models;

namespace FarmGear_Application.DTOs.Orders;

/// <summary>
/// 订单视图 DTO
/// </summary>
public class OrderViewDto
{
  /// <summary>
  /// 订单ID
  /// </summary>
  public string Id { get; set; } = string.Empty;

  /// <summary>
  /// 设备ID
  /// </summary>
  public string EquipmentId { get; set; } = string.Empty;

  /// <summary>
  /// 设备名称
  /// </summary>
  public string EquipmentName { get; set; } = string.Empty;

  /// <summary>
  /// 租客ID
  /// </summary>
  public string RenterId { get; set; } = string.Empty;

  /// <summary>
  /// 租客用户名
  /// </summary>
  public string RenterName { get; set; } = string.Empty;

  /// <summary>
  /// 开始日期
  /// </summary>
  public DateTime StartDate { get; set; }

  /// <summary>
  /// 结束日期
  /// </summary>
  public DateTime EndDate { get; set; }

  /// <summary>
  /// 总金额
  /// </summary>
  public decimal TotalAmount { get; set; }

  /// <summary>
  /// 订单状态
  /// </summary>
  public OrderStatus Status { get; set; }

  /// <summary>
  /// 创建时间
  /// </summary>
  public DateTime CreatedAt { get; set; }

  /// <summary>
  /// 更新时间
  /// </summary>
  public DateTime? UpdatedAt { get; set; }
}
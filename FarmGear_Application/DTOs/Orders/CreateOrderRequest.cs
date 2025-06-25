using System.ComponentModel.DataAnnotations;

namespace FarmGear_Application.DTOs.Orders;

/// <summary>
/// 创建订单请求 DTO
/// </summary>
public class CreateOrderRequest
{
  /// <summary>
  /// 设备ID
  /// </summary>
  [Required(ErrorMessage = "Equipment ID is required")]
  public string EquipmentId { get; set; } = string.Empty;

  /// <summary>
  /// 开始日期
  /// </summary>
  [Required(ErrorMessage = "Start date is required")]
  public DateTime StartDate { get; set; }

  /// <summary>
  /// 结束日期
  /// </summary>
  [Required(ErrorMessage = "End date is required")]
  public DateTime EndDate { get; set; }
}
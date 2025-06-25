using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FarmGear_Application.Models;

/// <summary>
/// 订单实体
/// </summary>
public class Order
{
  /// <summary>
  /// 订单ID
  /// </summary>
  [Key]
  public string Id { get; set; } = Guid.NewGuid().ToString();

  /// <summary>
  /// 设备ID
  /// </summary>
  [Required]
  public string EquipmentId { get; set; } = string.Empty;

  /// <summary>
  /// 设备
  /// </summary>
  [ForeignKey("EquipmentId")]
  public Equipment? Equipment { get; set; }

  /// <summary>
  /// 租客ID
  /// </summary>
  [Required]
  public string RenterId { get; set; } = string.Empty;

  /// <summary>
  /// 租客
  /// </summary>
  [ForeignKey("RenterId")]
  public AppUser? Renter { get; set; }

  /// <summary>
  /// 开始日期
  /// </summary>
  [Required]
  public DateTime StartDate { get; set; }

  /// <summary>
  /// 结束日期
  /// </summary>
  [Required]
  public DateTime EndDate { get; set; }

  /// <summary>
  /// 订单状态
  /// </summary>
  [Required]
  public OrderStatus Status { get; set; } = OrderStatus.Pending;

  /// <summary>
  /// 总金额
  /// </summary>
  [Required]
  [Column(TypeName = "decimal(18,2)")]
  public decimal TotalAmount { get; set; }

  /// <summary>
  /// 创建时间
  /// </summary>
  [Required]
  public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

  /// <summary>
  /// 更新时间
  /// </summary>
  public DateTime? UpdatedAt { get; set; }
}
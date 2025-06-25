using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FarmGear_Application.Models;

/// <summary>
/// 评论模型
/// </summary>
public class Review
{
  /// <summary>
  /// 评论ID
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
  [ForeignKey(nameof(EquipmentId))]
  public Equipment? Equipment { get; set; }

  /// <summary>
  /// 订单ID
  /// </summary>
  [Required]
  public string OrderId { get; set; } = string.Empty;

  /// <summary>
  /// 订单
  /// </summary>
  [ForeignKey(nameof(OrderId))]
  public Order? Order { get; set; }

  /// <summary>
  /// 用户ID
  /// </summary>
  [Required]
  public string UserId { get; set; } = string.Empty;

  /// <summary>
  /// 用户
  /// </summary>
  [ForeignKey(nameof(UserId))]
  public AppUser? User { get; set; }

  /// <summary>
  /// 评分（1-5）
  /// </summary>
  [Required]
  [Range(1, 5)]
  public int Rating { get; set; }

  /// <summary>
  /// 评论内容
  /// </summary>
  [MaxLength(500)]
  public string? Content { get; set; }

  /// <summary>
  /// 创建时间
  /// </summary>
  public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

  /// <summary>
  /// 更新时间
  /// </summary>
  public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
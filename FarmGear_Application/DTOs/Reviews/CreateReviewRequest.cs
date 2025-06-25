using System.ComponentModel.DataAnnotations;

namespace FarmGear_Application.DTOs.Reviews;

/// <summary>
/// 创建评论请求
/// </summary>
public class CreateReviewRequest
{
  /// <summary>
  /// 设备ID
  /// </summary>
  [Required]
  public string EquipmentId { get; set; } = string.Empty;

  /// <summary>
  /// 订单ID
  /// </summary>
  [Required]
  public string OrderId { get; set; } = string.Empty;

  /// <summary>
  /// 评分（1-5）
  /// </summary>
  [Required]
  [Range(1, 5)]
  public int Rating { get; set; }

  /// <summary>
  /// 评论内容（可选，最大500字符）
  /// </summary>
  [MaxLength(500)]
  public string? Content { get; set; }
}
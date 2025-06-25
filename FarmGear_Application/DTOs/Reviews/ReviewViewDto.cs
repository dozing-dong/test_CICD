namespace FarmGear_Application.DTOs.Reviews;

/// <summary>
/// 评论视图DTO
/// </summary>
public class ReviewViewDto
{
  /// <summary>
  /// 评论ID
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
  /// 订单ID
  /// </summary>
  public string OrderId { get; set; } = string.Empty;

  /// <summary>
  /// 用户ID
  /// </summary>
  public string UserId { get; set; } = string.Empty;

  /// <summary>
  /// 用户名
  /// </summary>
  public string UserName { get; set; } = string.Empty;

  /// <summary>
  /// 评分（1-5）
  /// </summary>
  public int Rating { get; set; }

  /// <summary>
  /// 评论内容
  /// </summary>
  public string? Content { get; set; }

  /// <summary>
  /// 创建时间
  /// </summary>
  public DateTime CreatedAt { get; set; }

  /// <summary>
  /// 更新时间
  /// </summary>
  public DateTime UpdatedAt { get; set; }
}
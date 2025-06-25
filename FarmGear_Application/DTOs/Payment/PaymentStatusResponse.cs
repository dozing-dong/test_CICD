namespace FarmGear_Application.DTOs.Payment;

/// <summary>
/// 支付状态响应
/// </summary>
public class PaymentStatusResponse
{
  /// <summary>
  /// 支付记录ID
  /// </summary>
  public string Id { get; set; } = string.Empty;

  /// <summary>
  /// 订单ID
  /// </summary>
  public string OrderId { get; set; } = string.Empty;

  /// <summary>
  /// 支付金额
  /// </summary>
  public decimal Amount { get; set; }

  /// <summary>
  /// 支付状态
  /// </summary>
  public PaymentStatus Status { get; set; }

  /// <summary>
  /// 支付时间
  /// </summary>
  public DateTime? PaidAt { get; set; }

  /// <summary>
  /// 创建时间
  /// </summary>
  public DateTime CreatedAt { get; set; }

  /// <summary>
  /// 支付用户ID
  /// </summary>
  public string UserId { get; set; } = string.Empty;

  /// <summary>
  /// 支付用户名称
  /// </summary>
  public string UserName { get; set; } = string.Empty;

  /// <summary>
  /// 支付跳转URL
  /// </summary>
  public string? PaymentUrl { get; set; }
}
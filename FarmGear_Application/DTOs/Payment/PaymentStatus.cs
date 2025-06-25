namespace FarmGear_Application.DTOs.Payment;

/// <summary>
/// 支付状态枚举
/// </summary>
public enum PaymentStatus
{
  /// <summary>
  /// 待支付
  /// </summary>
  Pending,

  /// <summary>
  /// 支付成功
  /// </summary>
  Paid,

  /// <summary>
  /// 支付失败
  /// </summary>
  Failed
}
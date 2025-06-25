namespace FarmGear_Application.Models;

/// <summary>
/// 订单状态枚举
/// </summary>
public enum OrderStatus
{
  /// <summary>
  /// 待处理
  /// </summary>
  Pending = 0,

  /// <summary>
  /// 已接受
  /// </summary>
  Accepted = 1,

  /// <summary>
  /// 已拒绝
  /// </summary>
  Rejected = 2,

  /// <summary>
  /// 已完成
  /// </summary>
  Completed = 3,

  /// <summary>
  /// 已取消
  /// </summary>
  Cancelled = 4
}
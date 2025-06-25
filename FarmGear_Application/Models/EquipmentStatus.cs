namespace FarmGear_Application.Models;

/// <summary>
/// 设备状态枚举
/// </summary>
public enum EquipmentStatus
{
  /// <summary>
  /// 可用
  /// </summary>
  Available = 0,

  /// <summary>
  /// 已租出
  /// </summary>
  Rented = 1,

  /// <summary>
  /// 维护中
  /// </summary>
  Maintenance = 2,

  /// <summary>
  /// 已下架
  /// </summary>
  Offline = 3
}
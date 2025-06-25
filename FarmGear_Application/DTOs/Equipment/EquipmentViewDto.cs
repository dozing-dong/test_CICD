using FarmGear_Application.Models;

namespace FarmGear_Application.DTOs.Equipment;

/// <summary>
/// 设备视图 DTO,供方法
/// </summary>
public class EquipmentViewDto
{
  /// <summary>
  /// 设备ID
  /// </summary>
  public string Id { get; set; } = string.Empty;

  /// <summary>
  /// 设备名称
  /// </summary>
  public string Name { get; set; } = string.Empty;

  /// <summary>
  /// 设备描述
  /// </summary>
  public string Description { get; set; } = string.Empty;

  /// <summary>
  /// 日租金（元）
  /// </summary>
  public decimal DailyPrice { get; set; }

  /// <summary>
  /// 纬度
  /// </summary>
  public double Latitude { get; set; }

  /// <summary>
  /// 经度
  /// </summary>
  public double Longitude { get; set; }

  /// <summary>
  /// 设备状态
  /// </summary>
  public EquipmentStatus Status { get; set; }

  /// <summary>
  /// 所有者ID
  /// </summary>
  public string OwnerId { get; set; } = string.Empty;

  /// <summary>
  /// 所有者用户名
  /// </summary>
  public string OwnerUsername { get; set; } = string.Empty;

  /// <summary>
  /// 设备类型
  /// </summary>
  public string Type { get; set; } = string.Empty;

  /// <summary>
  /// 创建时间
  /// </summary>
  public DateTime CreatedAt { get; set; }
}
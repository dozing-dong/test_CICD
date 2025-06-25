using FarmGear_Application.Models;

namespace FarmGear_Application.DTOs.Location;

/// <summary>
/// 设备位置信息DTO
/// </summary>
public class EquipmentLocationDto
{
  /// <summary>
  /// 设备ID
  /// </summary>
  public string Id { get; set; } = null!;

  /// <summary>
  /// 设备名称
  /// </summary>
  public string Name { get; set; } = null!;

  /// <summary>
  /// 纬度
  /// </summary>
  public double Latitude { get; set; }

  /// <summary>
  /// 经度
  /// </summary>
  public double Longitude { get; set; }

  /// <summary>
  /// 到中心点的距离（米）
  /// </summary>
  public double Distance { get; set; }

  /// <summary>
  /// 日租金
  /// </summary>
  public decimal DailyPrice { get; set; }

  /// <summary>
  /// 设备状态
  /// </summary>
  public EquipmentStatus Status { get; set; }

  /// <summary>
  /// 所有者名称
  /// </summary>
  public string OwnerName { get; set; } = null!;
}
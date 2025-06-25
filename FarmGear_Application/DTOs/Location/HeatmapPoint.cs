namespace FarmGear_Application.DTOs.Location;

/// <summary>
/// 热力图点数据
/// </summary>
public class HeatmapPoint
{
  /// <summary>
  /// 纬度
  /// </summary>
  public double Latitude { get; set; }

  /// <summary>
  /// 经度
  /// </summary>
  public double Longitude { get; set; }

  /// <summary>
  /// 权重（用于热力图显示）
  /// </summary>
  public double Weight { get; set; }

  /// <summary>
  /// 该点设备数量
  /// </summary>
  public int EquipmentCount { get; set; }
}
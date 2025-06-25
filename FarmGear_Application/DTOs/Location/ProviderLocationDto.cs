namespace FarmGear_Application.DTOs.Location;

/// <summary>
/// 供应商位置信息DTO
/// </summary>
public class ProviderLocationDto
{
  /// <summary>
  /// 供应商ID
  /// </summary>
  public string ProviderId { get; set; } = null!;

  /// <summary>
  /// 供应商名称
  /// </summary>
  public string ProviderName { get; set; } = null!;

  /// <summary>
  /// 纬度
  /// </summary>
  public double Latitude { get; set; }

  /// <summary>
  /// 经度
  /// </summary>
  public double Longitude { get; set; }

  /// <summary>
  /// 设备总数
  /// </summary>
  public int EquipmentCount { get; set; }

  /// <summary>
  /// 可用设备数量
  /// </summary>
  public int AvailableCount { get; set; }
}
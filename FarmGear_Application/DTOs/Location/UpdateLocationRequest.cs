using System.ComponentModel.DataAnnotations;

namespace FarmGear_Application.DTOs.Location;

/// <summary>
/// 更新位置请求
/// </summary>
public class UpdateLocationRequest
{
  /// <summary>
  /// 纬度
  /// </summary>
  [Required]
  [Range(-90, 90, ErrorMessage = "Latitude must be between -90 and 90 degrees")]
  public double Latitude { get; set; }

  /// <summary>
  /// 经度
  /// </summary>
  [Required]
  [Range(-180, 180, ErrorMessage = "Longitude must be between -180 and 180 degrees")]
  public double Longitude { get; set; }
}
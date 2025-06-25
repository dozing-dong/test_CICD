using System.ComponentModel.DataAnnotations;
using FarmGear_Application.Models;

namespace FarmGear_Application.DTOs.Location;

/// <summary>
/// 位置查询参数
/// </summary>
public class LocationQueryParameters
{
  /// <summary>
  /// 中心点纬度
  /// </summary>
  [Required]
  [Range(-90, 90, ErrorMessage = "Latitude must be between -90 and 90 degrees")]
  public double Latitude { get; set; }

  /// <summary>
  /// 中心点经度
  /// </summary>
  [Required]
  [Range(-180, 180, ErrorMessage = "Longitude must be between -180 and 180 degrees")]
  public double Longitude { get; set; }

  /// <summary>
  /// 搜索半径（米）
  /// </summary>
  [Required]
  [Range(100, 10000, ErrorMessage = "Search radius must be between 100 and 10000 meters")]
  public double Radius { get; set; }

  /// <summary>
  /// 页码
  /// </summary>
  [Range(1, int.MaxValue, ErrorMessage = "Page number must be greater than 0")]
  public int PageNumber { get; set; } = 1;

  /// <summary>
  /// 每页数量
  /// </summary>
  [Range(1, 100, ErrorMessage = "Page size must be between 1 and 100")]
  public int PageSize { get; set; } = 10;

  /// <summary>
  /// 最低价格
  /// </summary>
  [Range(0, double.MaxValue, ErrorMessage = "Minimum price must be greater than or equal to 0")]
  public decimal? MinPrice { get; set; }

  /// <summary>
  /// 最高价格
  /// </summary>
  [Range(0, double.MaxValue, ErrorMessage = "Maximum price must be greater than or equal to 0")]
  public decimal? MaxPrice { get; set; }

  /// <summary>
  /// 设备类型
  /// </summary>
  public string? EquipmentType { get; set; }

  /// <summary>
  /// 设备状态
  /// </summary>
  public EquipmentStatus? Status { get; set; }
}
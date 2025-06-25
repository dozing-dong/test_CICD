using FarmGear_Application.Models;

namespace FarmGear_Application.DTOs.Equipment;

/// <summary>
/// 设备查询参数
/// </summary>
public class EquipmentQueryParameters
{
  /// <summary>
  /// 页码（从1开始）
  /// </summary>
  public int PageNumber { get; set; } = 1;

  /// <summary>
  /// 每页大小
  /// </summary>
  public int PageSize { get; set; } = 10;

  /// <summary>
  /// 搜索关键词（设备名称或描述）
  /// </summary>
  public string? SearchTerm { get; set; }

  /// <summary>
  /// 最低日租金
  /// </summary>
  public decimal? MinDailyPrice { get; set; }

  /// <summary>
  /// 最高日租金
  /// </summary>
  public decimal? MaxDailyPrice { get; set; }

  /// <summary>
  /// 设备状态
  /// </summary>
  public EquipmentStatus? Status { get; set; }

  /// <summary>
  /// 排序字段
  /// </summary>
  public string? SortBy { get; set; }

  /// <summary>
  /// 是否升序
  /// </summary>
  public bool IsAscending { get; set; } = true;
}
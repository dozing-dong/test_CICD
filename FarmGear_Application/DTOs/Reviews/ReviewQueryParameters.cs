namespace FarmGear_Application.DTOs.Reviews;

/// <summary>
/// 评论查询参数
/// </summary>
public class ReviewQueryParameters
{
  private const int MaxPageSize = 50;
  private int _pageSize = 10;

  /// <summary>
  /// 页码（从1开始）
  /// </summary>
  public int PageNumber { get; set; } = 1;

  /// <summary>
  /// 每页大小（1-50）
  /// </summary>
  public int PageSize
  {
    get => _pageSize;
    set => _pageSize = Math.Min(value, MaxPageSize);
  }

  /// <summary>
  /// 设备ID
  /// </summary>
  public string? EquipmentId { get; set; }

  /// <summary>
  /// 用户ID
  /// </summary>
  public string? UserId { get; set; }

  /// <summary>
  /// 最低评分
  /// </summary>
  public int? MinRating { get; set; }

  /// <summary>
  /// 最高评分
  /// </summary>
  public int? MaxRating { get; set; }

  /// <summary>
  /// 开始时间
  /// </summary>
  public DateTime? StartDate { get; set; }

  /// <summary>
  /// 结束时间
  /// </summary>
  public DateTime? EndDate { get; set; }

  /// <summary>
  /// 排序字段（createdat, rating）
  /// </summary>
  public string? SortBy { get; set; }

  /// <summary>
  /// 是否升序
  /// </summary>
  public bool IsAscending { get; set; } = false;
}
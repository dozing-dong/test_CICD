using FarmGear_Application.Models;

namespace FarmGear_Application.DTOs.Orders;

/// <summary>
/// 订单查询参数
/// </summary>
public class OrderQueryParameters
{
  /// <summary>
  /// 页码
  /// </summary>
  public int PageNumber { get; set; } = 1;

  /// <summary>
  /// 每页大小
  /// </summary>
  public int PageSize { get; set; } = 10;

  /// <summary>
  /// 订单状态
  /// </summary>
  public OrderStatus? Status { get; set; }

  /// <summary>
  /// 开始日期范围（开始）
  /// </summary>
  public DateTime? StartDateFrom { get; set; }

  /// <summary>
  /// 开始日期范围（结束）
  /// </summary>
  public DateTime? StartDateTo { get; set; }

  /// <summary>
  /// 结束日期范围（开始）
  /// </summary>
  public DateTime? EndDateFrom { get; set; }

  /// <summary>
  /// 结束日期范围（结束）
  /// </summary>
  public DateTime? EndDateTo { get; set; }

  /// <summary>
  /// 最低总金额
  /// </summary>
  public decimal? MinTotalAmount { get; set; }

  /// <summary>
  /// 最高总金额
  /// </summary>
  public decimal? MaxTotalAmount { get; set; }

  /// <summary>
  /// 排序字段
  /// </summary>
  public string? SortBy { get; set; }

  /// <summary>
  /// 是否升序
  /// </summary>
  public bool IsAscending { get; set; } = true;
}
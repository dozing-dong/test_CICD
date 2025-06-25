using Microsoft.EntityFrameworkCore;

namespace FarmGear_Application.DTOs;

/// <summary>
/// 分页列表
/// </summary>
/// <typeparam name="T">列表项类型</typeparam>
public class PaginatedList<T>
{
  /// <summary>
  /// 当前页码
  /// </summary>
  public int PageNumber { get; }

  /// <summary>
  /// 每页大小
  /// </summary>
  public int PageSize { get; }

  /// <summary>
  /// 总页数
  /// </summary>
  public int TotalPages { get; }

  /// <summary>
  /// 总记录数
  /// </summary>
  public int TotalCount { get; }

  /// <summary>
  /// 是否有上一页
  /// </summary>
  public bool HasPreviousPage => PageNumber > 1;

  /// <summary>
  /// 是否有下一页
  /// </summary>
  public bool HasNextPage => PageNumber < TotalPages;

  /// <summary>
  /// 当前页数据
  /// </summary>
  public List<T> Items { get; }
  //构造函数,用于初始化分页数据

  public PaginatedList(List<T> items, int count, int pageNumber, int pageSize)
  {
    PageNumber = pageNumber;
    PageSize = pageSize;
    TotalPages = (int)Math.Ceiling(count / (double)pageSize);
    TotalCount = count;
    Items = items;
  }

  /// <summary>
  /// 创建分页列表
  /// </summary>
  /// <param name="source">数据源</param>
  /// <param name="pageNumber">页码</param>
  /// <param name="pageSize">每页大小</param>
  /// <returns>分页列表</returns>
  /// PaginatedList&lt;T&gt; 是一个你自定义的分页数据封装类
  /// CreateAsync 是一个静态方法,用于创建一个分页列表
  /// IQueryable&lt;T&gt; source 是一个数据源,它是一个可以延迟执行的查询
  /// int pageNumber 是页码
  /// int pageSize 是每页大小
  /// 
  /// 
  public static async Task<PaginatedList<T>> CreateAsync(
      IQueryable<T> source,
      int pageNumber,
      int pageSize)
  {
    //正式执行查询,获取总记录数
    var count = await source.CountAsync();
    //正式执行查询,获取分页数据,具体来说就是从source中跳过(pageNumber - 1) * pageSize条数据,然后取pageSize条数据
    //👉 例如:pageNumber = 1,pageSize = 10,那么就从source中跳过0条数据,然后取10条数据
    //👉 例如:pageNumber = 2,pageSize = 10,那么就从source中跳过10条数据,然后取10条数据
    var items = await source.Skip((pageNumber - 1) * pageSize)
                           //取pageSize条数据
                           .Take(pageSize)
                           //👉 最后将结果转换为列表
                           .ToListAsync();
    //返回分页数据,PaginatedList<T>是你自定义的分页数据封装类,这个方法一次执行等于返回你想要的某一页的数据,它就在本文件中
    return new PaginatedList<T>(items, count, pageNumber, pageSize);
  }
}
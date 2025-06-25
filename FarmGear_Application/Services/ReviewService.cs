using FarmGear_Application.Data;
using FarmGear_Application.DTOs;
using FarmGear_Application.DTOs.Reviews;
using FarmGear_Application.Models;
using Microsoft.EntityFrameworkCore;

namespace FarmGear_Application.Services;

/// <summary>
/// 评论服务实现
/// </summary>
public class ReviewService : IReviewService
{
  private readonly ApplicationDbContext _context;
  private readonly ILogger<ReviewService> _logger;

  public ReviewService(ApplicationDbContext context, ILogger<ReviewService> logger)
  {
    _context = context;
    _logger = logger;
  }

  /// <inheritdoc/>
  public async Task<ApiResponse<ReviewViewDto>> CreateReviewAsync(CreateReviewRequest request, string userId)
  {
    try
    {
      // 1. 验证设备是否存在
      var equipment = await _context.Equipment
          .FirstOrDefaultAsync(e => e.Id == request.EquipmentId);
      if (equipment == null)
      {
        return new ApiResponse<ReviewViewDto>
        {
          Success = false,
          Message = "Equipment not found"
        };
      }

      // 2. 验证订单是否存在且属于该用户
      if (!await IsOrderCompletedAndBelongsToUserAsync(request.OrderId, userId))
      {
        return new ApiResponse<ReviewViewDto>
        {
          Success = false,
          Message = "Order not found or does not belong to the user"
        };
      }

      // 3. 验证评分范围
      if (request.Rating < 1 || request.Rating > 5)
      {
        return new ApiResponse<ReviewViewDto>
        {
          Success = false,
          Message = "Rating must be between 1 and 5"
        };
      }

      // 4. 检查用户是否已评论过该设备
      if (await HasUserReviewedEquipmentAsync(request.EquipmentId, userId))
      {
        return new ApiResponse<ReviewViewDto>
        {
          Success = false,
          Message = "User has already reviewed this equipment"
        };
      }

      using var transaction = await _context.Database.BeginTransactionAsync();

      try
      {
        // 5. 创建评论
        var review = new Review
        {
          EquipmentId = request.EquipmentId,
          OrderId = request.OrderId,
          UserId = userId,
          Rating = request.Rating,
          Content = request.Content,
          CreatedAt = DateTime.UtcNow,
          UpdatedAt = DateTime.UtcNow
        };

        _context.Reviews.Add(review);

        // 6. 更新设备平均评分
        var averageRating = await _context.Reviews
            .Where(r => r.EquipmentId == request.EquipmentId)
            .AverageAsync(r => r.Rating);

        equipment.AverageRating = (decimal)averageRating;

        await _context.SaveChangesAsync();
        await transaction.CommitAsync();

        return new ApiResponse<ReviewViewDto>
        {
          Success = true,
          Message = "Review created successfully",
          Data = MapToViewDtoAsync(review)
        };
      }
      catch (Exception)
      {
        await transaction.RollbackAsync();
        throw;
      }
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Error creating review for equipment {EquipmentId}", request.EquipmentId);
      return new ApiResponse<ReviewViewDto>
      {
        Success = false,
        Message = "An error occurred while creating the review"
      };
    }
  }

  /// <inheritdoc/>
  public async Task<ApiResponse<PaginatedList<ReviewViewDto>>> GetReviewsAsync(ReviewQueryParameters parameters)
  {
    try
    {
      // 1. 构建基础查询
      var query = _context.Reviews
          .Include(r => r.Equipment)
          .Include(r => r.User)
          .AsQueryable();

      // 2. 应用过滤条件
      query = ApplyFilters(query, parameters);

      // 3. 应用排序
      query = ApplySorting(query, parameters);

      // 4. 获取总记录数
      var totalCount = await query.CountAsync();

      // 5. 获取分页数据
      var reviews = await query
          .Skip((parameters.PageNumber - 1) * parameters.PageSize)
          .Take(parameters.PageSize)
          .ToListAsync();

      var items = reviews.Select(MapToViewDtoAsync).ToList();

      var result = new PaginatedList<ReviewViewDto>(items, totalCount, parameters.PageNumber, parameters.PageSize);

      return new ApiResponse<PaginatedList<ReviewViewDto>>
      {
        Success = true,
        Data = result
      };
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Error getting reviews with parameters {@Parameters}", parameters);
      return new ApiResponse<PaginatedList<ReviewViewDto>>
      {
        Success = false,
        Message = "An error occurred while getting reviews"
      };
    }
  }

  /// <inheritdoc/>
  public async Task<ApiResponse<PaginatedList<ReviewViewDto>>> GetMyReviewsAsync(
      ReviewQueryParameters parameters,
      string userId)
  {
    try
    {
      // 1. 构建基础查询
      var query = _context.Reviews
          .Include(r => r.Equipment)
          .Include(r => r.User)
          .Where(r => r.UserId == userId)
          .AsQueryable();

      // 2. 应用过滤条件
      query = ApplyFilters(query, parameters);

      // 3. 应用排序
      query = ApplySorting(query, parameters);

      // 4. 获取总记录数
      var totalCount = await query.CountAsync();

      // 5. 获取分页数据
      var reviews = await query
          .Skip((parameters.PageNumber - 1) * parameters.PageSize)
          .Take(parameters.PageSize)
          .ToListAsync();

      var items = reviews.Select(MapToViewDtoAsync).ToList();

      var result = new PaginatedList<ReviewViewDto>(items, totalCount, parameters.PageNumber, parameters.PageSize);

      return new ApiResponse<PaginatedList<ReviewViewDto>>
      {
        Success = true,
        Data = result
      };
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Error getting reviews for user {UserId}", userId);
      return new ApiResponse<PaginatedList<ReviewViewDto>>
      {
        Success = false,
        Message = "An error occurred while getting user reviews"
      };
    }
  }

  /// <inheritdoc/>
  public async Task<ApiResponse<ReviewViewDto>> GetReviewByIdAsync(string id)
  {
    try
    {
      var review = await _context.Reviews
          .Include(r => r.Equipment)
          .Include(r => r.User)
          .FirstOrDefaultAsync(r => r.Id == id);

      if (review == null)
      {
        return new ApiResponse<ReviewViewDto>
        {
          Success = false,
          Message = "Review not found"
        };
      }

      return new ApiResponse<ReviewViewDto>
      {
        Success = true,
        Data = MapToViewDtoAsync(review)
      };
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Error getting review {ReviewId}", id);
      return new ApiResponse<ReviewViewDto>
      {
        Success = false,
        Message = "An error occurred while getting the review"
      };
    }
  }

  /// <inheritdoc/>
  public async Task<ApiResponse> DeleteReviewAsync(string id, string userId, bool isAdmin)
  {
    try
    {
      // 1. 获取评论信息
      var review = await _context.Reviews
          .Include(r => r.Equipment)
          .FirstOrDefaultAsync(r => r.Id == id);

      if (review == null)
      {
        return new ApiResponse
        {
          Success = false,
          Message = "Review not found"
        };
      }

      // 2. 检查权限
      if (!isAdmin && review.UserId != userId)
      {
        return new ApiResponse
        {
          Success = false,
          Message = "No permission to delete this review"
        };
      }

      using var transaction = await _context.Database.BeginTransactionAsync();

      try
      {
        // 3. 删除评论
        _context.Reviews.Remove(review);

        // 4. 更新设备平均评分
        if (review.Equipment != null)
        {
          var averageRating = await _context.Reviews
              .Where(r => r.EquipmentId == review.EquipmentId)
              .AverageAsync(r => r.Rating);

          review.Equipment.AverageRating = (decimal)averageRating;
        }

        await _context.SaveChangesAsync();
        await transaction.CommitAsync();

        return new ApiResponse
        {
          Success = true,
          Message = "Review deleted successfully"
        };
      }
      catch (Exception)
      {
        await transaction.RollbackAsync();
        throw;
      }
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Error deleting review {ReviewId}", id);
      return new ApiResponse
      {
        Success = false,
        Message = "An error occurred while deleting the review"
      };
    }
  }

  /// <inheritdoc/>
  public async Task<bool> HasUserReviewedEquipmentAsync(string equipmentId, string userId)
  {
    try
    {
      return await _context.Reviews
          .AnyAsync(r => r.EquipmentId == equipmentId && r.UserId == userId);
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Error checking if user {UserId} has reviewed equipment {EquipmentId}", userId, equipmentId);
      return false;
    }
  }

  /// <inheritdoc/>
  public async Task<bool> IsOrderCompletedAndBelongsToUserAsync(string orderId, string userId)
  {
    try
    {
      var order = await _context.Orders
          .FirstOrDefaultAsync(o => o.Id == orderId);

      return order != null &&
             order.RenterId == userId &&
             order.Status == OrderStatus.Completed;
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Error checking order {OrderId} status for user {UserId}", orderId, userId);
      return false;
    }
  }

  /// <summary>
  /// 应用过滤条件
  /// </summary>
  /// <param name="query">基础查询</param>
  /// <param name="parameters">查询参数</param>
  /// <returns>应用过滤条件后的查询</returns>
  private static IQueryable<Review> ApplyFilters(IQueryable<Review> query, ReviewQueryParameters parameters)
  {
    if (!string.IsNullOrEmpty(parameters.EquipmentId))
    {
      query = query.Where(r => r.EquipmentId == parameters.EquipmentId);
    }

    if (!string.IsNullOrEmpty(parameters.UserId))
    {
      query = query.Where(r => r.UserId == parameters.UserId);
    }

    if (parameters.MinRating.HasValue)
    {
      query = query.Where(r => r.Rating >= parameters.MinRating.Value);
    }

    if (parameters.MaxRating.HasValue)
    {
      query = query.Where(r => r.Rating <= parameters.MaxRating.Value);
    }

    if (parameters.StartDate.HasValue)
    {
      query = query.Where(r => r.CreatedAt >= parameters.StartDate.Value);
    }

    if (parameters.EndDate.HasValue)
    {
      query = query.Where(r => r.CreatedAt <= parameters.EndDate.Value);
    }

    return query;
  }

  /// <summary>
  /// 应用排序
  /// </summary>
  /// <param name="query">基础查询</param>
  /// <param name="parameters">查询参数</param>
  /// <returns>应用排序后的查询</returns>
  private static IQueryable<Review> ApplySorting(IQueryable<Review> query, ReviewQueryParameters parameters)
  {
    return parameters.SortBy?.ToLower() switch
    {
      "rating" => parameters.IsAscending
          ? query.OrderBy(r => r.Rating)
          : query.OrderByDescending(r => r.Rating),
      "createdat" => parameters.IsAscending
          ? query.OrderBy(r => r.CreatedAt)
          : query.OrderByDescending(r => r.CreatedAt),
      "updatedat" => parameters.IsAscending
          ? query.OrderBy(r => r.UpdatedAt)
          : query.OrderByDescending(r => r.UpdatedAt),
      _ => query.OrderByDescending(r => r.CreatedAt)
    };
  }

  /// <summary>
  /// 将 Review 实体映射为 ReviewViewDto
  /// </summary>
  /// <param name="review">评论实体</param>
  /// <returns>评论视图 DTO</returns>
  private static ReviewViewDto MapToViewDtoAsync(Review review)
  {
    return new ReviewViewDto
    {
      Id = review.Id,
      EquipmentId = review.EquipmentId,
      EquipmentName = review.Equipment?.Name ?? string.Empty,
      OrderId = review.OrderId,
      UserId = review.UserId,
      UserName = review.User?.UserName ?? string.Empty,
      Rating = review.Rating,
      Content = review.Content,
      CreatedAt = review.CreatedAt,
      UpdatedAt = review.UpdatedAt
    };
  }
}
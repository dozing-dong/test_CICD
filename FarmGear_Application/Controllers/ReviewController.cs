using FarmGear_Application.DTOs;
using FarmGear_Application.DTOs.Reviews;
using FarmGear_Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FarmGear_Application.Controllers;

/// <summary>
/// 评论控制器
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class ReviewController : ControllerBase
{
  private readonly IReviewService _reviewService;
  private readonly ILogger<ReviewController> _logger;

  public ReviewController(
      IReviewService reviewService,
      ILogger<ReviewController> logger)
  {
    _reviewService = reviewService;
    _logger = logger;
  }

  /// <summary>
  /// 创建评论
  /// </summary>
  /// <param name="request">创建评论请求</param>
  /// <returns>评论视图</returns>
  /// <remarks>
  /// 创建评论需要满足以下条件：
  /// 1. 用户必须是农民角色
  /// 2. 订单必须已完成
  /// 3. 订单必须属于当前用户
  /// 4. 用户不能重复评论同一设备
  /// </remarks>
  /// <response code="200">创建成功</response>
  /// <response code="400">请求参数错误</response>
  /// <response code="401">未授权</response>
  /// <response code="403">无权限</response>
  /// <response code="404">设备或订单不存在</response>
  /// <response code="409">用户已评论过该设备</response>
  [HttpPost]
  [Authorize(Roles = "Farmer")]
  [ProducesResponseType(typeof(ApiResponse<ReviewViewDto>), StatusCodes.Status200OK)]
  [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
  [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
  [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status403Forbidden)]
  [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
  [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status409Conflict)]
  public async Task<ApiResponse<ReviewViewDto>> CreateReview([FromBody] CreateReviewRequest request)
  {
    try
    {
      var userId = User.FindFirst("sub")?.Value;
      if (string.IsNullOrEmpty(userId))
      {
        return new ApiResponse<ReviewViewDto>
        {
          Success = false,
          Message = "Failed to get user information"
        };
      }

      return await _reviewService.CreateReviewAsync(request, userId);
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Error occurred while creating review");
      return new ApiResponse<ReviewViewDto>
      {
        Success = false,
        Message = "An error occurred while creating review"
      };
    }
  }

  /// <summary>
  /// 获取评论列表
  /// </summary>
  /// <param name="parameters">查询参数</param>
  /// <returns>分页评论列表</returns>
  /// <remarks>
  /// 查询参数说明：
  /// - EquipmentId: 按设备ID筛选
  /// - UserId: 按用户ID筛选
  /// - Rating: 按评分筛选
  /// - StartDate: 按开始日期筛选
  /// - EndDate: 按结束日期筛选
  /// - PageNumber: 页码
  /// - PageSize: 每页数量
  /// - SortBy: 排序字段
  /// - SortOrder: 排序方向
  /// </remarks>
  /// <response code="200">获取成功</response>
  /// <response code="400">请求参数错误</response>
  [HttpGet]
  [ProducesResponseType(typeof(ApiResponse<PaginatedList<ReviewViewDto>>), StatusCodes.Status200OK)]
  [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
  public async Task<ApiResponse<PaginatedList<ReviewViewDto>>> GetReviews([FromQuery] ReviewQueryParameters parameters)
  {
    try
    {
      return await _reviewService.GetReviewsAsync(parameters);
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Error occurred while getting reviews");
      return new ApiResponse<PaginatedList<ReviewViewDto>>
      {
        Success = false,
        Message = "An error occurred while getting reviews"
      };
    }
  }

  /// <summary>
  /// 获取我的评论列表
  /// </summary>
  /// <param name="parameters">查询参数</param>
  /// <returns>分页评论列表</returns>
  /// <remarks>
  /// 获取当前登录用户的所有评论
  /// </remarks>
  /// <response code="200">获取成功</response>
  /// <response code="400">请求参数错误</response>
  /// <response code="401">未授权</response>
  [HttpGet("my")]
  [Authorize]
  [ProducesResponseType(typeof(ApiResponse<PaginatedList<ReviewViewDto>>), StatusCodes.Status200OK)]
  [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
  [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
  public async Task<ApiResponse<PaginatedList<ReviewViewDto>>> GetMyReviews([FromQuery] ReviewQueryParameters parameters)
  {
    try
    {
      var userId = User.FindFirst("sub")?.Value;
      if (string.IsNullOrEmpty(userId))
      {
        return new ApiResponse<PaginatedList<ReviewViewDto>>
        {
          Success = false,
          Message = "Failed to get user information"
        };
      }

      return await _reviewService.GetMyReviewsAsync(parameters, userId);
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Error occurred while getting user reviews");
      return new ApiResponse<PaginatedList<ReviewViewDto>>
      {
        Success = false,
        Message = "An error occurred while getting user reviews"
      };
    }
  }

  /// <summary>
  /// 获取评论详情
  /// </summary>
  /// <param name="id">评论ID</param>
  /// <returns>评论视图</returns>
  /// <remarks>
  /// 获取指定评论的详细信息，包括：
  /// - 评论内容
  /// - 评分
  /// - 评论时间
  /// - 评论用户信息
  /// - 设备信息
  /// - 订单信息
  /// </remarks>
  /// <response code="200">获取成功</response>
  /// <response code="404">评论不存在</response>
  [HttpGet("{id}")]
  [ProducesResponseType(typeof(ApiResponse<ReviewViewDto>), StatusCodes.Status200OK)]
  [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
  public async Task<ApiResponse<ReviewViewDto>> GetReviewById(string id)
  {
    try
    {
      return await _reviewService.GetReviewByIdAsync(id);
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Error occurred while getting review by id: {ReviewId}", id);
      return new ApiResponse<ReviewViewDto>
      {
        Success = false,
        Message = "An error occurred while getting review"
      };
    }
  }

  /// <summary>
  /// 删除评论
  /// </summary>
  /// <param name="id">评论ID</param>
  /// <returns>操作结果</returns>
  /// <remarks>
  /// 删除评论需要满足以下条件：
  /// 1. 用户必须是评论的作者
  /// 2. 或者用户具有管理员权限
  /// </remarks>
  /// <response code="200">删除成功</response>
  /// <response code="400">请求参数错误</response>
  /// <response code="401">未授权</response>
  /// <response code="403">无权限</response>
  /// <response code="404">评论不存在</response>
  [HttpDelete("{id}")]
  [Authorize]
  [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
  [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
  [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
  [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status403Forbidden)]
  [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
  public async Task<ApiResponse> DeleteReview(string id)
  {
    try
    {
      var userId = User.FindFirst("sub")?.Value;
      if (string.IsNullOrEmpty(userId))
      {
        return new ApiResponse
        {
          Success = false,
          Message = "Failed to get user information"
        };
      }

      var isAdmin = User.IsInRole("Admin");
      return await _reviewService.DeleteReviewAsync(id, userId, isAdmin);
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Error occurred while deleting review: {ReviewId}", id);
      return new ApiResponse
      {
        Success = false,
        Message = "An error occurred while deleting review"
      };
    }
  }
}
using FarmGear_Application.DTOs;
using FarmGear_Application.DTOs.Reviews;

namespace FarmGear_Application.Services;

/// <summary>
/// 评论服务接口
/// </summary>
public interface IReviewService
{
  /// <summary>
  /// 创建评论
  /// </summary>
  /// <param name="request">创建评论请求</param>
  /// <param name="userId">用户ID</param>
  /// <returns>评论视图</returns>
  Task<ApiResponse<ReviewViewDto>> CreateReviewAsync(CreateReviewRequest request, string userId);

  /// <summary>
  /// 获取评论列表
  /// </summary>
  /// <param name="parameters">查询参数</param>
  /// <returns>分页评论列表</returns>
  Task<ApiResponse<PaginatedList<ReviewViewDto>>> GetReviewsAsync(ReviewQueryParameters parameters);

  /// <summary>
  /// 获取我的评论列表
  /// </summary>
  /// <param name="parameters">查询参数</param>
  /// <param name="userId">用户ID</param>
  /// <returns>分页评论列表</returns>
  Task<ApiResponse<PaginatedList<ReviewViewDto>>> GetMyReviewsAsync(ReviewQueryParameters parameters, string userId);

  /// <summary>
  /// 获取评论详情
  /// </summary>
  /// <param name="id">评论ID</param>
  /// <returns>评论视图</returns>
  Task<ApiResponse<ReviewViewDto>> GetReviewByIdAsync(string id);

  /// <summary>
  /// 删除评论
  /// </summary>
  /// <param name="id">评论ID</param>
  /// <param name="userId">用户ID</param>
  /// <param name="isAdmin">是否为管理员</param>
  /// <returns>操作结果</returns>
  Task<ApiResponse> DeleteReviewAsync(string id, string userId, bool isAdmin);

  /// <summary>
  /// 检查用户是否已评论过设备
  /// </summary>
  /// <param name="equipmentId">设备ID</param>
  /// <param name="userId">用户ID</param>
  /// <returns>是否已评论</returns>
  Task<bool> HasUserReviewedEquipmentAsync(string equipmentId, string userId);

  /// <summary>
  /// 检查订单是否已完成且属于用户
  /// </summary>
  /// <param name="orderId">订单ID</param>
  /// <param name="userId">用户ID</param>
  /// <returns>是否有效</returns>
  Task<bool> IsOrderCompletedAndBelongsToUserAsync(string orderId, string userId);
}
using FarmGear_Application.DTOs;
using FarmGear_Application.DTOs.Equipment;
using FarmGear_Application.Models;

namespace FarmGear_Application.Services;

/// <summary>
/// 设备服务接口
/// </summary>
public interface IEquipmentService
{
  /// <summary>
  /// 创建设备
  /// </summary>
  /// <param name="request">创建设备请求</param>
  /// <param name="ownerId">所有者ID</param>
  /// <returns>创建设备响应</returns>
  Task<ApiResponse<EquipmentViewDto>> CreateEquipmentAsync(CreateEquipmentRequest request, string ownerId);

  /// <summary>
  /// 获取设备列表
  /// </summary>
  /// <param name="parameters">查询参数</param>
  /// <returns>分页设备列表</returns>
  Task<ApiResponse<PaginatedList<EquipmentViewDto>>> GetEquipmentListAsync(EquipmentQueryParameters parameters);

  /// <summary>
  /// 获取设备详情
  /// </summary>
  /// <param name="id">设备ID</param>
  /// <returns>设备详情</returns>
  Task<ApiResponse<EquipmentViewDto>> GetEquipmentByIdAsync(string id);

  /// <summary>
  /// 获取用户的设备列表
  /// </summary>
  /// <param name="ownerId">所有者ID</param>
  /// <param name="parameters">查询参数</param>
  /// <returns>分页设备列表</returns>
  Task<ApiResponse<PaginatedList<EquipmentViewDto>>> GetUserEquipmentListAsync(string ownerId, EquipmentQueryParameters parameters);

  /// <summary>
  /// 更新设备
  /// </summary>
  /// <param name="id">设备ID</param>
  /// <param name="request">更新设备请求</param>
  /// <param name="ownerId">所有者ID</param>
  /// <returns>更新设备响应</returns>
  Task<ApiResponse<EquipmentViewDto>> UpdateEquipmentAsync(string id, UpdateEquipmentRequest request, string ownerId);

  /// <summary>
  /// 删除设备
  /// </summary>
  /// <param name="id">设备ID</param>
  /// <param name="ownerId">所有者ID</param>
  /// <param name="isAdmin">是否为管理员</param>
  /// <returns>删除设备响应</returns>
  Task<ApiResponse> DeleteEquipmentAsync(string id, string ownerId, bool isAdmin);

  /// <summary>
  /// 检查设备是否有活跃订单
  /// </summary>
  /// <param name="equipmentId">设备ID</param>
  /// <returns>是否有活跃订单</returns>
  Task<bool> HasActiveOrdersAsync(string equipmentId);
}
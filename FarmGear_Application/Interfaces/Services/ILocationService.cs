using FarmGear_Application.DTOs;
using FarmGear_Application.DTOs.Location;
using FarmGear_Application.Models;

namespace FarmGear_Application.Interfaces.Services;

/// <summary>
/// 位置服务接口
/// </summary>
public interface ILocationService
{
  /// <summary>
  /// 获取附近设备
  /// </summary>
  /// <param name="parameters">查询参数</param>
  /// <returns>分页的设备位置信息列表</returns>
  Task<ApiResponse<PaginatedList<EquipmentLocationDto>>> GetNearbyEquipmentAsync(LocationQueryParameters parameters);

  /// <summary>
  /// 获取设备分布热力图数据
  /// </summary>
  /// <param name="southWestLat">西南角纬度</param>
  /// <param name="southWestLng">西南角经度</param>
  /// <param name="northEastLat">东北角纬度</param>
  /// <param name="northEastLng">东北角经度</param>
  /// <param name="status">设备状态（可选）</param>
  /// <param name="equipmentType">设备类型（可选）</param>
  /// <returns>热力图点数据列表</returns>
  Task<ApiResponse<List<HeatmapPoint>>> GetEquipmentHeatmapAsync(
      double southWestLat,
      double southWestLng,
      double northEastLat,
      double northEastLng,
      EquipmentStatus? status = null,
      string? equipmentType = null);

  /// <summary>
  /// 获取供应商分布
  /// </summary>
  /// <param name="southWestLat">西南角纬度</param>
  /// <param name="southWestLng">西南角经度</param>
  /// <param name="northEastLat">东北角纬度</param>
  /// <param name="northEastLng">东北角经度</param>
  /// <param name="minEquipmentCount">最少设备数量（可选）</param>
  /// <returns>供应商位置信息列表</returns>
  Task<ApiResponse<List<ProviderLocationDto>>> GetProviderDistributionAsync(
      double southWestLat,
      double southWestLng,
      double northEastLat,
      double northEastLng,
      int? minEquipmentCount = null);

  /// <summary>
  /// 更新用户位置
  /// </summary>
  /// <param name="userId">用户ID</param>
  /// <param name="request">位置信息</param>
  /// <returns>更新结果</returns>
  Task<ApiResponse> UpdateUserLocationAsync(string userId, UpdateLocationRequest request);
}
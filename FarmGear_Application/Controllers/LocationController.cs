using FarmGear_Application.DTOs;
using FarmGear_Application.DTOs.Location;
using FarmGear_Application.Interfaces.Services;
using FarmGear_Application.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FarmGear_Application.Controllers;

/// <summary>
/// 位置控制器
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class LocationController : ControllerBase
{
  private readonly ILocationService _locationService;
  private readonly ILogger<LocationController> _logger;

  public LocationController(ILocationService locationService, ILogger<LocationController> logger)
  {
    _locationService = locationService;
    _logger = logger;
  }

  /// <summary>
  /// 获取附近设备
  /// </summary>
  /// <param name="parameters">查询参数</param>
  /// <returns>分页的设备位置信息列表</returns>
  [HttpGet("nearby-equipment")]
  public async Task<ApiResponse<PaginatedList<EquipmentLocationDto>>> GetNearbyEquipment([FromQuery] LocationQueryParameters parameters)
  {
    return await _locationService.GetNearbyEquipmentAsync(parameters);
  }

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
  [HttpGet("equipment-heatmap")]
  public async Task<ApiResponse<List<HeatmapPoint>>> GetEquipmentHeatmap(
      [FromQuery] double southWestLat,
      [FromQuery] double southWestLng,
      [FromQuery] double northEastLat,
      [FromQuery] double northEastLng,
      [FromQuery] EquipmentStatus? status = null,
      [FromQuery] string? equipmentType = null)
  {
    return await _locationService.GetEquipmentHeatmapAsync(
        southWestLat,
        southWestLng,
        northEastLat,
        northEastLng,
        status,
        equipmentType);
  }

  /// <summary>
  /// 获取供应商分布
  /// </summary>
  /// <param name="southWestLat">西南角纬度</param>
  /// <param name="southWestLng">西南角经度</param>
  /// <param name="northEastLat">东北角纬度</param>
  /// <param name="northEastLng">东北角经度</param>
  /// <param name="minEquipmentCount">最少设备数量（可选）</param>
  /// <returns>供应商位置信息列表</returns>
  [HttpGet("provider-distribution")]
  public async Task<ApiResponse<List<ProviderLocationDto>>> GetProviderDistribution(
      [FromQuery] double southWestLat,
      [FromQuery] double southWestLng,
      [FromQuery] double northEastLat,
      [FromQuery] double northEastLng,
      [FromQuery] int? minEquipmentCount = null)
  {
    return await _locationService.GetProviderDistributionAsync(
        southWestLat,
        southWestLng,
        northEastLat,
        northEastLng,
        minEquipmentCount);
  }

  /// <summary>
  /// 更新用户位置
  /// </summary>
  /// <param name="request">位置信息</param>
  /// <returns>更新结果</returns>
  [HttpPut("my-location")]
  [Authorize]
  public async Task<ApiResponse> UpdateMyLocation([FromBody] UpdateLocationRequest request)
  {
    var userId = User.FindFirst("sub")?.Value;
    if (string.IsNullOrEmpty(userId))
    {
      return new ApiResponse
      {
        Success = false,
        Message = "Unable to get user information"
      };
    }

    return await _locationService.UpdateUserLocationAsync(userId, request);
  }
}
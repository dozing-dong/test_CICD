using FarmGear_Application.DTOs;
using FarmGear_Application.DTOs.Equipment;
using FarmGear_Application.Models;

namespace FarmGear_Application.Tests.Helpers;

/// <summary>
/// 测试数据工厂类
/// </summary>
public static class TestDataFactory
{
  /// <summary>
  /// 创建测试用的CreateEquipmentRequest
  /// </summary>
  public static CreateEquipmentRequest CreateEquipmentRequest(
      string name = "Test Equipment",
      string description = "Test Description",
      decimal dailyPrice = 100.00m,
      double latitude = 39.9042,
      double longitude = 116.4074,
      string type = "Tractor")
  {
    return new CreateEquipmentRequest
    {
      Name = name,
      Description = description,
      DailyPrice = dailyPrice,
      Latitude = latitude,
      Longitude = longitude,
      Type = type
    };
  }

  /// <summary>
  /// 创建测试用的UpdateEquipmentRequest
  /// </summary>
  public static UpdateEquipmentRequest CreateUpdateEquipmentRequest(
      string name = "Updated Equipment",
      string description = "Updated Description",
      decimal dailyPrice = 150.00m,
      double latitude = 39.9042,
      double longitude = 116.4074,
      EquipmentStatus status = EquipmentStatus.Available,
      string type = "Updated Tractor")
  {
    return new UpdateEquipmentRequest
    {
      Name = name,
      Description = description,
      DailyPrice = dailyPrice,
      Latitude = latitude,
      Longitude = longitude,
      Status = status,
      Type = type
    };
  }

  /// <summary>
  /// 创建测试用的EquipmentViewDto
  /// </summary>
  public static EquipmentViewDto CreateEquipmentViewDto(
      string id = "test-equipment-id",
      string name = "Test Equipment",
      string description = "Test Description",
      decimal dailyPrice = 100.00m,
      double latitude = 39.9042,
      double longitude = 116.4074,
      EquipmentStatus status = EquipmentStatus.Available,
      string ownerId = "test-owner-id",
      string ownerUsername = "testuser",
      string type = "Tractor")
  {
    return new EquipmentViewDto
    {
      Id = id,
      Name = name,
      Description = description,
      DailyPrice = dailyPrice,
      Latitude = latitude,
      Longitude = longitude,
      Status = status,
      OwnerId = ownerId,
      OwnerUsername = ownerUsername,
      Type = type,
      CreatedAt = DateTime.UtcNow
    };
  }

  /// <summary>
  /// 创建测试用的EquipmentQueryParameters
  /// </summary>
  public static EquipmentQueryParameters CreateEquipmentQueryParameters(
      int page = 1,
      int pageSize = 10,
      string? searchTerm = null,
      EquipmentStatus? status = null,
      string? type = null,
      decimal? minPrice = null,
      decimal? maxPrice = null)
  {
    return new EquipmentQueryParameters
    {
      PageNumber = page,
      PageSize = pageSize,
      SearchTerm = searchTerm,
      Status = status,
      MinDailyPrice = minPrice,
      MaxDailyPrice = maxPrice
    };
  }

  /// <summary>
  /// 创建成功的ApiResponse
  /// </summary>
  public static ApiResponse<T> CreateSuccessResponse<T>(T data, string message = "Success")
  {
    return new ApiResponse<T>
    {
      Success = true,
      Data = data,
      Message = message
    };
  }

  /// <summary>
  /// 创建失败的ApiResponse
  /// </summary>
  public static ApiResponse<T> CreateErrorResponse<T>(string message, T? data = default)
  {
    return new ApiResponse<T>
    {
      Success = false,
      Data = data,
      Message = message
    };
  }

  /// <summary>
  /// 创建成功的ApiResponse（无数据）
  /// </summary>
  public static ApiResponse CreateSuccessResponse(string message = "Success")
  {
    return new ApiResponse
    {
      Success = true,
      Message = message
    };
  }

  /// <summary>
  /// 创建失败的ApiResponse（无数据）
  /// </summary>
  public static ApiResponse CreateErrorResponse(string message)
  {
    return new ApiResponse
    {
      Success = false,
      Message = message
    };
  }

  /// <summary>
  /// 创建分页列表
  /// </summary>
  public static PaginatedList<T> CreatePaginatedList<T>(
      IEnumerable<T> items,
      int totalCount,
      int page = 1,
      int pageSize = 10)
  {
    return new PaginatedList<T>(items.ToList(), totalCount, page, pageSize);
  }
}
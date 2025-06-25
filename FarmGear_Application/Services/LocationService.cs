using FarmGear_Application.Data;
using FarmGear_Application.DTOs;
using FarmGear_Application.DTOs.Location;
using FarmGear_Application.Interfaces.Services;
using FarmGear_Application.Models;
using Microsoft.EntityFrameworkCore;

namespace FarmGear_Application.Services;

/// <summary>
/// 位置服务实现
/// </summary>
public class LocationService : ILocationService
{
  private readonly ApplicationDbContext _context;
  private readonly ILogger<LocationService> _logger;

  public LocationService(ApplicationDbContext context, ILogger<LocationService> logger)
  {
    _context = context;
    _logger = logger;
  }

  /// <inheritdoc/>
  /// 这个方法用于获取附近可用的设备,需要提供查询参数,包括中心点坐标,半径,最小价格,最大价格,设备类型,状态,返回分页的设备位置信息列表
  public async Task<ApiResponse<PaginatedList<EquipmentLocationDto>>> GetNearbyEquipmentAsync(LocationQueryParameters parameters)
  {
    try
    {
      // 构建基础查询
      var query = _context.Equipment
          .Include(e => e.Owner)
          .Where(e => e.Status == EquipmentStatus.Available && e.Owner != null);

      // 应用价格过滤
      if (parameters.MinPrice.HasValue)
      {
        query = query.Where(e => e.DailyPrice >= (decimal)parameters.MinPrice.Value);
      }
      if (parameters.MaxPrice.HasValue)
      {
        query = query.Where(e => e.DailyPrice <= (decimal)parameters.MaxPrice.Value);
      }

      // 应用设备类型过滤
      if (!string.IsNullOrEmpty(parameters.EquipmentType))
      {
        query = query.Where(e => e.Type == parameters.EquipmentType);
      }

      // 应用状态过滤
      if (parameters.Status.HasValue)
      {
        query = query.Where(e => e.Status == parameters.Status.Value);
      }

      // 先获取所有符合条件的设备
      var equipment = await query.ToListAsync();

      // 在内存中计算距离并过滤
      var itemsWithDistance = equipment
          //构建表达式树, 并不立即执行, list继承了IEnumerable<T>, 表达式树通过这个格式来构建,实际上是对list的包装
          //这里其实很像for循环的一层,tolist()执行循环
          //select这里的包装,实际上是构建一个list里的元素为新的数据结构, 这是一种映射机制
          //LINQ 的 .Select() 是只读映射，不会修改原始数据,
          //需要new的是新的数据结构, 而不是原始数据结构; 而不需要new是因为有已有的简单类型(比如int,string)
          .Select(e => new
          {
            Equipment = e,
            Distance = CalculateDistance(
                parameters.Latitude,
                parameters.Longitude,
                (double)e.Latitude,
                (double)e.Longitude)
          })
          //where等等继续包装
          .Where(x => x.Distance <= parameters.Radius)
          .OrderBy(x => x.Distance)
          .Skip((parameters.PageNumber - 1) * parameters.PageSize)
          .Take(parameters.PageSize)
          //最后再映射为新的数据结构
          .Select(x => new EquipmentLocationDto
          {
            Id = x.Equipment.Id,
            Name = x.Equipment.Name,
            Latitude = (double)x.Equipment.Latitude,
            Longitude = (double)x.Equipment.Longitude,
            Distance = x.Distance,
            DailyPrice = x.Equipment.DailyPrice,
            Status = x.Equipment.Status,
            OwnerName = x.Equipment.Owner!.UserName ?? string.Empty
          })
          .ToList();

      // 计算总数
      var totalCount = equipment
          .Select(e => CalculateDistance(
              parameters.Latitude,
              parameters.Longitude,
              (double)e.Latitude,
              (double)e.Longitude))
          .Count(d => d <= parameters.Radius);

      var result = new PaginatedList<EquipmentLocationDto>(
          itemsWithDistance,
          totalCount,
          parameters.PageNumber,
          parameters.PageSize);

      return new ApiResponse<PaginatedList<EquipmentLocationDto>>
      {
        Success = true,
        Data = result
      };
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Error getting nearby equipment");
      return new ApiResponse<PaginatedList<EquipmentLocationDto>>
      {
        Success = false,
        Message = "Error occurred while getting nearby equipment"
      };
    }
  }

  /// <inheritdoc/>
  public async Task<ApiResponse<List<HeatmapPoint>>> GetEquipmentHeatmapAsync(
      double southWestLat,
      double southWestLng,
      double northEastLat,
      double northEastLng,
      EquipmentStatus? status = null,
      string? equipmentType = null)
  {
    try
    {
      // 验证边界
      if (!IsValidBounds(southWestLat, southWestLng, northEastLat, northEastLng))
      {
        return new ApiResponse<List<HeatmapPoint>>
        {
          Success = false,
          Message = "Invalid map bounds"
        };
      }

      // 构建查询
      var query = _context.Equipment
          .Where(e => (double)e.Latitude >= southWestLat && (double)e.Latitude <= northEastLat
              && (double)e.Longitude >= southWestLng && (double)e.Longitude <= northEastLng);

      // 应用过滤
      if (status.HasValue)
      {

        query = query.Where(e => e.Status == status.Value);
      }
      if (!string.IsNullOrEmpty(equipmentType))
      {
        query = query.Where(e => e.Type == equipmentType);
      }

      // 按网格聚合数据
      const double gridSize = 0.01; // 约1公里
      var heatmapData = await query
          .GroupBy(e => new
          {
            Lat = Math.Floor((double)e.Latitude / gridSize) * gridSize + gridSize / 2,
            Lng = Math.Floor((double)e.Longitude / gridSize) * gridSize + gridSize / 2
          })
          .Select(g => new HeatmapPoint
          {
            Latitude = g.Key.Lat,
            Longitude = g.Key.Lng,
            EquipmentCount = g.Count(),
            Weight = g.Count() // 可以根据需要调整权重计算方式
          })
          .ToListAsync();

      return new ApiResponse<List<HeatmapPoint>>
      {
        Success = true,
        Data = heatmapData
      };
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Error getting equipment heatmap");
      return new ApiResponse<List<HeatmapPoint>>
      {
        Success = false,
        Message = "Error occurred while getting equipment heatmap"
      };
    }
  }

  /// <inheritdoc/>
  public async Task<ApiResponse<List<ProviderLocationDto>>> GetProviderDistributionAsync(
      double southWestLat,
      double southWestLng,
      double northEastLat,
      double northEastLng,
      int? minEquipmentCount = null)
  {
    try
    {
      // 验证边界
      if (!IsValidBounds(southWestLat, southWestLng, northEastLat, northEastLng))
      {
        return new ApiResponse<List<ProviderLocationDto>>
        {
          Success = false,
          Message = "Invalid map bounds"
        };
      }

      // 构建查询,具体来说, 这里的操作包括调用where对list进行过滤, 调用select对list进行映射
      var query = _context.Users
      //这一步是对数据的过滤, 包括判断是否为空, 判断是否在范围内
          .Where(u => u.Lat.HasValue && u.Lng.HasValue
              && (double)u.Lat.Value >= southWestLat && (double)u.Lat.Value <= northEastLat
              && (double)u.Lng.Value >= southWestLng && (double)u.Lng.Value <= northEastLng)
          .Select(u => new
          {
            Provider = u,
            EquipmentCount = _context.Equipment.Count(e => e.OwnerId == u.Id),
            AvailableCount = _context.Equipment.Count(e => e.OwnerId == u.Id && e.Status == EquipmentStatus.Available)
          });

      // 应用设备数量过滤
      if (minEquipmentCount.HasValue)
      {
        query = query.Where(x => x.EquipmentCount >= minEquipmentCount.Value);
      }

      // 执行查询
      var providers = await query
          .Select(x => new ProviderLocationDto
          {
            ProviderId = x.Provider.Id,
            ProviderName = x.Provider.UserName ?? string.Empty,
            Latitude = (double)x.Provider.Lat!.Value,
            Longitude = (double)x.Provider.Lng!.Value,
            EquipmentCount = x.EquipmentCount,
            AvailableCount = x.AvailableCount
          })
          .ToListAsync();

      return new ApiResponse<List<ProviderLocationDto>>
      {
        Success = true,
        Data = providers
      };
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Error getting provider distribution");
      return new ApiResponse<List<ProviderLocationDto>>
      {
        Success = false,
        Message = "Error occurred while getting provider distribution"
      };
    }
  }

  /// <inheritdoc/>
  public async Task<ApiResponse> UpdateUserLocationAsync(string userId, UpdateLocationRequest request)
  {
    using var transaction = await _context.Database.BeginTransactionAsync();
    try
    {
      var user = await _context.Users.FindAsync(userId);
      if (user == null)
      {
        return new ApiResponse
        {
          Success = false,
          Message = "User not found"
        };
      }

      user.Lat = (decimal)request.Latitude;
      user.Lng = (decimal)request.Longitude;

      await _context.SaveChangesAsync();
      await transaction.CommitAsync();

      return new ApiResponse
      {
        Success = true,
        Message = "Location updated successfully"
      };
    }
    catch (Exception ex)
    {
      await transaction.RollbackAsync();
      _logger.LogError(ex, "Error updating user location for user {UserId}", userId);
      return new ApiResponse
      {
        Success = false,
        Message = "Error occurred while updating user location"
      };
    }
  }

  /// <summary>
  /// 计算两点之间的距离（米）
  /// </summary>
  private static double CalculateDistance(double lat1, double lon1, double lat2, double lon2)
  {
    const double R = 6371000; // 地球半径（米）
    var dLat = ToRadians(lat2 - lat1);
    var dLon = ToRadians(lon2 - lon1);
    var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
            Math.Cos(ToRadians(lat1)) * Math.Cos(ToRadians(lat2)) *
            Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
    var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
    return R * c;
  }

  /// <summary>
  /// 将角度转换为弧度
  /// </summary>
  private static double ToRadians(double degrees)
  {
    return degrees * Math.PI / 180;
  }

  /// <summary>
  /// 验证传入的地图边界坐标是否合理有效,防止传入的坐标超出范围
  /// </summary>
  private static bool IsValidBounds(double southWestLat, double southWestLng, double northEastLat, double northEastLng)
  {
    return southWestLat >= -90 && southWestLat <= 90
        && northEastLat >= -90 && northEastLat <= 90
        && southWestLng >= -180 && southWestLng <= 180
        && northEastLng >= -180 && northEastLng <= 180
        && southWestLat <= northEastLat
        && southWestLng <= northEastLng;
  }
}
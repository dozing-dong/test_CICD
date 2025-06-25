using FarmGear_Application.Data;
using FarmGear_Application.DTOs;
using FarmGear_Application.DTOs.Equipment;
using FarmGear_Application.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace FarmGear_Application.Services;

/// <summary>
/// 设备服务实现
/// </summary>
public class EquipmentService : IEquipmentService
{
  private readonly ApplicationDbContext _context;
  private readonly UserManager<AppUser> _userManager;
  private readonly ILogger<EquipmentService> _logger;

  public EquipmentService(
      //这是ApplicationDbContext，它表示：应用程序上下文，用于管理数据库连接和操作
      ApplicationDbContext context,
      //这是UserManager<AppUser>，它表示：用户管理器，用于管理用户信息
      UserManager<AppUser> userManager,
      //这是ILogger<EquipmentService>，它表示：日志记录器，用于记录日志信息
      ILogger<EquipmentService> logger)
  {
    _context = context;
    _userManager = userManager;
    _logger = logger;
  }

  public async Task<ApiResponse<EquipmentViewDto>> CreateEquipmentAsync(CreateEquipmentRequest request, string ownerId)
  {
    using var transaction = await _context.Database.BeginTransactionAsync();
    try
    {
      // 检查用户是否存在,并将查询出来的用户数据赋值给owner变量
      var owner = await _userManager.FindByIdAsync(ownerId);
      if (owner == null)
      {
        return new ApiResponse<EquipmentViewDto>
        {
          Success = false,
          Message = "User does not exist"
        };
      }

      // 检查用户角色,这里的owner是前面根据ownerId从User表中查出来的用户数据
      var roles = await _userManager.GetRolesAsync(owner);
      if (!roles.Contains("Provider") && !roles.Contains("Official"))
      {
        return new ApiResponse<EquipmentViewDto>
        {
          Success = false,
          Message = "Only providers and officials can create equipment"
        };
      }

      // 创建设备
      var equipment = new Equipment
      {
        Name = request.Name,
        Description = request.Description,
        DailyPrice = request.DailyPrice,
        Latitude = (decimal)request.Latitude,
        Longitude = (decimal)request.Longitude,
        OwnerId = ownerId,
        Status = EquipmentStatus.Available,
        Type = request.Type // 添加设备类型
      };

      _context.Equipment.Add(equipment);
      await _context.SaveChangesAsync();
      await transaction.CommitAsync();

      // 返回设备视图,MapToViewDtoAsync返回的是一个精简或重构后的数据结构,
      // 它只包含设备的一些必要信息,而不是所有信息
      return new ApiResponse<EquipmentViewDto>
      {
        Success = true,
        Message = "Equipment created successfully",
        Data = await MapToViewDtoAsync(equipment)
      };
    }
    catch (Exception ex)
    {
      await transaction.RollbackAsync();
      _logger.LogError(ex, "An error occurred while creating equipment");
      return new ApiResponse<EquipmentViewDto>
      {
        Success = false,
        Message = "An error occurred while creating equipment"
      };
    }
  }

  public async Task<ApiResponse<PaginatedList<EquipmentViewDto>>> GetEquipmentListAsync(EquipmentQueryParameters parameters)
  {
    try
    {
      var query = _context.Equipment
          //这是优化查询性能的,因为每次查询都要查询Owner表,所以这里提前查询出来,避免多次查询
          .Include(e => e.Owner)
          //一个dbset既实现IEnumerable<T>又实现IQueryable<T>
          //.AsQueryable() 就是为了让一个集合延迟执行
          //👉 不要立刻执行查询，
          //👉 而是构建出一个可以继续追加操作的表达式树，最后再统一执行。
          //如果不加这个,那么context可能会被隐式转换IEnumerable<Equipment>,也就是立即执行查询,那你在后面if条件中就无法使用query.Where()了
          //因为IEnumerable<Equipment>没有Where方法,且你的查询链断了
          //这里我们就确保query是一个IQueryable<Equipment>类
          .AsQueryable();

      // 应用筛选条件,如果搜索条件不为空,则将搜索条件应用到查询中
      // 搜索条件是SearchTerm,它是一个字符串,表示搜索的关键词
      // 搜索的关键词可以是设备名称或描述

      //第一个if条件是搜索条件不为空,则将搜索条件应用到查询中,并将搜索条件变成小写,然后放进表达式树中,等待执行
      if (!string.IsNullOrWhiteSpace(parameters.SearchTerm))
      {
        var searchTerm = parameters.SearchTerm.ToLower();
        query = query.Where(e =>
            e.Name.ToLower().Contains(searchTerm) ||
            e.Description.ToLower().Contains(searchTerm));
      }

      //第二个if条件是如果最小价格不为空,则将最小价格应用到查询中
      if (parameters.MinDailyPrice.HasValue)
      {
        query = query.Where(e => e.DailyPrice >= parameters.MinDailyPrice.Value);
      }

      //第三个if条件是如果最大价格不为空,则将最大价格应用到查询中
      if (parameters.MaxDailyPrice.HasValue)
      {
        query = query.Where(e => e.DailyPrice <= parameters.MaxDailyPrice.Value);
      }

      //第四个if条件是如果状态不为空,则将状态应用到查询中,这个所谓状态就是EquipmentStatus枚举中的Available,
      //EquipmentStatus枚举中的Available表示设备可用,其他状态表示设备不可用
      if (parameters.Status.HasValue)
      {
        query = query.Where(e => e.Status == parameters.Status.Value);
      }

      // 应用排序,对query进行排序,排序的依据是SortBy,它是一个字符串,表示排序的依据
      // 排序的依据可以是设备名称,设备价格,设备创建时间
      // 排序的顺序可以是升序,也可以是降序
      // 排序的顺序是根据IsAscending决定的,如果IsAscending为true,则表示升序,否则表示降序
      // 排序的依据是根据SortBy变量决定的,如果SortBy为"name",则表示按设备名称排序,如果SortBy为"dailyprice",则表示按设备价格排序,如果SortBy为"createdat",则表示按设备创建时间排序
      // 如果SortBy为其他值,则表示按设备创建时间排序
      //使用switch语句来根据SortBy的值来决定排序的依据
      //👉 如果SortBy为"name",则表示按设备名称排序,如果SortBy为"dailyprice",则表示按设备价格排序,如果SortBy为"createdat",则表示按设备创建时间排序
      //👉 如果SortBy为其他值,则表示按设备创建时间排序
      //👉 如果IsAscending为true,则表示升序,否则表示降序
      //👉 如果SortBy为null,则表示按设备创建时间排序
      //👉 如果SortBy为空,则表示按设备创建时间排序
      //👉 如果SortBy为空,则表示按设备创建时间排序
      query = parameters.SortBy?.ToLower() switch
      {
        "name" => parameters.IsAscending
            ? query.OrderBy(e => e.Name)
            : query.OrderByDescending(e => e.Name),
        "dailyprice" => parameters.IsAscending
            ? query.OrderBy(e => e.DailyPrice)
            : query.OrderByDescending(e => e.DailyPrice),
        "createdat" => parameters.IsAscending
            ? query.OrderBy(e => e.CreatedAt)
            : query.OrderByDescending(e => e.CreatedAt),
        _ => query.OrderByDescending(e => e.CreatedAt)
      };

      // 获取分页数据
      //👉 获取分页数据,PaginatedList<Equipment>是分页数据封装类,CreateAsync是静态方法,用于创建一个分页列表
      //👉 参数:query是查询,parameters.PageNumber是页码,parameters.PageSize是每页大小
      //👉 返回:分页数据,PaginatedList<Equipment>是分页数据封装类,这个方法一次执行等于返回你想要的某一页的数据
      //说白了就是返回了一个自定义的列表,这个列表包含了某一单页的数据还有总记录数,总页数,当前页码,每页大小
      //然后cr
      var paginatedList = await PaginatedList<Equipment>.CreateAsync(
          query,
          parameters.PageNumber,
          parameters.PageSize);

      // 转换为视图 DTO

      var items = await Task.WhenAll(
          //.Select(...) 是 LINQ 的扩展方法
          //👉 它用于对集合中的每个元素执行一个函数,并返回一个包含结果的新的集合
          //MapToViewDtoAsync是你自定义的方法,用于将Equipment表中的数据转换为EquipmentViewDto表中的数据
          //对于集合的每一个元素,都一遍遍的异步的执行MapToViewDtoAsync方法,直到集合中的所有元素都执行完毕
          //👉 最后将结果转换为一个数组,数组中的每个元素都是EquipmentViewDto类型的数据
          //EquipmentViewDto[] items = [Dto1, Dto2, Dto3]
          paginatedList.Items.Select(MapToViewDtoAsync));

      return new ApiResponse<PaginatedList<EquipmentViewDto>>
      {
        Success = true,
        Data = new PaginatedList<EquipmentViewDto>(
              //👉 将items数组转换为列表,并通过这个构造函数,存入PaginatedList<EquipmentViewDto>的Items属性中
              items.ToList(),
              paginatedList.TotalCount,
              paginatedList.PageNumber,
              paginatedList.PageSize)
      };
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "An error occurred while retrieving equipment list");
      return new ApiResponse<PaginatedList<EquipmentViewDto>>
      {
        Success = false,
        Message = "An error occurred while retrieving equipment list"
      };
    }
  }
  // 根据设备id获取设备详情
  public async Task<ApiResponse<EquipmentViewDto>> GetEquipmentByIdAsync(string id)
  {
    try
    {
      //你有注意到每个方法都会var一个_context.Equipments 
      //这 是一个 **"查询入口"（query gateway），每次访问都相当于开始一条新的数据库查询链**
      //换句话说：  
      //👉 每次访问都相当于开始一条新的数据库查询链

      var equipment = await _context.Equipment
          .Include(e => e.Owner)
          .FirstOrDefaultAsync(e => e.Id == id);

      if (equipment == null)
      {
        return new ApiResponse<EquipmentViewDto>
        {
          Success = false,
          Message = "Equipment does not exist"
        };
      }

      return new ApiResponse<EquipmentViewDto>
      {
        Success = true,
        Data = await MapToViewDtoAsync(equipment)
      };
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "An error occurred while retrieving equipment details");
      return new ApiResponse<EquipmentViewDto>
      {
        Success = false,
        Message = "An error occurred while retrieving equipment details"
      };
    }
  }
  //根据用户id获取用户所有的设备,且可以对设备进行筛选,传参第一个是用户id,第二个是查询参数
  public async Task<ApiResponse<PaginatedList<EquipmentViewDto>>> GetUserEquipmentListAsync(
      string ownerId,
      EquipmentQueryParameters parameters)
  {
    try
    {
      var query = _context.Equipment
          .Include(e => e.Owner)
          .Where(e => e.OwnerId == ownerId)
          .AsQueryable();

      // 应用筛选条件,如果搜索条件不为空,则将搜索条件应用到查询中
      if (!string.IsNullOrWhiteSpace(parameters.SearchTerm))
      {
        var searchTerm = parameters.SearchTerm.ToLower();
        query = query.Where(e =>
            e.Name.ToLower().Contains(searchTerm) ||
            e.Description.ToLower().Contains(searchTerm));
      }

      if (parameters.MinDailyPrice.HasValue)
      {
        query = query.Where(e => e.DailyPrice >= parameters.MinDailyPrice.Value);
      }

      if (parameters.MaxDailyPrice.HasValue)
      {
        query = query.Where(e => e.DailyPrice <= parameters.MaxDailyPrice.Value);
      }

      if (parameters.Status.HasValue)
      {
        query = query.Where(e => e.Status == parameters.Status.Value);
      }

      // 应用排序
      query = parameters.SortBy?.ToLower() switch
      {
        "name" => parameters.IsAscending
            ? query.OrderBy(e => e.Name)
            : query.OrderByDescending(e => e.Name),
        "dailyprice" => parameters.IsAscending
            ? query.OrderBy(e => e.DailyPrice)
            : query.OrderByDescending(e => e.DailyPrice),
        "createdat" => parameters.IsAscending
            ? query.OrderBy(e => e.CreatedAt)
            : query.OrderByDescending(e => e.CreatedAt),
        _ => query.OrderByDescending(e => e.CreatedAt)
      };

      // 获取分页数据
      var paginatedList = await PaginatedList<Equipment>.CreateAsync(
          query,
          parameters.PageNumber,
          parameters.PageSize);

      // 转换为视图 DTO
      var items = await Task.WhenAll(
          paginatedList.Items.Select(MapToViewDtoAsync));

      return new ApiResponse<PaginatedList<EquipmentViewDto>>
      {
        Success = true,
        Data = new PaginatedList<EquipmentViewDto>(
              items.ToList(),
              paginatedList.TotalCount,
              paginatedList.PageNumber,
              paginatedList.PageSize)
      };
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "An error occurred while retrieving user equipment list");
      return new ApiResponse<PaginatedList<EquipmentViewDto>>
      {
        Success = false,
        Message = "An error occurred while retrieving user equipment list"
      };
    }
  }
  //根据设备id更新设备信息,传参第一个是设备id,第二个是你想更新的设备信息,第三个是当前登录用户的id
  public async Task<ApiResponse<EquipmentViewDto>> UpdateEquipmentAsync(
      string id,
      UpdateEquipmentRequest request,
      string ownerId)
  {
    using var transaction = await _context.Database.BeginTransactionAsync();
    try
    {
      //根据设备id获取设备信息,并关联查询出设备所属用户信息
      var equipment = await _context.Equipment
          .Include(e => e.Owner)
          .FirstOrDefaultAsync(e => e.Id == id);

      if (equipment == null)
      {
        return new ApiResponse<EquipmentViewDto>
        {
          Success = false,
          Message = "Equipment does not exist"
        };
      }

      // 检查权限
      //👉 检查当前登录用户是否是设备所属用户
      //👉 如果不是,则返回错误信息
      if (equipment.OwnerId != ownerId)
      {
        return new ApiResponse<EquipmentViewDto>
        {
          Success = false,
          Message = "No permission to modify this equipment"
        };
      }

      // 检查设备状态
      //👉 检查设备状态是否为可用
      //👉 如果不是,则返回错误信息
      if (equipment.Status != EquipmentStatus.Available)
      {
        return new ApiResponse<EquipmentViewDto>
        {
          Success = false,
          Message = "Only equipment in available status can be modified"
        };
      }

      // 更新设备信息
      equipment.Name = request.Name;
      equipment.Description = request.Description;
      equipment.DailyPrice = request.DailyPrice;
      equipment.Latitude = (decimal)request.Latitude;
      equipment.Longitude = (decimal)request.Longitude;
      equipment.Status = request.Status;
      equipment.Type = request.Type; // 添加设备类型更新

      await _context.SaveChangesAsync();
      await transaction.CommitAsync();

      return new ApiResponse<EquipmentViewDto>
      {
        Success = true,
        Message = "Equipment updated successfully",
        //依旧是转换为前端专属的EquipmentViewDto类型
        Data = await MapToViewDtoAsync(equipment)
      };
    }
    catch (Exception ex)
    {
      await transaction.RollbackAsync();
      _logger.LogError(ex, "An error occurred while updating equipment");
      return new ApiResponse<EquipmentViewDto>
      {
        Success = false,
        Message = "An error occurred while updating equipment"
      };
    }
  }
  //根据设备id删除设备,传参第一个是设备id,第二个是当前登录用户的id,第三个是是否为管理员
  public async Task<ApiResponse> DeleteEquipmentAsync(string id, string ownerId, bool isAdmin)
  {
    try
    {
      //根据设备id获取设备信息
      var equipment = await _context.Equipment
          .FirstOrDefaultAsync(e => e.Id == id);

      if (equipment == null)
      {
        return new ApiResponse
        {
          Success = false,
          Message = "Equipment does not exist"
        };
      }

      // 检查权限
      //👉 检查当前登录用户是否是设备所属用户
      //👉 如果不是,则返回错误信息
      if (!isAdmin && equipment.OwnerId != ownerId)
      {
        return new ApiResponse
        {
          Success = false,
          Message = "No permission to delete this equipment"
        };
      }

      // 检查是否有活跃订单
      //👉 检查设备是否有活跃订单
      //👉 如果有,则返回错误信息
      if (await HasActiveOrdersAsync(id))
      {
        return new ApiResponse
        {
          Success = false,
          Message = "Equipment has active orders and cannot be deleted"
        };
      }

      //👉 删除设备
      _context.Equipment.Remove(equipment);
      await _context.SaveChangesAsync();

      return new ApiResponse
      {
        Success = true,
        Message = "Equipment deleted successfully"
      };
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "An error occurred while deleting equipment");
      return new ApiResponse
      {
        Success = false,
        Message = "An error occurred while deleting equipment"
      };
    }
  }
  //这个方法用于检查设备是否有活跃订单,传参是设备id,目前没有实现,所以返回false,将来有订单服务的时候,这里需要实现
  public async Task<bool> HasActiveOrdersAsync(string equipmentId)
  {
    // TODO: Implement order service check
    return await Task.FromResult(false);
  }
  //这个方法是将Equipment对象转换为EquipmentViewDto对象,也就是将Equipment表中的数据转换为EquipmentViewDto表中的数据
  //EquipmentViewDto它是一个精简或重构后的数据结构,它只包含设备的一些必要信息,而不是所有信息
  //这个新的数据结构用于在Controller层中返回给前端
  private async Task<EquipmentViewDto> MapToViewDtoAsync(Equipment equipment)
  {
    var owner = equipment.Owner ?? await _userManager.FindByIdAsync(equipment.OwnerId);

    return new EquipmentViewDto
    {
      Id = equipment.Id,
      Name = equipment.Name,
      Description = equipment.Description,
      DailyPrice = equipment.DailyPrice,
      Latitude = (double)equipment.Latitude,
      Longitude = (double)equipment.Longitude,
      Status = equipment.Status,
      OwnerId = equipment.OwnerId,
      //owner是前面根据ownerId从User表中查出来的用户数据
      OwnerUsername = owner?.UserName ?? string.Empty,
      Type = equipment.Type,
      CreatedAt = equipment.CreatedAt
    };
  }
}
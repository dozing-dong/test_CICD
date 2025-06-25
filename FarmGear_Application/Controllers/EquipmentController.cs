using FarmGear_Application.DTOs;
using FarmGear_Application.DTOs.Equipment;
using FarmGear_Application.Models;
using FarmGear_Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FarmGear_Application.Controllers;

/// <summary>
/// 设备控制器
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class EquipmentController : ControllerBase
{
  private readonly IEquipmentService _equipmentService;
  private readonly ILogger<EquipmentController> _logger;

  public EquipmentController(
      IEquipmentService equipmentService,
      ILogger<EquipmentController> logger)
  {
    _equipmentService = equipmentService;
    _logger = logger;
  }

  /// <summary>
  /// 创建设备
  /// </summary>
  /// <param name="request">创建设备请求</param>
  /// <returns>设备视图</returns>
  /// <response code="201">创建成功</response>
  /// <response code="400">请求参数错误</response>
  /// <response code="401">未授权</response>
  /// <response code="403">无权限</response>
  [HttpPost]
  [Authorize(Roles = "Provider,Official")]
  [ProducesResponseType(typeof(ApiResponse<EquipmentViewDto>), StatusCodes.Status201Created)]
  [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
  [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
  [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status403Forbidden)]
  public async Task<IActionResult> CreateEquipment([FromBody] CreateEquipmentRequest request)
  {
    try
    {
      var ownerId = User.FindFirst("sub")?.Value;
      if (string.IsNullOrEmpty(ownerId))
      {
        return BadRequest(new ApiResponse<EquipmentViewDto>
        {
          Success = false,
          Message = "Failed to get user information"
        });
      }

      var result = await _equipmentService.CreateEquipmentAsync(request, ownerId);
      return result.Success switch
      {
        true => result.Data == null
          ? StatusCode(500, new ApiResponse<EquipmentViewDto> { Success = false, Message = "Created equipment data is null" })
          : CreatedAtAction(nameof(GetEquipmentById), new { id = result.Data.Id }, result),
        false => result.Message switch
        {
          "User does not exist" => NotFound(result),
          "Only providers and officials can create equipment" => Forbid(),
          _ => BadRequest(result)
        }
      };
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Error occurred while creating equipment");
      return StatusCode(500, new ApiResponse<EquipmentViewDto>
      {
        Success = false,
        Message = "An error occurred while creating equipment"
      });
    }
  }

  /// <summary>
  /// 获取设备列表
  /// </summary>
  /// <param name="parameters">查询参数</param>
  /// <returns>分页设备列表</returns>
  /// <response code="200">获取成功</response>
  /// <response code="400">请求参数错误</response>
  [HttpGet]
  [ProducesResponseType(typeof(ApiResponse<PaginatedList<EquipmentViewDto>>), StatusCodes.Status200OK)]
  [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
  public async Task<IActionResult> GetEquipmentList([FromQuery] EquipmentQueryParameters parameters)
  {
    try
    {
      var result = await _equipmentService.GetEquipmentListAsync(parameters);
      return result.Success ? Ok(result) : BadRequest(result);
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Error occurred while retrieving equipment list");
      return StatusCode(500, new ApiResponse<PaginatedList<EquipmentViewDto>>
      {
        Success = false,
        Message = "An error occurred while retrieving equipment list"
      });
    }
  }

  /// <summary>
  /// 获取设备详情
  /// </summary>
  /// <param name="id">设备ID</param>
  /// <returns>设备视图</returns>
  /// <response code="200">获取成功</response>
  /// <response code="404">设备不存在</response>
  [HttpGet("{id}")]
  [ProducesResponseType(typeof(ApiResponse<EquipmentViewDto>), StatusCodes.Status200OK)]
  [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
  public async Task<IActionResult> GetEquipmentById(string id)
  {
    try
    {
      var result = await _equipmentService.GetEquipmentByIdAsync(id);
      return result.Success switch
      {
        true => Ok(result),
        false => result.Message switch
        {
          "Equipment does not exist" => NotFound(result),
          _ => BadRequest(result)
        }
      };
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Error occurred while retrieving equipment details: {EquipmentId}", id);
      return StatusCode(500, new ApiResponse<EquipmentViewDto>
      {
        Success = false,
        Message = "An error occurred while retrieving equipment details"
      });
    }
  }

  /// <summary>
  /// 获取我的设备列表
  /// </summary>
  /// <param name="parameters">查询参数</param>
  /// <returns>分页设备列表</returns>
  /// <response code="200">获取成功</response>
  /// <response code="400">请求参数错误</response>
  /// <response code="401">未授权</response>
  [HttpGet("my-equipment")]
  [Authorize]
  [ProducesResponseType(typeof(ApiResponse<PaginatedList<EquipmentViewDto>>), StatusCodes.Status200OK)]
  [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
  [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
  public async Task<IActionResult> GetMyEquipmentList([FromQuery] EquipmentQueryParameters parameters)
  {
    try
    {
      var ownerId = User.FindFirst("sub")?.Value;
      if (string.IsNullOrEmpty(ownerId))
      {
        return BadRequest(new ApiResponse<PaginatedList<EquipmentViewDto>>
        {
          Success = false,
          Message = "Failed to get user information"
        });
      }

      var result = await _equipmentService.GetUserEquipmentListAsync(ownerId, parameters);
      return result.Success ? Ok(result) : BadRequest(result);
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Error occurred while retrieving user equipment list");
      return StatusCode(500, new ApiResponse<PaginatedList<EquipmentViewDto>>
      {
        Success = false,
        Message = "An error occurred while retrieving user equipment list"
      });
    }
  }

  /// <summary>
  /// 更新设备信息
  /// </summary>
  /// <param name="id">设备ID</param>
  /// <param name="request">更新设备请求</param>
  /// <returns>设备视图</returns>
  /// <response code="200">更新成功</response>
  /// <response code="400">请求参数错误</response>
  /// <response code="401">未授权</response>
  /// <response code="403">无权限</response>
  /// <response code="404">设备不存在</response>
  [HttpPut("{id}")]
  [Authorize]
  [ProducesResponseType(typeof(ApiResponse<EquipmentViewDto>), StatusCodes.Status200OK)]
  [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
  [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
  [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status403Forbidden)]
  [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
  public async Task<IActionResult> UpdateEquipment(string id, [FromBody] UpdateEquipmentRequest request)
  {
    try
    {
      var ownerId = User.FindFirst("sub")?.Value;
      if (string.IsNullOrEmpty(ownerId))
      {
        return BadRequest(new ApiResponse<EquipmentViewDto>
        {
          Success = false,
          Message = "Failed to get user information"
        });
      }

      var result = await _equipmentService.UpdateEquipmentAsync(id, request, ownerId);
      return result.Success switch
      {
        true => Ok(result),
        false => result.Message switch
        {
          "Equipment does not exist" => NotFound(result),
          "No permission to modify this equipment" => Forbid(),
          "Only equipment in available status can be modified" => BadRequest(result),
          _ => BadRequest(result)
        }
      };
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Error occurred while updating equipment: {EquipmentId}", id);
      return StatusCode(500, new ApiResponse<EquipmentViewDto>
      {
        Success = false,
        Message = "An error occurred while updating equipment"
      });
    }
  }

  /// <summary>
  /// 删除设备
  /// </summary>
  /// <param name="id">设备ID</param>
  /// <returns>操作结果</returns>
  /// <response code="200">删除成功</response>
  /// <response code="400">请求参数错误</response>
  /// <response code="401">未授权</response>
  /// <response code="403">无权限</response>
  /// <response code="404">设备不存在</response>
  /// <response code="409">设备有活跃订单</response>
  [HttpDelete("{id}")]
  [Authorize]
  [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
  [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
  [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
  [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status403Forbidden)]
  [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
  [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status409Conflict)]
  public async Task<IActionResult> DeleteEquipment(string id)
  {
    try
    {
      var ownerId = User.FindFirst("sub")?.Value;
      if (string.IsNullOrEmpty(ownerId))
      {
        return BadRequest(new ApiResponse
        {
          Success = false,
          Message = "Failed to get user information"
        });
      }

      var isAdmin = User.IsInRole("Admin");
      var result = await _equipmentService.DeleteEquipmentAsync(id, ownerId, isAdmin);
      return result.Success switch
      {
        true => Ok(result),
        false => result.Message switch
        {
          "Equipment does not exist" => NotFound(result),
          "No permission to delete this equipment" => Forbid(),
          "Equipment has active orders and cannot be deleted" => Conflict(result),
          _ => BadRequest(result)
        }
      };
    }
    catch (Exception ex)
    {
      _logger.LogError(ex, "Error occurred while deleting equipment: {EquipmentId}", id);
      return StatusCode(500, new ApiResponse
      {
        Success = false,
        Message = "An error occurred while deleting equipment"
      });
    }
  }
}
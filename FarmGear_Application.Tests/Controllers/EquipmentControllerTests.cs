using System.Security.Claims;
using FarmGear_Application.Controllers;
using FarmGear_Application.DTOs;
using FarmGear_Application.DTOs.Equipment;
using FarmGear_Application.Models;
using FarmGear_Application.Services;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace FarmGear_Application.Tests.Controllers;

/// <summary>
/// 设备控制器的单元测试类
/// 用于测试设备管理API的各个功能点，包括：
/// - 设备的CRUD操作
/// - 用户权限验证
/// - 错误处理
/// - 业务逻辑验证
/// </summary>
public class EquipmentControllerTests
{
  /// <summary>
  /// 模拟的设备服务接口，用于模拟后端服务的行为
  /// </summary>
  private readonly Mock<IEquipmentService> _mockEquipmentService;

  /// <summary>
  /// 模拟的日志记录器，用于记录控制器操作日志
  /// </summary>
  private readonly Mock<ILogger<EquipmentController>> _mockLogger;

  /// <summary>
  /// 被测试的设备控制器实例
  /// </summary>
  private readonly EquipmentController _controller;

  /// <summary>
  /// 构造函数，初始化测试环境
  /// 创建模拟对象和控制器实例
  /// </summary>
  public EquipmentControllerTests()
  {
    _mockEquipmentService = new Mock<IEquipmentService>();
    _mockLogger = new Mock<ILogger<EquipmentController>>();
    _controller = new EquipmentController(_mockEquipmentService.Object, _mockLogger.Object);
  }

  /// <summary>
  /// 设置用户上下文，模拟用户认证状态
  /// </summary>
  /// <param name="userId">用户ID，默认为"test-user-id"</param>
  /// <param name="role">用户角色，默认为"Provider"</param>
  private void SetupUserContext(string userId = "test-user-id", string role = "Provider")
  {
    var claims = new List<Claim>
        {
            new Claim("sub", userId),
            new Claim(ClaimTypes.Role, role)
        };
    var identity = new ClaimsIdentity(claims, "test");
    var principal = new ClaimsPrincipal(identity);

    _controller.ControllerContext = new ControllerContext
    {
      HttpContext = new DefaultHttpContext { User = principal }
    };
  }

  #region CreateEquipment Tests

  /// <summary>
  /// 测试创建设备 - 有效请求场景
  /// 验证：
  /// - 返回201 Created状态码
  /// - 返回正确的设备信息
  /// - 返回正确的操作消息
  /// - 返回正确的Action名称
  /// </summary>
  [Fact]
  public async Task CreateEquipment_ValidRequest_ReturnsCreatedResult()
  {
    // Arrange
    var userId = "test-user-id";
    SetupUserContext(userId);

    var request = new CreateEquipmentRequest
    {
      Name = "Test Equipment",
      Description = "Test Description",
      DailyPrice = 100.00m,
      Latitude = 39.9042,
      Longitude = 116.4074,
      Type = "Tractor"
    };

    var equipmentView = new EquipmentViewDto
    {
      Id = "equipment-id",
      Name = request.Name,
      Description = request.Description,
      DailyPrice = request.DailyPrice,
      Latitude = request.Latitude,
      Longitude = request.Longitude,
      Type = request.Type,
      OwnerId = userId,
      Status = EquipmentStatus.Available,
      CreatedAt = DateTime.UtcNow
    };

    var successResponse = new ApiResponse<EquipmentViewDto>
    {
      Success = true,
      Data = equipmentView,
      Message = "Equipment created successfully"
    };

    _mockEquipmentService
        .Setup(x => x.CreateEquipmentAsync(request, userId))
        .ReturnsAsync(successResponse);

    // Act
    var result = await _controller.CreateEquipment(request);

    // Assert
    result.Should().BeOfType<CreatedAtActionResult>();
    var createdResult = result as CreatedAtActionResult;
    createdResult!.Value.Should().BeEquivalentTo(successResponse);
    createdResult.ActionName.Should().Be(nameof(EquipmentController.GetEquipmentById));
  }

  /// <summary>
  /// 测试创建设备 - 无用户声明场景
  /// 验证：
  /// - 返回400 BadRequest状态码
  /// - 返回失败消息
  /// - 提示无法获取用户信息
  /// </summary>
  [Fact]
  public async Task CreateEquipment_NoUserClaim_ReturnsBadRequest()
  {
    // Arrange
    SetupUserContext("", "Provider"); // Empty user ID
    var request = new CreateEquipmentRequest();

    // Act
    var result = await _controller.CreateEquipment(request);

    // Assert
    result.Should().BeOfType<BadRequestObjectResult>();
    var badRequestResult = result as BadRequestObjectResult;
    var response = badRequestResult!.Value as ApiResponse<EquipmentViewDto>;
    response!.Success.Should().BeFalse();
    response.Message.Should().Be("Failed to get user information");
  }

  /// <summary>
  /// 测试创建设备 - 用户不存在场景
  /// 验证：
  /// - 返回404 NotFound状态码
  /// - 返回用户不存在的错误消息
  /// </summary>
  [Fact]
  public async Task CreateEquipment_UserNotExists_ReturnsNotFound()
  {
    // Arrange
    var userId = "test-user-id";
    SetupUserContext(userId);
    var request = new CreateEquipmentRequest();

    var errorResponse = new ApiResponse<EquipmentViewDto>
    {
      Success = false,
      Message = "User does not exist"
    };

    _mockEquipmentService
        .Setup(x => x.CreateEquipmentAsync(request, userId))
        .ReturnsAsync(errorResponse);

    // Act
    var result = await _controller.CreateEquipment(request);

    // Assert
    result.Should().BeOfType<NotFoundObjectResult>();
  }

  /// <summary>
  /// 测试创建设备 - 无权限场景
  /// 验证：
  /// - 返回403 Forbidden状态码
  /// - 返回权限不足的错误消息
  /// </summary>
  [Fact]
  public async Task CreateEquipment_NoPermission_ReturnsForbid()
  {
    // Arrange
    var userId = "test-user-id";
    SetupUserContext(userId);
    var request = new CreateEquipmentRequest();

    var errorResponse = new ApiResponse<EquipmentViewDto>
    {
      Success = false,
      Message = "Only providers and officials can create equipment"
    };

    _mockEquipmentService
        .Setup(x => x.CreateEquipmentAsync(request, userId))
        .ReturnsAsync(errorResponse);

    // Act
    var result = await _controller.CreateEquipment(request);

    // Assert
    result.Should().BeOfType<ForbidResult>();
  }

  #endregion

  #region GetEquipmentList Tests

  /// <summary>
  /// 测试获取设备列表 - 有效请求场景
  /// 验证：
  /// - 返回200 OK状态码
  /// - 返回分页的设备列表
  /// - 返回正确的操作消息
  /// </summary>
  [Fact]
  public async Task GetEquipmentList_ValidRequest_ReturnsOkResult()
  {
    // Arrange
    var parameters = new EquipmentQueryParameters
    {
      PageNumber = 1,
      PageSize = 10
    };

    var equipmentList = new List<EquipmentViewDto>
        {
            new EquipmentViewDto
            {
                Id = "equipment-1",
                Name = "Test Equipment 1",
                Description = "Description 1",
                DailyPrice = 100.00m,
                Status = EquipmentStatus.Available
            }
        };

    var paginatedList = new PaginatedList<EquipmentViewDto>(
        equipmentList, 1, parameters.PageNumber, parameters.PageSize);

    var successResponse = new ApiResponse<PaginatedList<EquipmentViewDto>>
    {
      Success = true,
      Data = paginatedList,
      Message = "Equipment list retrieved successfully"
    };

    _mockEquipmentService
        .Setup(x => x.GetEquipmentListAsync(parameters))
        .ReturnsAsync(successResponse);

    // Act
    var result = await _controller.GetEquipmentList(parameters);

    // Assert
    result.Should().BeOfType<OkObjectResult>();
    var okResult = result as OkObjectResult;
    okResult!.Value.Should().BeEquivalentTo(successResponse);
  }

  /// <summary>
  /// 测试获取设备列表 - 服务错误场景
  /// 验证：
  /// - 返回400 BadRequest状态码
  /// - 返回服务错误消息
  /// </summary>
  [Fact]
  public async Task GetEquipmentList_ServiceError_ReturnsBadRequest()
  {
    // Arrange
    var parameters = new EquipmentQueryParameters();
    var errorResponse = new ApiResponse<PaginatedList<EquipmentViewDto>>
    {
      Success = false,
      Message = "Invalid parameters"
    };

    _mockEquipmentService
        .Setup(x => x.GetEquipmentListAsync(parameters))
        .ReturnsAsync(errorResponse);

    // Act
    var result = await _controller.GetEquipmentList(parameters);

    // Assert
    result.Should().BeOfType<BadRequestObjectResult>();
  }

  #endregion

  #region GetEquipmentById Tests

  /// <summary>
  /// 测试获取单个设备 - 有效ID场景
  /// 验证：
  /// - 返回200 OK状态码
  /// - 返回正确的设备信息
  /// - 返回正确的操作消息
  /// </summary>
  [Fact]
  public async Task GetEquipmentById_ValidId_ReturnsOkResult()
  {
    // Arrange
    var equipmentId = "equipment-id";
    var equipmentView = new EquipmentViewDto
    {
      Id = equipmentId,
      Name = "Test Equipment",
      Description = "Test Description",
      DailyPrice = 100.00m,
      Status = EquipmentStatus.Available
    };

    var successResponse = new ApiResponse<EquipmentViewDto>
    {
      Success = true,
      Data = equipmentView,
      Message = "Equipment retrieved successfully"
    };

    _mockEquipmentService
        .Setup(x => x.GetEquipmentByIdAsync(equipmentId))
        .ReturnsAsync(successResponse);

    // Act
    var result = await _controller.GetEquipmentById(equipmentId);

    // Assert
    result.Should().BeOfType<OkObjectResult>();
    var okResult = result as OkObjectResult;
    okResult!.Value.Should().BeEquivalentTo(successResponse);
  }

  /// <summary>
  /// 测试获取单个设备 - 设备不存在场景
  /// 验证：
  /// - 返回404 NotFound状态码
  /// - 返回设备不存在的错误消息
  /// </summary>
  [Fact]
  public async Task GetEquipmentById_EquipmentNotExists_ReturnsNotFound()
  {
    // Arrange
    var equipmentId = "non-existent-id";
    var errorResponse = new ApiResponse<EquipmentViewDto>
    {
      Success = false,
      Message = "Equipment does not exist"
    };

    _mockEquipmentService
        .Setup(x => x.GetEquipmentByIdAsync(equipmentId))
        .ReturnsAsync(errorResponse);

    // Act
    var result = await _controller.GetEquipmentById(equipmentId);

    // Assert
    result.Should().BeOfType<NotFoundObjectResult>();
  }

  #endregion

  #region GetMyEquipmentList Tests

  /// <summary>
  /// 测试获取我的设备列表 - 有效请求场景
  /// 验证：
  /// - 返回200 OK状态码
  /// - 返回当前用户的设备列表
  /// - 返回正确的操作消息
  /// </summary>
  [Fact]
  public async Task GetMyEquipmentList_ValidRequest_ReturnsOkResult()
  {
    // Arrange
    var userId = "test-user-id";
    SetupUserContext(userId);
    var parameters = new EquipmentQueryParameters();

    var equipmentList = new List<EquipmentViewDto>
        {
            new EquipmentViewDto
            {
                Id = "equipment-1",
                Name = "My Equipment",
                OwnerId = userId,
                Status = EquipmentStatus.Available
            }
        };

    var paginatedList = new PaginatedList<EquipmentViewDto>(
        equipmentList, 1, parameters.PageNumber, parameters.PageSize);

    var successResponse = new ApiResponse<PaginatedList<EquipmentViewDto>>
    {
      Success = true,
      Data = paginatedList,
      Message = "User equipment list retrieved successfully"
    };

    _mockEquipmentService
        .Setup(x => x.GetUserEquipmentListAsync(userId, parameters))
        .ReturnsAsync(successResponse);

    // Act
    var result = await _controller.GetMyEquipmentList(parameters);

    // Assert
    result.Should().BeOfType<OkObjectResult>();
    var okResult = result as OkObjectResult;
    okResult!.Value.Should().BeEquivalentTo(successResponse);
  }

  /// <summary>
  /// 测试获取我的设备列表 - 无用户声明场景
  /// 验证：
  /// - 返回400 BadRequest状态码
  /// - 返回无法获取用户信息的错误消息
  /// </summary>
  [Fact]
  public async Task GetMyEquipmentList_NoUserClaim_ReturnsBadRequest()
  {
    // Arrange
    SetupUserContext(""); // Empty user ID
    var parameters = new EquipmentQueryParameters();

    // Act
    var result = await _controller.GetMyEquipmentList(parameters);

    // Assert
    result.Should().BeOfType<BadRequestObjectResult>();
    var badRequestResult = result as BadRequestObjectResult;
    var response = badRequestResult!.Value as ApiResponse<PaginatedList<EquipmentViewDto>>;
    response!.Success.Should().BeFalse();
    response.Message.Should().Be("Failed to get user information");
  }

  #endregion

  #region UpdateEquipment Tests

  /// <summary>
  /// 测试更新设备 - 有效请求场景
  /// 验证：
  /// - 返回200 OK状态码
  /// - 返回更新后的设备信息
  /// - 返回正确的操作消息
  /// </summary>
  [Fact]
  public async Task UpdateEquipment_ValidRequest_ReturnsOkResult()
  {
    // Arrange
    var userId = "test-user-id";
    var equipmentId = "equipment-id";
    SetupUserContext(userId);

    var request = new UpdateEquipmentRequest
    {
      Name = "Updated Equipment",
      Description = "Updated Description",
      DailyPrice = 150.00m,
      Latitude = 39.9042,
      Longitude = 116.4074,
      Status = EquipmentStatus.Available,
      Type = "Updated Tractor"
    };

    var updatedEquipment = new EquipmentViewDto
    {
      Id = equipmentId,
      Name = request.Name,
      Description = request.Description,
      DailyPrice = request.DailyPrice,
      Latitude = request.Latitude,
      Longitude = request.Longitude,
      Status = request.Status,
      Type = request.Type,
      OwnerId = userId
    };

    var successResponse = new ApiResponse<EquipmentViewDto>
    {
      Success = true,
      Data = updatedEquipment,
      Message = "Equipment updated successfully"
    };

    _mockEquipmentService
        .Setup(x => x.UpdateEquipmentAsync(equipmentId, request, userId))
        .ReturnsAsync(successResponse);

    // Act
    var result = await _controller.UpdateEquipment(equipmentId, request);

    // Assert
    result.Should().BeOfType<OkObjectResult>();
    var okResult = result as OkObjectResult;
    okResult!.Value.Should().BeEquivalentTo(successResponse);
  }

  /// <summary>
  /// 测试更新设备 - 设备不存在场景
  /// 验证：
  /// - 返回404 NotFound状态码
  /// - 返回设备不存在的错误消息
  /// </summary>
  [Fact]
  public async Task UpdateEquipment_EquipmentNotExists_ReturnsNotFound()
  {
    // Arrange
    var userId = "test-user-id";
    var equipmentId = "non-existent-id";
    SetupUserContext(userId);
    var request = new UpdateEquipmentRequest();

    var errorResponse = new ApiResponse<EquipmentViewDto>
    {
      Success = false,
      Message = "Equipment does not exist"
    };

    _mockEquipmentService
        .Setup(x => x.UpdateEquipmentAsync(equipmentId, request, userId))
        .ReturnsAsync(errorResponse);

    // Act
    var result = await _controller.UpdateEquipment(equipmentId, request);

    // Assert
    result.Should().BeOfType<NotFoundObjectResult>();
  }

  /// <summary>
  /// 测试更新设备 - 无权限场景
  /// 验证：
  /// - 返回403 Forbidden状态码
  /// - 返回权限不足的错误消息
  /// </summary>
  [Fact]
  public async Task UpdateEquipment_NoPermission_ReturnsForbid()
  {
    // Arrange
    var userId = "test-user-id";
    var equipmentId = "equipment-id";
    SetupUserContext(userId);
    var request = new UpdateEquipmentRequest();

    var errorResponse = new ApiResponse<EquipmentViewDto>
    {
      Success = false,
      Message = "No permission to modify this equipment"
    };

    _mockEquipmentService
        .Setup(x => x.UpdateEquipmentAsync(equipmentId, request, userId))
        .ReturnsAsync(errorResponse);

    // Act
    var result = await _controller.UpdateEquipment(equipmentId, request);

    // Assert
    result.Should().BeOfType<ForbidResult>();
  }

  #endregion

  #region DeleteEquipment Tests

  /// <summary>
  /// 测试删除设备 - 有效请求场景
  /// 验证：
  /// - 返回200 OK状态码
  /// - 返回删除成功的消息
  /// </summary>
  [Fact]
  public async Task DeleteEquipment_ValidRequest_ReturnsOkResult()
  {
    // Arrange
    var userId = "test-user-id";
    var equipmentId = "equipment-id";
    SetupUserContext(userId);

    var successResponse = new ApiResponse
    {
      Success = true,
      Message = "Equipment deleted successfully"
    };

    _mockEquipmentService
        .Setup(x => x.DeleteEquipmentAsync(equipmentId, userId, false))
        .ReturnsAsync(successResponse);

    // Act
    var result = await _controller.DeleteEquipment(equipmentId);

    // Assert
    result.Should().BeOfType<OkObjectResult>();
    var okResult = result as OkObjectResult;
    okResult!.Value.Should().BeEquivalentTo(successResponse);
  }

  /// <summary>
  /// 测试删除设备 - 设备不存在场景
  /// 验证：
  /// - 返回404 NotFound状态码
  /// - 返回设备不存在的错误消息
  /// </summary>
  [Fact]
  public async Task DeleteEquipment_EquipmentNotExists_ReturnsNotFound()
  {
    // Arrange
    var userId = "test-user-id";
    var equipmentId = "non-existent-id";
    SetupUserContext(userId);

    var errorResponse = new ApiResponse
    {
      Success = false,
      Message = "Equipment does not exist"
    };

    _mockEquipmentService
        .Setup(x => x.DeleteEquipmentAsync(equipmentId, userId, false))
        .ReturnsAsync(errorResponse);

    // Act
    var result = await _controller.DeleteEquipment(equipmentId);

    // Assert
    result.Should().BeOfType<NotFoundObjectResult>();
  }

  /// <summary>
  /// 测试删除设备 - 无权限场景
  /// 验证：
  /// - 返回403 Forbidden状态码
  /// - 返回权限不足的错误消息
  /// </summary>
  [Fact]
  public async Task DeleteEquipment_NoPermission_ReturnsForbid()
  {
    // Arrange
    var userId = "test-user-id";
    var equipmentId = "equipment-id";
    SetupUserContext(userId);

    var errorResponse = new ApiResponse
    {
      Success = false,
      Message = "No permission to delete this equipment"
    };

    _mockEquipmentService
        .Setup(x => x.DeleteEquipmentAsync(equipmentId, userId, false))
        .ReturnsAsync(errorResponse);

    // Act
    var result = await _controller.DeleteEquipment(equipmentId);

    // Assert
    result.Should().BeOfType<ForbidResult>();
  }

  /// <summary>
  /// 测试删除设备 - 有活动订单场景
  /// 验证：
  /// - 返回409 Conflict状态码
  /// - 返回设备有活动订单的错误消息
  /// </summary>
  [Fact]
  public async Task DeleteEquipment_HasActiveOrders_ReturnsConflict()
  {
    // Arrange
    var userId = "test-user-id";
    var equipmentId = "equipment-id";
    SetupUserContext(userId);

    var errorResponse = new ApiResponse
    {
      Success = false,
      Message = "Equipment has active orders and cannot be deleted"
    };

    _mockEquipmentService
        .Setup(x => x.DeleteEquipmentAsync(equipmentId, userId, false))
        .ReturnsAsync(errorResponse);

    // Act
    var result = await _controller.DeleteEquipment(equipmentId);

    // Assert
    result.Should().BeOfType<ConflictObjectResult>();
  }

  /// <summary>
  /// 测试删除设备 - 管理员操作场景
  /// 验证：
  /// - 返回200 OK状态码
  /// - 使用管理员标志调用服务
  /// - 返回删除成功的消息
  /// </summary>
  [Fact]
  public async Task DeleteEquipment_AsAdmin_CallsServiceWithAdminFlag()
  {
    // Arrange
    var userId = "admin-user-id";
    var equipmentId = "equipment-id";
    SetupUserContext(userId, "Admin");

    var successResponse = new ApiResponse
    {
      Success = true,
      Message = "Equipment deleted successfully"
    };

    _mockEquipmentService
        .Setup(x => x.DeleteEquipmentAsync(equipmentId, userId, true))
        .ReturnsAsync(successResponse);

    // Act
    var result = await _controller.DeleteEquipment(equipmentId);

    // Assert
    result.Should().BeOfType<OkObjectResult>();
    _mockEquipmentService.Verify(x => x.DeleteEquipmentAsync(equipmentId, userId, true), Times.Once);
  }

  #endregion

  #region Exception Handling Tests

  /// <summary>
  /// 测试创建设备 - 异常处理场景
  /// 验证：
  /// - 返回500 InternalServerError状态码
  /// - 正确处理服务层抛出的异常
  /// </summary>
  [Fact]
  public async Task CreateEquipment_ExceptionThrown_ReturnsInternalServerError()
  {
    // Arrange
    var userId = "test-user-id";
    SetupUserContext(userId);
    var request = new CreateEquipmentRequest();

    _mockEquipmentService
        .Setup(x => x.CreateEquipmentAsync(request, userId))
        .ThrowsAsync(new Exception("Database error"));

    // Act
    var result = await _controller.CreateEquipment(request);

    // Assert
    result.Should().BeOfType<ObjectResult>();
    var objectResult = result as ObjectResult;
    objectResult!.StatusCode.Should().Be(500);
  }

  /// <summary>
  /// 测试获取设备列表 - 异常处理场景
  /// 验证：
  /// - 返回500 InternalServerError状态码
  /// - 正确处理服务层抛出的异常
  /// </summary>
  [Fact]
  public async Task GetEquipmentList_ExceptionThrown_ReturnsInternalServerError()
  {
    // Arrange
    var parameters = new EquipmentQueryParameters();

    _mockEquipmentService
        .Setup(x => x.GetEquipmentListAsync(parameters))
        .ThrowsAsync(new Exception("Database error"));

    // Act
    var result = await _controller.GetEquipmentList(parameters);

    // Assert
    result.Should().BeOfType<ObjectResult>();
    var objectResult = result as ObjectResult;
    objectResult!.StatusCode.Should().Be(500);
  }

  #endregion
}
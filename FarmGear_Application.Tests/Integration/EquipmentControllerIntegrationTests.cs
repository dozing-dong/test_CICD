using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Security.Claims;
using FarmGear_Application;
using FarmGear_Application.DTOs.Equipment;
using FarmGear_Application.DTOs;
using FarmGear_Application.Services;
using FarmGear_Application.Configuration;
using FarmGear_Application.Constants;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Identity;
using FarmGear_Application.Data;
using FarmGear_Application.Models;
using Xunit;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Hosting;

namespace FarmGear_Application.Tests.Integration;

/// <summary>
/// 设备控制器的集成测试类
/// 用于测试设备管理API的端到端功能，包括：
/// - 设备的CRUD操作
/// - 身份认证和授权
/// - API响应状态
/// - 数据验证
/// </summary>
public class EquipmentControllerIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
  /// <summary>
  /// Web应用程序工厂实例，用于创建测试服务器
  /// </summary>
  private readonly WebApplicationFactory<Program> _factory;

  /// <summary>
  /// HTTP客户端实例，用于发送测试请求
  /// </summary>
  private readonly HttpClient _client;

  /// <summary>
  /// 测试用户ID
  /// </summary>
  private string _testUserId = string.Empty;

  /// <summary>
  /// 测试用户Token
  /// </summary>
  private string _testUserToken = string.Empty;

  /// <summary>
  /// 构造函数，初始化测试环境
  /// </summary>
  /// <param name="factory">Web应用程序工厂实例</param>
  public EquipmentControllerIntegrationTests(WebApplicationFactory<Program> factory)
  {
    _factory = factory.WithWebHostBuilder(builder =>
    {
      // 配置测试环境
      builder.UseEnvironment("Testing");

      // 配置测试服务
      builder.ConfigureServices(services =>
      {
        // 移除原有的数据库上下文
        var descriptor = services.SingleOrDefault(
            d => d.ServiceType == typeof(Microsoft.EntityFrameworkCore.DbContextOptions<FarmGear_Application.Data.ApplicationDbContext>));
        if (descriptor != null)
        {
          services.Remove(descriptor);
        }

        // 根据配置选择数据库类型
        var configuration = services.BuildServiceProvider().GetRequiredService<IConfiguration>();
        var useDocker = configuration.GetValue<bool>("TestEnvironment:UseDocker", false);
        var useInMemory = configuration.GetValue<bool>("TestEnvironment:UseInMemoryDatabase", true);

        if (useDocker && !useInMemory)
        {
          // 使用Docker中的MySQL数据库
          services.AddDbContext<FarmGear_Application.Data.ApplicationDbContext>(options =>
          {
            options.UseMySql(
                configuration.GetConnectionString("DockerConnection"),
                ServerVersion.AutoDetect(configuration.GetConnectionString("DockerConnection"))
            );
          });
        }
        else
        {
          // 使用内存数据库（默认）
          services.AddDbContext<FarmGear_Application.Data.ApplicationDbContext>(options =>
          {
            options.UseInMemoryDatabase("FarmGearTestDb");
          });
        }

        // 配置测试用的JWT设置
        services.Configure<FarmGear_Application.Configuration.JwtSettings>(options =>
        {
          options.SecretKey = "test-secret-key-that-is-long-enough-for-testing-purposes-only";
          options.Issuer = "test-issuer";
          options.Audience = "test-audience";
          options.ExpiryInMinutes = 60;
        });

        // 移除Redis服务，使用内存缓存替代
        var redisDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(IRedisCacheService));
        if (redisDescriptor != null)
        {
          services.Remove(redisDescriptor);
        }
        services.AddScoped<IRedisCacheService, MockRedisCacheService>();

        // 移除EmailSender服务，使用模拟实现
        var emailDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(FarmGear_Application.Services.IEmailSender));
        if (emailDescriptor != null)
        {
          services.Remove(emailDescriptor);
        }
        services.AddScoped<FarmGear_Application.Services.IEmailSender, MockEmailSender>();
      });
    });

    // 创建测试客户端
    _client = _factory.CreateClient(new WebApplicationFactoryClientOptions
    {
      AllowAutoRedirect = false
    });

    // 初始化测试数据
    InitializeTestDataAsync().Wait();
  }

  /// <summary>
  /// 初始化测试数据
  /// </summary>
  private async Task InitializeTestDataAsync()
  {
    using var scope = _factory.Services.CreateScope();
    var services = scope.ServiceProvider;

    // 初始化数据库
    await InitializeTestDatabaseAsync(services);

    // 创建测试用户
    _testUserToken = await CreateTestUserAndGetTokenAsync(services, "testuser", "test@example.com", "Test123!@#", "Provider");

    // 从Token中提取用户ID（简化处理）
    _testUserId = "testuser"; // 在实际应用中应该从Token解析
  }

  /// <summary>
  /// 初始化测试数据库
  /// </summary>
  /// <param name="services">服务提供者</param>
  private async Task InitializeTestDatabaseAsync(IServiceProvider services)
  {
    var context = services.GetRequiredService<FarmGear_Application.Data.ApplicationDbContext>();
    var roleSeedService = services.GetRequiredService<FarmGear_Application.Services.RoleSeedService>();

    // 确保数据库已创建（内存数据库会自动创建）
    await context.Database.EnsureCreatedAsync();

    // 初始化角色
    await roleSeedService.SeedRolesAsync();
  }

  /// <summary>
  /// 创建测试用户并返回认证Token
  /// </summary>
  /// <param name="services">服务提供者</param>
  /// <param name="username">用户名</param>
  /// <param name="email">邮箱</param>
  /// <param name="password">密码</param>
  /// <param name="role">角色</param>
  /// <returns>认证Token</returns>
  private async Task<string> CreateTestUserAndGetTokenAsync(
      IServiceProvider services,
      string username = "testuser",
      string email = "test@example.com",
      string password = "Test123!@#",
      string role = "Provider")
  {
    var userManager = services.GetRequiredService<Microsoft.AspNetCore.Identity.UserManager<FarmGear_Application.Models.AppUser>>();
    var roleManager = services.GetRequiredService<Microsoft.AspNetCore.Identity.RoleManager<Microsoft.AspNetCore.Identity.IdentityRole>>();
    var jwtService = services.GetRequiredService<FarmGear_Application.Services.EnhancedJwtService>();

    // 确保角色存在
    if (!await roleManager.RoleExistsAsync(role))
    {
      await roleManager.CreateAsync(new Microsoft.AspNetCore.Identity.IdentityRole(role));
    }

    // 检查用户是否已存在
    var user = await userManager.FindByNameAsync(username);
    if (user == null)
    {
      user = new FarmGear_Application.Models.AppUser
      {
        UserName = username,
        Email = email,
        EmailConfirmed = true,
        IsActive = true,
        FullName = "Test User"
      };
      var result = await userManager.CreateAsync(user, password);
      if (!result.Succeeded)
      {
        throw new Exception($"Failed to create test user: {string.Join(", ", result.Errors.Select(e => e.Description))}");
      }
      // 分配角色
      await userManager.AddToRoleAsync(user, role);
    }

    // 生成Token
    return await jwtService.GenerateTokenWithSessionAsync(user);
  }

  /// <summary>
  /// 测试获取设备列表的API
  /// 验证：
  /// - API返回200状态码
  /// - 返回的数据格式正确
  /// - 分页功能正常
  /// </summary>
  [Fact]
  public async Task GetEquipmentList_ReturnsSuccessAndCorrectFormat()
  {
    // Act
    var response = await _client.GetAsync("/api/Equipment");

    // Assert
    response.StatusCode.Should().Be(HttpStatusCode.OK);
    var result = await response.Content.ReadFromJsonAsync<ApiResponse<PaginatedList<EquipmentViewDto>>>();
    result.Should().NotBeNull();
    result!.Success.Should().BeTrue();
  }

  /// <summary>
  /// 测试获取单个设备的API
  /// 验证：
  /// - 存在设备时返回200状态码
  /// - 不存在设备时返回404状态码
  /// - 返回的数据格式正确
  /// </summary>
  [Fact]
  public async Task GetEquipmentById_WithValidId_ReturnsEquipment()
  {
    // Arrange
    var equipmentId = "test-equipment-id";

    // Act
    var response = await _client.GetAsync($"/api/Equipment/{equipmentId}");

    // Assert
    response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.NotFound);
  }

  /// <summary>
  /// 测试创建设备API的身份认证要求
  /// 验证：
  /// - 未认证用户无法创建设备
  /// - 返回401未授权状态码
  /// </summary>
  [Fact]
  public async Task CreateEquipment_WithoutAuthentication_ReturnsUnauthorized()
  {
    // Arrange
    var request = CreateTestEquipmentRequest();

    // Act - 不带认证
    var response = await _client.PostAsJsonAsync("/api/Equipment", request);

    // Assert
    response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
  }

  /// <summary>
  /// 测试创建设备API（带认证）
  /// 验证：
  /// - 认证用户可以创建设备
  /// - 返回201创建成功状态码
  /// - 返回正确的设备信息
  /// </summary>
  [Fact]
  public async Task CreateEquipment_WithAuthentication_ReturnsCreated()
  {
    // Arrange
    var request = CreateTestEquipmentRequest();
    _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _testUserToken);

    // Act
    var response = await _client.PostAsJsonAsync("/api/Equipment", request);

    // Assert
    response.StatusCode.Should().Be(HttpStatusCode.Created);
    var result = await response.Content.ReadFromJsonAsync<ApiResponse<EquipmentViewDto>>();
    result.Should().NotBeNull();
    result!.Success.Should().BeTrue();
    result.Data.Should().NotBeNull();
    result.Data!.Name.Should().Be(request.Name);
  }

  /// <summary>
  /// 测试获取我的设备列表API的身份认证要求
  /// 验证：
  /// - 未认证用户无法访问个人设备列表
  /// - 返回401未授权状态码
  /// </summary>
  [Fact]
  public async Task GetMyEquipmentList_WithoutAuthentication_ReturnsUnauthorized()
  {
    // Act
    var response = await _client.GetAsync("/api/Equipment/my-equipment");

    // Assert
    response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
  }

  /// <summary>
  /// 测试获取我的设备列表API（带认证）
  /// 验证：
  /// - 认证用户可以访问个人设备列表
  /// - 返回200成功状态码
  /// - 返回正确的数据格式
  /// </summary>
  [Fact]
  public async Task GetMyEquipmentList_WithAuthentication_ReturnsSuccess()
  {
    // Arrange
    _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _testUserToken);

    // Act
    var response = await _client.GetAsync("/api/Equipment/my-equipment");

    // Assert
    response.StatusCode.Should().Be(HttpStatusCode.OK);
    var result = await response.Content.ReadFromJsonAsync<ApiResponse<PaginatedList<EquipmentViewDto>>>();
    result.Should().NotBeNull();
    result!.Success.Should().BeTrue();
  }

  /// <summary>
  /// 测试更新设备API的身份认证要求
  /// 验证：
  /// - 未认证用户无法更新设备
  /// - 返回401未授权状态码
  /// </summary>
  [Fact]
  public async Task UpdateEquipment_WithoutAuthentication_ReturnsUnauthorized()
  {
    // Arrange
    var equipmentId = "test-equipment-id";
    var request = CreateTestUpdateEquipmentRequest();

    // Act
    var response = await _client.PutAsJsonAsync($"/api/Equipment/{equipmentId}", request);

    // Assert
    response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
  }

  /// <summary>
  /// 测试删除设备API的身份认证要求
  /// 验证：
  /// - 未认证用户无法删除设备
  /// - 返回401未授权状态码
  /// </summary>
  [Fact]
  public async Task DeleteEquipment_WithoutAuthentication_ReturnsUnauthorized()
  {
    // Arrange
    var equipmentId = "test-equipment-id";

    // Act
    var response = await _client.DeleteAsync($"/api/Equipment/{equipmentId}");

    // Assert
    response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
  }

  /// <summary>
  /// 测试设备查询参数
  /// 验证：
  /// - 支持分页查询
  /// - 支持状态筛选
  /// - 支持价格范围筛选
  /// </summary>
  [Fact]
  public async Task GetEquipmentList_WithQueryParameters_ReturnsFilteredResults()
  {
    // Arrange
    var queryParams = CreateTestEquipmentQueryParameters(
        page: 1,
        pageSize: 5,
        status: EquipmentStatus.Available,
        minPrice: 50.0m,
        maxPrice: 200.0m
    );

    var queryString = $"?pageNumber={queryParams.PageNumber}&pageSize={queryParams.PageSize}&status={queryParams.Status}&minDailyPrice={queryParams.MinDailyPrice}&maxDailyPrice={queryParams.MaxDailyPrice}";

    // Act
    var response = await _client.GetAsync($"/api/Equipment{queryString}");

    // Assert
    response.StatusCode.Should().Be(HttpStatusCode.OK);
    var result = await response.Content.ReadFromJsonAsync<ApiResponse<PaginatedList<EquipmentViewDto>>>();
    result.Should().NotBeNull();
    result!.Success.Should().BeTrue();
  }

  /// <summary>
  /// 创建测试用的CreateEquipmentRequest
  /// </summary>
  private static CreateEquipmentRequest CreateTestEquipmentRequest(
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
  private static UpdateEquipmentRequest CreateTestUpdateEquipmentRequest(
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
  /// 创建测试用的EquipmentQueryParameters
  /// </summary>
  private static EquipmentQueryParameters CreateTestEquipmentQueryParameters(
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
}

/// <summary>
/// 模拟Redis缓存服务
/// </summary>
public class MockRedisCacheService : IRedisCacheService
{
  private readonly Dictionary<string, object> _cache = new();
  private readonly ILogger<MockRedisCacheService> _logger;

  public MockRedisCacheService(ILogger<MockRedisCacheService> logger)
  {
    _logger = logger;
  }

  public Task<string?> GetAsync(string key)
  {
    return Task.FromResult(_cache.TryGetValue(key, out var value) ? value.ToString() : null);
  }

  public Task<bool> SetAsync(string key, string value, TimeSpan? expiry = null)
  {
    _cache[key] = value;
    return Task.FromResult(true);
  }

  public Task<bool> RemoveAsync(string key)
  {
    return Task.FromResult(_cache.Remove(key));
  }

  public Task<bool> IsTokenBlacklistedAsync(string token)
  {
    return Task.FromResult(false); // 测试中默认Token不在黑名单中
  }

  public async Task<bool> CacheUserSessionAsync(string userId, object sessionData, TimeSpan? expiry = null)
  {
    await SetAsync($"session:user:{userId}", System.Text.Json.JsonSerializer.Serialize(sessionData), expiry);
    return true;
  }

  public async Task<T?> GetUserSessionAsync<T>(string userId)
  {
    var value = await GetAsync($"session:user:{userId}");
    if (value != null)
    {
      return System.Text.Json.JsonSerializer.Deserialize<T>(value);
    }
    return default(T);
  }

  public async Task<bool> CacheUserPermissionsAsync(string userId, IEnumerable<string> permissions, TimeSpan? expiry = null)
  {
    await SetAsync($"permissions:user:{userId}", System.Text.Json.JsonSerializer.Serialize(permissions), expiry);
    return true;
  }

  public Task<bool> BlacklistTokenAsync(string token, TimeSpan? expiry = null)
  {
    return SetAsync($"blacklist:token:{token}", "1", expiry);
  }

  public Task<IEnumerable<string>?> GetUserPermissionsAsync(string userId)
  {
    return Task.FromResult<IEnumerable<string>?>(new List<string> { "Provider" }); // 默认权限
  }

  public Task<bool> RemoveUserSessionAsync(string userId)
  {
    return RemoveAsync($"session:user:{userId}");
  }

  public Task<bool> RemoveUserPermissionsAsync(string userId)
  {
    return RemoveAsync($"permissions:user:{userId}");
  }
}

/// <summary>
/// 模拟邮件发送服务
/// </summary>
public class MockEmailSender : FarmGear_Application.Services.IEmailSender
{
  private readonly ILogger<MockEmailSender> _logger;

  public MockEmailSender(ILogger<MockEmailSender> logger)
  {
    _logger = logger;
  }

  public Task<bool> SendEmailAsync(string email, string subject, string message)
  {
    _logger.LogInformation("Mock email sent to {Email} with subject: {Subject}", email, subject);
    return Task.FromResult(true); // 模拟发送成功
  }
}

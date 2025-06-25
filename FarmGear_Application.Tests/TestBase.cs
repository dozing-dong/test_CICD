using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Extensions.Options;
using System.Text.Encodings.Web;
using FarmGear_Application.Data;
using FarmGear_Application.Models;
using FarmGear_Application.Configuration;
using FarmGear_Application.Services;
using FarmGear_Application.DTOs.Auth;
using Microsoft.AspNetCore.Identity;

namespace FarmGear_Application.Tests;

/// <summary>
/// 测试基类，提供常用的测试辅助方法
/// </summary>
public abstract class TestBase
{
  /// <summary>
  /// 为控制器设置用户上下文
  /// </summary>
  /// <param name="controller">控制器实例</param>
  /// <param name="userId">用户ID</param>
  /// <param name="roles">用户角色</param>
  protected static void SetupUserContext(ControllerBase controller, string userId = "test-user-id", params string[] roles)
  {
    var claims = new List<Claim>
        {
            new("sub", userId),
            new(ClaimTypes.NameIdentifier, userId)
        };

    // 添加角色声明
    foreach (var role in roles)
    {
      claims.Add(new Claim(ClaimTypes.Role, role));
    }

    var identity = new ClaimsIdentity(claims, "test");
    var principal = new ClaimsPrincipal(identity);

    controller.ControllerContext = new ControllerContext
    {
      HttpContext = new DefaultHttpContext { User = principal }
    };
  }

  /// <summary>
  /// 获取测试用的设备数据
  /// </summary>
  /// <param name="id">设备ID</param>
  /// <param name="ownerId">所有者ID</param>
  /// <returns>测试设备数据</returns>
  protected static (string name, string description, decimal dailyPrice, double latitude, double longitude, string type) GetTestEquipmentData(
      string id = "test-equipment-id",
      string ownerId = "test-owner-id")
  {
    return (
        name: $"Test Equipment {id}",
        description: $"Test Description for {id}",
        dailyPrice: 100.00m,
        latitude: 39.9042,
        longitude: 116.4074,
        type: "Tractor"
    );
  }

  /// <summary>
  /// 创建测试用的WebApplicationFactory
  /// </summary>
  /// <returns>配置好的WebApplicationFactory</returns>
  protected static WebApplicationFactory<Program> CreateTestWebApplicationFactory()
  {
    return new WebApplicationFactory<Program>()
      .WithWebHostBuilder(builder =>
      {
        // 配置测试环境
        builder.UseEnvironment("Testing");

        // 配置测试服务
        builder.ConfigureServices(services =>
        {
          // 移除原有的数据库上下文
          var descriptor = services.SingleOrDefault(
              d => d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>));
          if (descriptor != null)
          {
            services.Remove(descriptor);
          }

          // 根据环境选择数据库类型
          var useInMemoryDb = Environment.GetEnvironmentVariable("USE_IN_MEMORY_DB") != "false";
          if (useInMemoryDb)
          {
            // 使用内存数据库（本地开发测试）
            services.AddDbContext<ApplicationDbContext>(options =>
            {
              options.UseInMemoryDatabase("FarmGearTestDb");
            });
          }
          else
          {
            // 使用真实数据库（CI 环境）
            services.AddDbContext<ApplicationDbContext>(options =>
            {
              var connectionString = Environment.GetEnvironmentVariable("TEST_DB_CONNECTION")
                ?? "Server=localhost;Port=3307;Database=FarmGearTestDb;User=testuser;Password=test123456;";
              try
              {
                options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString),
                  mySqlOptions =>
                  {
                    mySqlOptions.EnableRetryOnFailure(3, TimeSpan.FromSeconds(5), null);
                    mySqlOptions.CommandTimeout(30);
                  });
              }
              catch (Exception)
              {
                // 如果无法连接MySQL，回退到内存数据库
                options.UseInMemoryDatabase("FarmGearTestDb_Fallback");
              }
            });
          }

          // 配置测试用的JWT设置
          services.Configure<JwtSettings>(options =>
          {
            options.SecretKey = "test-secret-key-that-is-long-enough-for-testing-purposes-only";
            options.Issuer = "test-issuer";
            options.Audience = "test-audience";
            options.ExpiryInMinutes = 60;
          });

          // 配置测试用的认证
          services.AddAuthentication("Test")
                  .AddScheme<AuthenticationSchemeOptions, TestAuthenticationHandler>("Test", options => { });

          // 配置测试用的授权
          services.AddAuthorization();

          // 根据环境选择 Redis 服务
          var useMockRedis = Environment.GetEnvironmentVariable("USE_MOCK_REDIS") != "false";
          if (useMockRedis)
          {
            // 移除Redis服务，使用内存缓存替代
            var redisDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(IRedisCacheService));
            if (redisDescriptor != null)
            {
              services.Remove(redisDescriptor);
            }
            services.AddScoped<IRedisCacheService, MockRedisCacheService>();
          }
          // 否则使用真实的 RedisCacheService（CI 环境）
        });
      });
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
  protected static async Task<string> CreateTestUserAndGetTokenAsync(
      IServiceProvider services,
      string username = "testuser",
      string email = "test@example.com",
      string password = "Test123!@#",
      string role = "Provider")
  {
    var userManager = services.GetRequiredService<UserManager<AppUser>>();
    var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
    var jwtService = services.GetRequiredService<EnhancedJwtService>();

    // 确保角色存在
    if (!await roleManager.RoleExistsAsync(role))
    {
      await roleManager.CreateAsync(new IdentityRole(role));
    }

    // 检查用户是否已存在
    var user = await userManager.FindByNameAsync(username);
    if (user == null)
    {
      user = new AppUser
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
  /// 初始化测试数据库
  /// </summary>
  /// <param name="services">服务提供者</param>
  protected static async Task InitializeTestDatabaseAsync(IServiceProvider services)
  {
    var context = services.GetRequiredService<ApplicationDbContext>();
    var roleSeedService = services.GetRequiredService<RoleSeedService>();

    // 确保数据库已创建
    await context.Database.EnsureCreatedAsync();

    // 初始化角色
    await roleSeedService.SeedRolesAsync();
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
/// 测试认证处理器
/// </summary>
public class TestAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
  public TestAuthenticationHandler(
      IOptionsMonitor<AuthenticationSchemeOptions> options,
      ILoggerFactory logger,
      UrlEncoder encoder)
      : base(options, logger, encoder)
  {
    // 设置 TimeProvider
    Options.TimeProvider = timeProvider;
  }

  protected override Task<AuthenticateResult> HandleAuthenticateAsync()
  {
    // 在测试中可以根据需要返回不同的认证结果
    return Task.FromResult(AuthenticateResult.NoResult());
  }
}
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using FarmGear_Application.Data;
using FarmGear_Application.Models;
using FarmGear_Application.Services;
using FarmGear_Application.Configuration;

namespace FarmGear_Application.Tests;

/// <summary>
/// 测试环境的启动配置类
/// 用于配置测试专用的服务和中间件
/// </summary>
public class TestStartup
{
  public TestStartup(IConfiguration configuration)
  {
    Configuration = configuration;
  }

  public IConfiguration Configuration { get; }

  public void ConfigureServices(IServiceCollection services)
  {
    // 配置数据库
    var useInMemoryDb = Environment.GetEnvironmentVariable("USE_IN_MEMORY_DB") != "false";
    if (useInMemoryDb)
    {
      services.AddDbContext<ApplicationDbContext>(options =>
          options.UseInMemoryDatabase("FarmGearTestDb"));
    }
    else
    {
      services.AddDbContext<ApplicationDbContext>(options =>
      {
        var connectionString = Environment.GetEnvironmentVariable("TEST_DB_CONNECTION")
                  ?? Configuration.GetConnectionString("DockerConnection");

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
          // 回退到内存数据库
          options.UseInMemoryDatabase("FarmGearTestDb_Fallback");
        }
      });
    }

    // 配置Identity
    services.AddIdentity<AppUser, IdentityRole>(options =>
    {
      options.Password.RequireDigit = false;
      options.Password.RequireLowercase = false;
      options.Password.RequireUppercase = false;
      options.Password.RequireNonAlphanumeric = false;
      options.Password.RequiredLength = 6;
    })
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

    // 配置JWT
    services.Configure<JwtSettings>(Configuration.GetSection("JwtSettings"));

    // 配置认证
    services.AddAuthentication("Test")
            .AddScheme<AuthenticationSchemeOptions, TestAuthenticationHandler>("Test", options => { });

    // 配置授权
    services.AddAuthorization(options =>
    {
      options.DefaultPolicy = new AuthorizationPolicyBuilder("Test")
              .RequireAuthenticatedUser()
              .Build();
    });

    // 配置Redis服务
    var useMockRedis = Environment.GetEnvironmentVariable("USE_MOCK_REDIS") != "false";
    if (useMockRedis)
    {
      services.AddScoped<IRedisCacheService, MockRedisCacheService>();
    }
    else
    {
      services.AddScoped<IRedisCacheService, RedisCacheService>();
    }

    // 注册应用服务
    services.AddScoped<IEquipmentService, EquipmentService>();
    services.AddScoped<IOrderService, OrderService>();
    services.AddScoped<IPaymentService, PaymentService>();
    services.AddScoped<IReviewService, ReviewService>();
    services.AddScoped<IAuthService, AuthService>();
    services.AddScoped<EnhancedJwtService>();

    // 配置MVC
    services.AddControllers();

    // 配置日志
    services.AddLogging(builder =>
    {
      builder.AddConsole();
      builder.SetMinimumLevel(LogLevel.Warning);
    });
  }

  public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
  {
    if (env.IsDevelopment() || env.IsEnvironment("Testing"))
    {
      app.UseDeveloperExceptionPage();
    }

    app.UseRouting();
    app.UseAuthentication();
    app.UseAuthorization();

    app.UseEndpoints(endpoints =>
    {
      endpoints.MapControllers();
    });
  }
}
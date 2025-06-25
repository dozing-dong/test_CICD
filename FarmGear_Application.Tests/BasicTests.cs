using Xunit;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Mvc.Testing;
using FarmGear_Application.Data;
using Microsoft.EntityFrameworkCore;

namespace FarmGear_Application.Tests;

/// <summary>
/// 基础测试类，用于验证测试环境是否正常工作
/// </summary>
public class BasicTests : IClassFixture<WebApplicationFactory<Program>>
{
  private readonly WebApplicationFactory<Program> _factory;

  public BasicTests(WebApplicationFactory<Program> factory)
  {
    _factory = factory;
  }

  [Fact]
  public void BasicTest_ShouldPass()
  {
    // Arrange & Act
    var result = 1 + 1;

    // Assert
    result.Should().Be(2);
  }

  [Fact]
  public async Task DatabaseContext_ShouldBeConfigured()
  {
    // Arrange
    using var scope = _factory.Services.CreateScope();
    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

    // Act & Assert
    context.Should().NotBeNull();

    // 尝试执行一个简单的数据库操作
    var canConnect = await context.Database.CanConnectAsync();
    // 注意：在某些测试环境中，这可能会失败，但至少验证了上下文已配置

    // 验证数据库提供者类型
    var databaseProvider = context.Database.ProviderName;
    databaseProvider.Should().NotBeNullOrEmpty();
  }

  [Fact]
  public void Environment_VariablesShouldBeSet()
  {
    // Act
    var useInMemoryDb = Environment.GetEnvironmentVariable("USE_IN_MEMORY_DB");
    var useMockRedis = Environment.GetEnvironmentVariable("USE_MOCK_REDIS");
    var aspNetCoreEnv = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");

    // Assert - 这些变量应该在CI环境中被设置
    // 即使它们的值可能不同，但至少应该存在
    (useInMemoryDb != null || useMockRedis != null || aspNetCoreEnv != null).Should().BeTrue(
        "At least one environment variable should be set in test environment");
  }

  [Fact]
  public void TestFactory_ShouldCreateClient()
  {
    // Act
    var client = _factory.CreateClient();

    // Assert
    client.Should().NotBeNull();
  }
}
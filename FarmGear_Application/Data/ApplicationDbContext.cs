using FarmGear_Application.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace FarmGear_Application.Data;

/// <summary>
/// 应用程序数据库上下文
/// </summary>
public class ApplicationDbContext : IdentityDbContext<AppUser>
{
  public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
      : base(options)
  {
  }

  public DbSet<Equipment> Equipment { get; set; } = null!;
  public DbSet<Order> Orders { get; set; } = null!;
  public DbSet<PaymentRecord> PaymentRecords { get; set; } = null!;
  public DbSet<Review> Reviews { get; set; } = null!;

  protected override void OnModelCreating(ModelBuilder builder)
  {
    base.OnModelCreating(builder);

    // 配置用户表的索引
    builder.Entity<AppUser>(entity =>
    {
      entity.HasIndex(u => u.Email).IsUnique();
      entity.HasIndex(u => u.UserName).IsUnique();
      entity.HasIndex(u => u.CreatedAt);
      entity.HasIndex(u => u.LastLoginAt);

      // 配置空间索引
      entity.HasIndex(u => new { u.Lng, u.Lat });

      // 配置属性
      entity.Property(u => u.FullName).IsRequired().HasMaxLength(100);
      entity.Property(u => u.IsActive).HasDefaultValue(true);
      entity.Property(u => u.CreatedAt).IsRequired();
      entity.Property(u => u.Lat).HasColumnType("decimal(10,6)");
      entity.Property(u => u.Lng).HasColumnType("decimal(10,6)");
    });

    // 配置 Equipment 实体
    builder.Entity<Equipment>(entity =>
    {
      entity.HasKey(e => e.Id);
      entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
      entity.Property(e => e.Description).HasMaxLength(500);
      entity.Property(e => e.DailyPrice).HasColumnType("decimal(18,2)");
      entity.Property(e => e.Latitude).HasColumnType("decimal(10,6)");
      entity.Property(e => e.Longitude).HasColumnType("decimal(10,6)");
      entity.Property(e => e.Status).IsRequired();
      entity.Property(e => e.OwnerId).IsRequired();
      entity.Property(e => e.CreatedAt).IsRequired();
      entity.Property(e => e.AverageRating)
          .HasColumnType("decimal(3,2)")
          .HasDefaultValue(0.0m)
          .IsRequired();

      // 配置与 AppUser 的关系
      entity.HasOne(e => e.Owner)
          .WithMany()
          .HasForeignKey(e => e.OwnerId)
          .OnDelete(DeleteBehavior.Restrict);

      // 配置空间索引
      entity.HasIndex(e => new { e.Longitude, e.Latitude });
    });

    // 配置 Order 实体
    builder.Entity<Order>(entity =>
    {
      entity.HasKey(e => e.Id);
      entity.Property(e => e.TotalAmount)
          .HasColumnType("decimal(18,2)")
          .HasDefaultValue(0m);
      entity.Property(e => e.Status).HasConversion<int>();
      entity.Property(e => e.StartDate).IsRequired();
      entity.Property(e => e.EndDate).IsRequired();
      entity.Property(e => e.CreatedAt).IsRequired();
      entity.Property(e => e.UpdatedAt)
          .IsRequired(false)
          .ValueGeneratedOnAddOrUpdate();

      // 配置与 Equipment 的关系
      entity.HasOne(e => e.Equipment)
          .WithMany()
          .HasForeignKey(e => e.EquipmentId)
          .OnDelete(DeleteBehavior.Restrict);

      // 配置与 Renter 的关系
      entity.HasOne(e => e.Renter)
          .WithMany()
          .HasForeignKey(e => e.RenterId)
          .OnDelete(DeleteBehavior.Restrict);

      // 添加索引
      entity.HasIndex(e => e.EquipmentId);
      entity.HasIndex(e => e.RenterId);
      entity.HasIndex(e => e.Status);
      entity.HasIndex(e => e.CreatedAt);
      entity.HasIndex(e => e.StartDate);
      entity.HasIndex(e => e.EndDate);
    });

    // 配置支付记录
    builder.Entity<PaymentRecord>(entity =>
    {
      entity.HasKey(p => p.Id);
      entity.Property(p => p.Amount).HasColumnType("decimal(18,2)");
      entity.Property(p => p.Status).HasConversion<int>();
      entity.Property(p => p.CreatedAt).IsRequired();
      entity.Property(p => p.PaidAt).IsRequired(false);

      // 配置唯一约束：一个订单只能有一条支付记录
      entity.HasIndex(p => p.OrderId).IsUnique();

      // 配置外键关系
      entity.HasOne(p => p.Order)
          .WithOne()
          .HasForeignKey<PaymentRecord>(p => p.OrderId)
          .OnDelete(DeleteBehavior.Restrict);

      entity.HasOne(p => p.User)
          .WithMany()
          .HasForeignKey(p => p.UserId)
          .OnDelete(DeleteBehavior.Restrict);

      // 添加索引
      entity.HasIndex(p => p.CreatedAt);
      entity.HasIndex(p => p.Status);
      entity.HasIndex(p => p.UserId);
    });

    // 配置评论实体
    builder.Entity<Review>(entity =>
    {
      entity.HasKey(r => r.Id);
      entity.Property(r => r.Rating)
          .IsRequired()
          .HasAnnotation("Range", new[] { 1, 5 });
      entity.Property(r => r.Content).HasMaxLength(500);
      entity.Property(r => r.CreatedAt).IsRequired();
      entity.Property(r => r.UpdatedAt)
          .IsRequired()
          .ValueGeneratedOnAddOrUpdate();

      // 配置与 Equipment 的关系
      entity.HasOne(r => r.Equipment)
          .WithMany()
          .HasForeignKey(r => r.EquipmentId)
          .OnDelete(DeleteBehavior.Restrict);

      // 配置与 Order 的关系
      entity.HasOne(r => r.Order)
          .WithMany()
          .HasForeignKey(r => r.OrderId)
          .OnDelete(DeleteBehavior.Restrict);

      // 配置与 User 的关系
      entity.HasOne(r => r.User)
          .WithMany()
          .HasForeignKey(r => r.UserId)
          .OnDelete(DeleteBehavior.Restrict);

      // 添加索引
      entity.HasIndex(r => r.EquipmentId);
      entity.HasIndex(r => r.OrderId);
      entity.HasIndex(r => r.UserId);
      entity.HasIndex(r => r.CreatedAt);
      entity.HasIndex(r => r.Rating);

      // 添加唯一约束：一个用户对同一个设备只能评论一次
      entity.HasIndex(r => new { r.EquipmentId, r.UserId }).IsUnique();
    });
  }

  public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
  {
    var result = await base.SaveChangesAsync(cancellationToken);

    // 确保空间索引存在
    try
    {
      await Database.ExecuteSqlRawAsync(@"
            CREATE SPATIAL INDEX IX_Equipment_Location 
            ON Equipment (ST_GeomFromText(CONCAT('POINT(', Longitude, ' ', Latitude, ')')))",
          cancellationToken);
    }
    catch (Exception)
    {
      // 如果索引已存在，忽略错误
    }

    return result;
  }
}
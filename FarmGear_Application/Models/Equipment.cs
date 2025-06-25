using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace FarmGear_Application.Models;

/// <summary>
/// 设备实体
/// </summary>
public class Equipment
{
  /// <summary>
  /// 设备ID
  /// </summary>
  [Key]
  public string Id { get; set; } = Guid.NewGuid().ToString();

  /// <summary>
  /// 设备名称
  /// </summary>
  [Required]
  [StringLength(100)]
  public string Name { get; set; } = string.Empty;

  /// <summary>
  /// 设备描述
  /// </summary>
  [StringLength(500)]
  public string Description { get; set; } = string.Empty;

  /// <summary>
  /// 日租金（元）
  /// </summary>
  [Required]
  [Column(TypeName = "decimal(18,2)")]
  public decimal DailyPrice { get; set; }

  /// <summary>
  /// 纬度
  /// </summary>
  [Required]
  [Column(TypeName = "decimal(10,6)")]
  public decimal Latitude { get; set; }

  /// <summary>
  /// 经度
  /// </summary>
  [Required]
  [Column(TypeName = "decimal(10,6)")]
  public decimal Longitude { get; set; }

  /// <summary>
  /// 设备状态
  /// </summary>
  [Required]
  public EquipmentStatus Status { get; set; }

  /// <summary>
  /// 所有者ID
  /// </summary>
  [Required]
  public string OwnerId { get; set; } = string.Empty;

  /// <summary>
  /// 所有者（导航属性）
  /// </summary>
  [ForeignKey(nameof(OwnerId))]
  public AppUser? Owner { get; set; }

  /// <summary>
  /// 创建时间
  /// </summary>
  [Required]
  public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

  /// <summary>
  /// 设备类型
  /// </summary>
  [Required]
  [StringLength(50)]
  public string Type { get; set; } = string.Empty;

  /// <summary>
  /// 空间位置（用于空间查询）
  /// </summary>
  [NotMapped]
  public string Location => $"POINT({Longitude} {Latitude})";

  /// <summary>
  /// 平均评分
  /// </summary>
  [Column(TypeName = "decimal(3,2)")]
  public decimal AverageRating { get; set; }
}
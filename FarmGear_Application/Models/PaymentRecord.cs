using FarmGear_Application.DTOs.Payment;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FarmGear_Application.Models;

/// <summary>
/// Payment Record
/// </summary>
public class PaymentRecord
{
  /// <summary>
  /// Payment Record ID
  /// </summary>
  [Key]
  public string Id { get; set; } = Guid.NewGuid().ToString();

  /// <summary>
  /// Order ID
  /// </summary>
  [Required(ErrorMessage = "Order ID cannot be empty")]
  public string OrderId { get; set; } = string.Empty;

  /// <summary>
  /// Payment User ID
  /// </summary>
  [Required(ErrorMessage = "Payment User ID cannot be empty")]
  public string UserId { get; set; } = string.Empty;

  /// <summary>
  /// Payment Amount
  /// </summary>
  [Column(TypeName = "decimal(18,2)")]
  public decimal Amount { get; set; }

  /// <summary>
  /// Payment Status
  /// </summary>
  public PaymentStatus Status { get; set; }

  /// <summary>
  /// Payment Time
  /// </summary>
  public DateTime? PaidAt { get; set; }

  /// <summary>
  /// Created At
  /// </summary>
  public DateTime CreatedAt { get; set; }

  /// <summary>
  /// Order (Foreign Key)
  /// </summary>
  [ForeignKey("OrderId")]
  public Order? Order { get; set; }

  /// <summary>
  /// User (Foreign Key)
  /// </summary>
  [ForeignKey("UserId")]
  public AppUser? User { get; set; }
}
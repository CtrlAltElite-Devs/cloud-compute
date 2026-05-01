using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using CloudCompute.Models.Enums;

namespace CloudCompute.Models;

public class CreditTransaction
{
    public int Id { get; set; }

    [Required]
    public string UserId { get; set; } = string.Empty;

    public CreditTransactionType Type { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal Amount { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal BalanceAfter { get; set; }

    public int? RelatedRentalId { get; set; }

    public string? AdminId { get; set; }

    [Required]
    [StringLength(500)]
    public string Reason { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ApplicationUser? User { get; set; }

    public ApplicationUser? Admin { get; set; }

    public Rental? RelatedRental { get; set; }
}

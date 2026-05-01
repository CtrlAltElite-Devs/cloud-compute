using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using CloudCompute.Models.Enums;

namespace CloudCompute.Models;

public class Rental
{
    public int Id { get; set; }

    [Required]
    [StringLength(30)]
    public string ReferenceNumber { get; set; } = string.Empty;

    [Required]
    public string RenterId { get; set; } = string.Empty;

    public int GpuId { get; set; }

    [Required]
    public string OwnerId { get; set; } = string.Empty;

    public DateTime StartTime { get; set; }

    public DateTime EndTime { get; set; }

    [Range(1, 168)]
    public int DurationHours { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal PricePerHour { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal TotalCost { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal PlatformFee { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal OwnerEarnings { get; set; }

    public RentalStatus Status { get; set; } = RentalStatus.Active;

    public bool TerminatedEarly { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ApplicationUser? Renter { get; set; }

    public ApplicationUser? Owner { get; set; }

    public Gpu? Gpu { get; set; }

    public Review? Review { get; set; }

    public ICollection<CreditTransaction> CreditTransactions { get; set; } = new List<CreditTransaction>();
}

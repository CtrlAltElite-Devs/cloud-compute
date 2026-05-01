using System.ComponentModel.DataAnnotations;

namespace CloudCompute.Models;

public class Review
{
    public int Id { get; set; }

    [Required]
    public string RenterId { get; set; } = string.Empty;

    public int GpuId { get; set; }

    public int RentalId { get; set; }

    [Range(1, 5)]
    public int Rating { get; set; }

    [StringLength(1000)]
    public string? Comment { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ApplicationUser? Renter { get; set; }

    public Gpu? Gpu { get; set; }

    public Rental? Rental { get; set; }
}

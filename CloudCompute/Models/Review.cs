using System.ComponentModel.DataAnnotations;

namespace CloudCompute.Models;

public class Review
{
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    public Guid RenterId { get; set; }

    public Guid GpuId { get; set; }

    public Guid RentalId { get; set; }

    [Range(1, 5)]
    public int Rating { get; set; }

    [StringLength(1000)]
    public string? Comment { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ApplicationUser? Renter { get; set; }

    public Gpu? Gpu { get; set; }

    public Rental? Rental { get; set; }
}

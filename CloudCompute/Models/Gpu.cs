using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using CloudCompute.Models.Enums;

namespace CloudCompute.Models;

public class Gpu
{
    public int Id { get; set; }

    [Required]
    public string OwnerId { get; set; } = string.Empty;

    [Required]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [StringLength(100)]
    public string Model { get; set; } = string.Empty;

    [Range(1, 512)]
    public int VramGb { get; set; }

    [Range(1, 200000)]
    public int CudaCores { get; set; }

    [Range(1, 100000)]
    [Column(TypeName = "decimal(18,2)")]
    public decimal PricePerHour { get; set; }

    [StringLength(4000)]
    public string? Description { get; set; }

    [StringLength(300)]
    public string? ImagePath { get; set; }

    public GpuStatus Status { get; set; } = GpuStatus.Pending;

    [Column(TypeName = "decimal(3,2)")]
    public decimal AverageRating { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ApplicationUser? Owner { get; set; }


    public ICollection<Rental> Rentals { get; set; } = new List<Rental>();

    public ICollection<Review> Reviews { get; set; } = new List<Review>();
}

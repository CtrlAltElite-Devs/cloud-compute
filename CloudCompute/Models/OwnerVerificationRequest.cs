using System.ComponentModel.DataAnnotations;
using CloudCompute.Models.Enums;

namespace CloudCompute.Models;

public class OwnerVerificationRequest
{
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    public Guid UserId { get; set; }

    public ApplicationUser? User { get; set; }

    [Required]
    [StringLength(2000)]
    public string Justification { get; set; } = string.Empty;

    public OwnerVerificationStatus Status { get; set; } = OwnerVerificationStatus.Pending;

    public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;

    public DateTime? ReviewedAt { get; set; }

    public Guid? ReviewedById { get; set; }

    [StringLength(2000)]
    public string? ReviewNotes { get; set; }
}

using System.ComponentModel.DataAnnotations;
using CloudCompute.Models.Enums;

namespace CloudCompute.Models;

public class Notification
{
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    public Guid UserId { get; set; }

    public NotificationType Type { get; set; }

    [Required]
    [StringLength(500)]
    public string Message { get; set; } = string.Empty;

    [StringLength(300)]
    public string? Link { get; set; }

    public bool IsRead { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ApplicationUser? User { get; set; }
}

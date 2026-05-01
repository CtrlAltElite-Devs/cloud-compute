using System.ComponentModel.DataAnnotations;
using CloudCompute.Models.Enums;

namespace CloudCompute.Models;

public class Notification
{
    public int Id { get; set; }

    [Required]
    public string UserId { get; set; } = string.Empty;

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

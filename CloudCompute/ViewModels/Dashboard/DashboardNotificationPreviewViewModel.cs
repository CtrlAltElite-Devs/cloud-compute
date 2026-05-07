using CloudCompute.Models.Enums;

namespace CloudCompute.ViewModels.Dashboard;

public class DashboardNotificationPreviewViewModel
{
    public Guid Id { get; init; }

    public NotificationType Type { get; init; }

    public string Message { get; init; } = string.Empty;

    public bool IsRead { get; init; }

    public DateTime CreatedAt { get; init; }

    public string RelativeTime
    {
        get
        {
            var delta = DateTime.UtcNow - CreatedAt;
            if (delta.TotalSeconds < 60) return "just now";
            if (delta.TotalMinutes < 60) return $"{(int)delta.TotalMinutes}m ago";
            if (delta.TotalHours < 24) return $"{(int)delta.TotalHours}h ago";
            if (delta.TotalDays < 30) return $"{(int)delta.TotalDays}d ago";
            return CreatedAt.ToString("MMM d");
        }
    }

    public string IconCssClass => Type switch
    {
        NotificationType.ListingApproved => "bi-check-circle text-success",
        NotificationType.ListingRejected => "bi-x-octagon text-danger",
        NotificationType.CreditGranted => "bi-coin text-warning",
        NotificationType.RentalConfirmed => "bi-lightning-charge text-info",
        NotificationType.RentalExpiring => "bi-clock-history text-warning",
        NotificationType.RentalCompleted => "bi-check2-circle text-success",
        NotificationType.RentalTerminated => "bi-x-circle text-danger",
        NotificationType.ReviewReceived => "bi-star text-warning",
        _ => "bi-bell text-secondary"
    };
}

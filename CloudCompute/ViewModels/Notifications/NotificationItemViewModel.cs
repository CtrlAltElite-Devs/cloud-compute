using CloudCompute.Extensions;
using CloudCompute.Models.Enums;

namespace CloudCompute.ViewModels.Notifications;

public class NotificationItemViewModel
{
    public Guid Id { get; init; }

    public NotificationType Type { get; init; }

    public string Message { get; init; } = string.Empty;

    public string? Link { get; init; }

    public bool IsRead { get; init; }

    public DateTime CreatedAt { get; init; }

    public bool HasLink => !string.IsNullOrWhiteSpace(Link);

    public string IconCssClass => Type switch
    {
        NotificationType.ListingApproved => "bi-check-circle text-success",
        NotificationType.ListingRejected => "bi-x-octagon text-danger",
        NotificationType.CreditGranted => "bi-coin text-warning",
        NotificationType.CreditRevoked => "bi-coin text-danger",
        NotificationType.RentalConfirmed => "bi-lightning-charge text-info",
        NotificationType.RentalExpiring => "bi-clock-history text-warning",
        NotificationType.RentalCompleted => "bi-check2-circle text-success",
        NotificationType.RentalTerminated => "bi-x-circle text-danger",
        NotificationType.ReviewReceived => "bi-star text-warning",
        NotificationType.VerificationApproved => "bi-patch-check text-success",
        NotificationType.VerificationRejected => "bi-patch-exclamation text-danger",
        NotificationType.Welcome => "bi-stars text-info",
        _ => "bi-bell text-secondary"
    };

    public string BadgeLabel => Type switch
    {
        NotificationType.ListingApproved => "LISTING APPROVED",
        NotificationType.ListingRejected => "LISTING REJECTED",
        NotificationType.CreditGranted => "CREDITS",
        NotificationType.CreditRevoked => "CREDITS REVOKED",
        NotificationType.RentalConfirmed => "RENTAL CONFIRMED",
        NotificationType.RentalExpiring => "RENTAL EXPIRING",
        NotificationType.RentalCompleted => "RENTAL COMPLETED",
        NotificationType.RentalTerminated => "RENTAL TERMINATED",
        NotificationType.ReviewReceived => "REVIEW",
        NotificationType.VerificationApproved => "VERIFIED",
        NotificationType.VerificationRejected => "VERIFICATION REJECTED",
        NotificationType.Welcome => "WELCOME",
        _ => "NOTIFICATION"
    };

    public string BadgeCssClass => Type switch
    {
        NotificationType.ListingApproved => "border border-success text-success bg-transparent",
        NotificationType.ListingRejected => "border border-danger text-danger bg-transparent",
        NotificationType.CreditGranted => "border border-warning text-warning bg-transparent",
        NotificationType.CreditRevoked => "border border-danger text-danger bg-transparent",
        NotificationType.RentalConfirmed => "border border-info text-info bg-transparent",
        NotificationType.RentalExpiring => "border border-warning text-warning bg-transparent",
        NotificationType.RentalCompleted => "border border-success text-success bg-transparent",
        NotificationType.RentalTerminated => "border border-danger text-danger bg-transparent",
        NotificationType.ReviewReceived => "border border-info text-info bg-transparent",
        NotificationType.VerificationApproved => "border border-success text-success bg-transparent",
        NotificationType.VerificationRejected => "border border-danger text-danger bg-transparent",
        NotificationType.Welcome => "border border-info text-info bg-transparent",
        _ => "border border-secondary text-secondary bg-transparent"
    };

    public string RowCssClass => IsRead
        ? "profile-card"
        : "profile-card border-start border-2 border-info";

    public string RelativeTime
    {
        get
        {
            var delta = DateTime.UtcNow - CreatedAt;
            if (delta.TotalSeconds < 60)
            {
                return "just now";
            }
            if (delta.TotalMinutes < 60)
            {
                var minutes = (int)delta.TotalMinutes;
                return $"{minutes} minute{(minutes == 1 ? "" : "s")} ago";
            }
            if (delta.TotalHours < 24)
            {
                var hours = (int)delta.TotalHours;
                return $"{hours} hour{(hours == 1 ? "" : "s")} ago";
            }
            if (delta.TotalDays < 30)
            {
                var days = (int)delta.TotalDays;
                return $"{days} day{(days == 1 ? "" : "s")} ago";
            }
            return CreatedAt.ToFriendlyDate();
        }
    }
}

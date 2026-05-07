namespace CloudCompute.Constants;

public static class NotificationConstants
{
    public static class Status
    {
        public const string TempDataMessageKey = "NotificationStatusMessage";
        public const string TempDataTypeKey = "NotificationStatusType";
        public const string Success = "success";
        public const string Danger = "danger";
    }

    public static class Routes
    {
        public const string MyListingsPath = "/gpus/mine";
        public const string DashboardPath = "/dashboard";
        public const string ActiveRentalsPath = "/rentals/active";
    }

    public static class Messages
    {
        public const string ListingApprovedFormat = "Your listing '{0}' has been approved and is now live.";
        public const string ListingRejectedFormat = "Your listing '{0}' was rejected.";
        public const string ListingRejectedWithReasonFormat = "Your listing '{0}' was rejected: {1}";
        public const string CreditGrantedFormat = "You received {0:N0} credits. {1}";
        public const string CreditRevokedFormat = "{0:N0} credits were removed from your account. {1}";
        public const string RentalExpiringFormat = "Your rental {0} expires in less than 1 hour.";

        public const string MarkedAsRead = "Notification marked as read.";
        public const string AllMarkedAsRead = "All notifications marked as read.";
        public const string NotificationNotFound = "Notification could not be found.";
        public const string SaveFailed = "We couldn't update the notification. Please try again.";
    }

    public static class ExpiryWatcher
    {
        public const int PollIntervalSeconds = 120;
        public const int WarningWindowMinutes = 60;
    }

    public static class Pagination
    {
        public const int MaxItems = 100;
    }

    public static class Limits
    {
        public const int MessageMaxLength = 500;
        public const int LinkMaxLength = 300;
    }
}

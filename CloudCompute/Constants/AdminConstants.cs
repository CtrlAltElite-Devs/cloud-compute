namespace CloudCompute.Constants;

public static class AdminConstants
{
    public static class Routes
    {
        public const string DashboardPath = "/admin/dashboard";
        public const string UsersPath = "/admin/users";
        public const string CreditsPath = "/admin/credits";
        public const string ListingsPath = "/admin/listings";
        public const string AnalyticsPath = "/admin/analytics";
        public const string VerificationsPath = "/admin/verifications";
    }

    public static class Status
    {
        public const string TempDataMessageKey = "AdminStatusMessage";
        public const string TempDataTypeKey = "AdminStatusType";
        public const string Success = "success";
        public const string Danger = "danger";
    }

    public static class Messages
    {
        public const string UserNotFound = "User could not be found.";
        public const string CannotSuspendSelf = "You can't suspend your own account.";
        public const string CannotSuspendAdmin = "Other admin accounts can't be suspended from this screen.";
        public const string CannotVerifyAdmin = "Admin accounts don't need owner verification.";
        public const string UserSuspended = "User suspended.";
        public const string UserReactivated = "User reactivated.";
        public const string OwnerVerificationGranted = "Owner verification granted.";
        public const string OwnerVerificationRevoked = "Owner verification revoked.";

        public const string CreditsGranted = "Credits granted.";
        public const string CreditsRevoked = "Credits revoked.";
        public const string CreditsBulkGranted = "Bulk credit grant complete.";
        public const string AmountOutOfRange = "Amount must be between 1 and 100,000.";
        public const string ReasonTooShort = "Please provide a reason of at least 5 characters.";
        public const string InsufficientBalance = "User doesn't have enough credits to revoke this amount.";
        public const string CannotGrantToAdmin = "Credit grants target members, not admin accounts.";
        public const string SaveFailed = "We couldn't save those changes. Please try again.";

        public const string ListingNotFound = "Listing could not be found.";
        public const string ListingNotPending = "This listing has already been reviewed.";
        public const string ListingApproved = "Listing approved.";
        public const string ListingRejected = "Listing rejected.";
    }

    public static class Validation
    {
        public const decimal MinGrantAmount = 1m;
        public const decimal MaxGrantAmount = 100_000m;
        public const int MinReasonLength = 5;
        public const int MaxReasonLength = 500;
    }

    public static class Pagination
    {
        public const int DefaultPageSize = 20;
        public const int MaxPageSize = 100;
    }
}

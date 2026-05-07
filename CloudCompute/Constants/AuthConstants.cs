namespace CloudCompute.Constants;

public static class AuthConstants
{
    public static class Cookie
    {
        public const string Name = "CloudCompute.Auth";
        public const int ExpirationHours = 8;
        public const int PersistentExpirationDays = 14;
    }

    public static class Routes
    {
        public const string MemberLoginPath = "/auth/login";
        public const string AdminLoginPath = "/admin/login";
        public const string LogoutPath = "/auth/logout";
        public const string AccessDeniedRoute = "auth/access-denied";
        public const string AccessDeniedPath = "/" + AccessDeniedRoute;
        public const string AdminPathPrefix = "/admin";
    }

    public static class Messages
    {
        public const string InvalidCredentials = "Invalid email/username or password.";
        public const string InvalidAdminCredentials = "Invalid admin email/username or password.";
        public const string InactiveAccount = "This account is inactive. Please contact support.";
        public const string SuspendedAccountAction = "Your account is suspended. Please contact support.";
        public const string DuplicateEmail = "An account with this email already exists.";
        public const string DuplicateUserName = "Username is already taken.";
        public const string DuplicateAccount = "An account with the same username or email already exists.";
        public const string InitialCreditReason = "Initial signup credit";
    }

    public static class Validation
    {
        public const string ModelErrorKey = "";
    }

    public static class Diagnostics
    {
        public const string MissingHttpContext = "No active HTTP context is available.";
        public const string SeedAdminIdentityConflict = "Seed admin email and username are assigned to different accounts.";
    }

    public static class Claims
    {
        public const string FullName = "full_name";
        public const string ProfilePicturePath = "profile_picture_path";
    }

    public static class Profile
    {
        public const string ProfileUpdated = "Profile updated.";
        public const string PasswordUpdated = "Password updated.";
        public const string AvatarUpdated = "Profile picture updated.";
        public const string AvatarRemoved = "Profile picture removed.";
        public const string AccountDeleted = "Your account has been deleted.";
        public const string AccountDeleteHasActiveRentals = "Please terminate your active rentals before deleting your account.";
        public const string AccountDeletePasswordRequired = "Enter your current password to confirm account deletion.";
        public const string IncorrectCurrentPassword = "Current password is incorrect.";
        public const string DuplicateEmail = "Another account already uses this email.";
        public const string DuplicateUserName = "Another account already uses this username.";
        public const string AvatarRequired = "Please choose an image to upload.";
        public const string AvatarUnsupportedFormat = "Unsupported image format. Use JPG, PNG, or WebP.";
        public const string AvatarTooLarge = "Image must be 2 MB or smaller.";
        public const string UserNotFound = "Account could not be found.";
        public const long AvatarMaxBytes = 2 * 1024 * 1024;
    }

    public static class Redirects
    {
        public const string DashboardController = "Dashboard";
        public const string DashboardIndexAction = "Index";
        public const string AdminDashboardAction = "Dashboard";
        public const string LoginAction = "Login";
    }
}

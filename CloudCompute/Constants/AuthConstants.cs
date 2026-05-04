namespace CloudCompute.Constants;

public static class AuthConstants
{
    public static class Cookie
    {
        public const string Name = ".CloudCompute.Auth";
        public const int ExpirationHours = 8;
    }

    public static class Routes
    {
        public const string LoginPath = "/auth/login";
        public const string LogoutPath = "/auth/logout";
        public const string AccessDeniedRoute = "auth/access-denied";
        public const string AccessDeniedPath = "/" + AccessDeniedRoute;
    }

    public static class Messages
    {
        public const string InvalidLogin = "Invalid username/email or password.";
        public const string SuspendedAccount = "This account has been suspended.";
        public const string DuplicateUserName = "Username is already taken.";
        public const string DuplicateEmail = "Email is already registered.";
        public const string DuplicateAccount = "An account with the same username or email already exists.";
    }

    public static class Validation
    {
        public const string ModelErrorKey = "";
    }

    public static class Diagnostics
    {
        public const string MissingHttpContext = "AuthService requires an active HTTP context.";
    }

    public static class Claims
    {
        public const string FullName = "FullName";
    }

    public static class Redirects
    {
        public const string LandingController = "Landing";
        public const string LandingIndexAction = "Index";
    }
}

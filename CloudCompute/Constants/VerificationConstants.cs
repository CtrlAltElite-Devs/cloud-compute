namespace CloudCompute.Constants;

public static class VerificationConstants
{
    public static class Messages
    {
        public const string UserNotFound = "Account could not be found.";
        public const string AlreadyVerified = "Your account is already verified.";
        public const string PendingRequestExists = "You already have a pending verification request.";
        public const string RequestSubmitted = "Verification request submitted. We'll review it shortly.";
        public const string RequestNotFound = "Verification request could not be found.";
        public const string RequestNotPending = "This request has already been reviewed.";
        public const string RequestApproved = "Verification request approved.";
        public const string RequestRejected = "Verification request rejected.";
        public const string IdentityImageRequired = "Please upload a photo of your government-issued ID.";
        public const string IdentityImageTooLarge = "ID image must be 4 MB or smaller.";
        public const string IdentityImageUnsupportedFormat = "Unsupported image format. Use JPG, PNG, or WebP.";
        public const string SaveFailed = "We couldn't submit your request. Please try again.";
    }

    public static class Status
    {
        public const string TempDataMessageKey = "VerificationStatusMessage";
        public const string TempDataTypeKey = "VerificationStatusType";
    }

    public static class IdentityImage
    {
        public const long MaxBytes = 4 * 1024 * 1024;
        public const string DirectorySegment = "uploads/verification";
        public static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
        {
            ".jpg",
            ".jpeg",
            ".png",
            ".webp"
        };
    }
}

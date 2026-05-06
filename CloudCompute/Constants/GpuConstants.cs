namespace CloudCompute.Constants;

public static class GpuConstants
{
    public static class Photo
    {
        public const long MaxBytes = 5 * 1024 * 1024;
        public const string DirectorySegment = "uploads/gpus";
        public static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
        {
            ".jpg",
            ".jpeg",
            ".png",
            ".webp"
        };
    }

    public static class Messages
    {
        public const string UserNotFound = "Account could not be found.";
        public const string NotOwnerVerified = "You need to be a verified owner to list a GPU.";
        public const string PhotoUnsupportedFormat = "Unsupported image format. Use JPG, PNG, or WebP.";
        public const string PhotoTooLarge = "Photo must be 5 MB or smaller.";
        public const string GpuCreated = "Listing submitted for approval.";
        public const string SaveFailed = "We couldn't save your listing. Please try again.";
    }

    public static class Status
    {
        public const string TempDataMessageKey = "GpuStatusMessage";
        public const string TempDataTypeKey = "GpuStatusType";
    }
}

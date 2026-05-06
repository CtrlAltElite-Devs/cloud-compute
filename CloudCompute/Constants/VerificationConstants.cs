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
    }

    public static class Status
    {
        public const string TempDataMessageKey = "VerificationStatusMessage";
        public const string TempDataTypeKey = "VerificationStatusType";
    }
}

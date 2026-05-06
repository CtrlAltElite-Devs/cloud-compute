using CloudCompute.Models.Enums;

namespace CloudCompute.ViewModels.Verification;

public class VerificationPageViewModel
{
    public OwnerVerificationStatus? LatestRequestStatus { get; set; }

    public DateTime? LatestSubmittedAt { get; set; }

    public string? LatestJustification { get; set; }

    public string? LatestReviewNotes { get; set; }

    public RequestVerificationViewModel Form { get; set; } = new();
}

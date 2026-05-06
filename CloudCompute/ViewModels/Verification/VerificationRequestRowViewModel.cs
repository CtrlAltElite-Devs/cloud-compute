using CloudCompute.Models.Enums;

namespace CloudCompute.ViewModels.Verification;

public class VerificationRequestRowViewModel
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    public string UserFullName { get; set; } = string.Empty;

    public string UserEmail { get; set; } = string.Empty;

    public string Justification { get; set; } = string.Empty;

    public DateTime SubmittedAt { get; set; }

    public OwnerVerificationStatus Status { get; set; }
}

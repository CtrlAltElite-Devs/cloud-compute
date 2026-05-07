using CloudCompute.Models.Enums;

namespace CloudCompute.ViewModels.Admin;

public class AdminAnalyticsViewModel
{
    public int TotalUsers { get; set; }
    public int ActiveUsers { get; set; }
    public int SuspendedUsers { get; set; }
    public int AdminCount { get; set; }
    public int VerifiedOwners { get; set; }

    public decimal TotalCreditsInCirculation { get; set; }
    public decimal TotalCreditsGranted { get; set; }
    public decimal TotalCreditsRevoked { get; set; }
    public decimal TotalRentalEarnings { get; set; }

    public IReadOnlyDictionary<GpuStatus, int> ListingCountsByStatus { get; set; } = new Dictionary<GpuStatus, int>();

    public IReadOnlyList<TopEarnerRow> TopEarners { get; set; } = Array.Empty<TopEarnerRow>();
}

public class TopEarnerRow
{
    public Guid OwnerId { get; set; }
    public string OwnerDisplay { get; set; } = string.Empty;
    public decimal TotalEarned { get; set; }
}

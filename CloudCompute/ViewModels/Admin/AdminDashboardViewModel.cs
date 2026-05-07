namespace CloudCompute.ViewModels.Admin;

public class AdminDashboardViewModel
{
    public int PendingVerifications { get; set; }
    public int PendingListings { get; set; }
    public int ActiveUsers { get; set; }
    public int SuspendedUsers { get; set; }
    public decimal TotalCreditsInCirculation { get; set; }

    public IReadOnlyList<AdminDashboardCreditEvent> RecentCreditEvents { get; set; } = Array.Empty<AdminDashboardCreditEvent>();
}

public class AdminDashboardCreditEvent
{
    public DateTime CreatedAt { get; set; }
    public string UserDisplay { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string? AdminDisplay { get; set; }
}

namespace CloudCompute.ViewModels.Dashboard;

public class DashboardViewModel
{
    public string DisplayName { get; init; } = string.Empty;

    public decimal CreditBalance { get; init; }

    public int ActiveRentalCount { get; init; }

    public decimal CurrentMonthSpend { get; init; }

    public int LifetimeComputeHours { get; init; }

    public IReadOnlyCollection<DashboardRentalPreviewViewModel> ActiveRentals { get; init; } = Array.Empty<DashboardRentalPreviewViewModel>();

    public IReadOnlyCollection<DashboardNotificationPreviewViewModel> Notifications { get; init; } = Array.Empty<DashboardNotificationPreviewViewModel>();
}

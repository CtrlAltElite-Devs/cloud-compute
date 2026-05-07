namespace CloudCompute.ViewModels.Dashboard;

public class DashboardRentalPreviewViewModel
{
    public Guid Id { get; init; }

    public string GpuName { get; init; } = string.Empty;

    public string GpuModel { get; init; } = string.Empty;

    public string OwnerName { get; init; } = string.Empty;

    public string? ImagePath { get; init; }

    public DateTime EndTime { get; init; }

    public int DurationHours { get; init; }

    public decimal TotalCost { get; init; }

    public TimeSpan TimeRemaining => EndTime <= DateTime.UtcNow ? TimeSpan.Zero : EndTime - DateTime.UtcNow;

    public string TimeRemainingDisplay
    {
        get
        {
            var remaining = TimeRemaining;
            if (remaining == TimeSpan.Zero)
            {
                return "Ending soon";
            }

            if (remaining.TotalHours >= 1)
            {
                return $"{(int)remaining.TotalHours}h {remaining.Minutes}m";
            }

            return $"{Math.Max(1, remaining.Minutes)}m";
        }
    }
}

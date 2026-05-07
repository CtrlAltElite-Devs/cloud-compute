namespace CloudCompute.ViewModels.Owner;

public class OwnerEarningsViewModel
{
    public bool IsOwnerVerified { get; init; }

    public decimal TotalEarnedAllTime { get; init; }

    public decimal PendingPayouts { get; init; }

    public int CompletedRentalCount { get; init; }

    public int ActiveRentalCount { get; init; }

    public IReadOnlyList<OwnerEarningsByGpuViewModel> PerGpu { get; init; } =
        Array.Empty<OwnerEarningsByGpuViewModel>();

    public IReadOnlyList<OwnerEarningsChartPointViewModel> Last7Days { get; init; } =
        Array.Empty<OwnerEarningsChartPointViewModel>();

    public IReadOnlyList<OwnerEarningsChartPointViewModel> Last30Days { get; init; } =
        Array.Empty<OwnerEarningsChartPointViewModel>();
}

public class OwnerEarningsByGpuViewModel
{
    public Guid GpuId { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Model { get; init; } = string.Empty;
    public string? ImagePath { get; init; }
    public decimal TotalEarnings { get; init; }
    public int RentalCount { get; init; }
}

public class OwnerEarningsChartPointViewModel
{
    public DateOnly Date { get; init; }
    public decimal Amount { get; init; }
}

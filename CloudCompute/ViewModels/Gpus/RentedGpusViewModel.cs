namespace CloudCompute.ViewModels.Gpus;

public class RentedGpusViewModel
{
    public IReadOnlyList<RentedGpuItemViewModel> Items { get; init; } = Array.Empty<RentedGpuItemViewModel>();
}

public class RentedGpuItemViewModel
{
    public Guid RentalId { get; init; }

    public string ReferenceNumber { get; init; } = string.Empty;

    public string GpuName { get; init; } = string.Empty;

    public string GpuModel { get; init; } = string.Empty;

    public string? ImagePath { get; init; }

    public string RenterDisplayName { get; init; } = string.Empty;

    public DateTime StartTime { get; init; }

    public DateTime EndTime { get; init; }

    public int DurationHours { get; init; }

    public decimal OwnerEarnings { get; init; }

    public decimal TotalCost { get; init; }
}

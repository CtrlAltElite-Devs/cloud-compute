using CloudCompute.Models.Enums;

namespace CloudCompute.ViewModels.Gpus;

public class MyListingItemViewModel
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Model { get; init; } = string.Empty;
    public int VramGb { get; init; }
    public int CudaCores { get; init; }
    public decimal PricePerHour { get; init; }
    public string? ImagePath { get; init; }
    public GpuStatus Status { get; init; }
    public string? RejectionReason { get; init; }
    public int RentalCount { get; init; }
    public decimal AverageRating { get; init; }
    public bool HasReviews { get; init; }

    public string BadgeLabel => Status switch
    {
        GpuStatus.Pending => "PENDING REVIEW",
        GpuStatus.Available => "LIVE",
        GpuStatus.Rented => "RENTED",
        GpuStatus.Maintenance => "MAINTENANCE",
        GpuStatus.Unavailable => "UNAVAILABLE",
        GpuStatus.Rejected => "REJECTED",
        _ => Status.ToString().ToUpperInvariant()
    };

    public string BadgeCssClass => Status switch
    {
        GpuStatus.Pending => "bg-warning text-dark",
        GpuStatus.Available => "bg-success",
        GpuStatus.Rented => "bg-info text-dark",
        GpuStatus.Maintenance => "bg-secondary",
        GpuStatus.Unavailable => "bg-secondary",
        GpuStatus.Rejected => "bg-danger",
        _ => "bg-secondary"
    };

    public bool IsStatusSelectorEnabled =>
        Status is GpuStatus.Available or GpuStatus.Unavailable or GpuStatus.Maintenance;

    public bool IsEditEnabled => Status != GpuStatus.Rented;

    public bool IsDeleteEnabled => Status != GpuStatus.Rented && RentalCount == 0 && !HasReviews;
}

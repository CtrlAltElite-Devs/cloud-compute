using System.ComponentModel.DataAnnotations;
using CloudCompute.Models.Enums;

namespace CloudCompute.ViewModels.Rentals;

public class RentalConfirmViewModel
{
    public Guid GpuId { get; set; }

    public string GpuName { get; set; } = string.Empty;

    public string GpuModel { get; set; } = string.Empty;

    public string? ImagePath { get; set; }

    public string OwnerDisplayName { get; set; } = string.Empty;

    public decimal PricePerHour { get; set; }

    public int MinRentalHours { get; set; }

    public decimal CurrentBalance { get; set; }

    [Range(1, 168, ErrorMessage = "Duration must be between 1 and 168 hours.")]
    public int DurationHours { get; set; }

    public DateTime StartTime { get; set; }

    public DateTime EndTime => StartTime.AddHours(DurationHours);

    public decimal TotalCost => PricePerHour * DurationHours;

    public decimal BalanceAfter => CurrentBalance - TotalCost;
}

public class ActiveRentalsViewModel
{
    public IReadOnlyList<ActiveRentalItemViewModel> Items { get; set; } = Array.Empty<ActiveRentalItemViewModel>();
}

public class ActiveRentalItemViewModel
{
    public Guid Id { get; set; }

    public string ReferenceNumber { get; set; } = string.Empty;

    public string GpuName { get; set; } = string.Empty;

    public string GpuModel { get; set; } = string.Empty;

    public string? ImagePath { get; set; }

    public string OwnerDisplayName { get; set; } = string.Empty;

    public DateTime StartTime { get; set; }

    public DateTime EndTime { get; set; }

    public int DurationHours { get; set; }

    public decimal TotalCost { get; set; }
}

public class RentalHistoryViewModel
{
    public RentalHistoryFilterViewModel Filter { get; set; } = new();

    public IReadOnlyList<RentalHistoryItemViewModel> Items { get; set; } = Array.Empty<RentalHistoryItemViewModel>();

    public bool HasAnyRentals { get; set; }
}

public class RentalHistoryItemViewModel
{
    public Guid Id { get; set; }

    public string ReferenceNumber { get; set; } = string.Empty;

    public string GpuName { get; set; } = string.Empty;

    public string GpuModel { get; set; } = string.Empty;

    public string? ImagePath { get; set; }

    public string OwnerDisplayName { get; set; } = string.Empty;

    public RentalStatus Status { get; set; }

    public DateTime StartTime { get; set; }

    public DateTime EndTime { get; set; }

    public int DurationHours { get; set; }

    public decimal PricePerHour { get; set; }

    public decimal TotalCost { get; set; }

    public bool HasReview { get; set; }
}

public class RentalHistoryFilterViewModel
{
    public string? Search { get; set; }

    public RentalStatus? Status { get; set; }

    public bool HasActiveFilters =>
        !string.IsNullOrWhiteSpace(Search) ||
        Status.HasValue;
}

public class RentalReceiptViewModel
{
    public Guid Id { get; set; }

    public string ReferenceNumber { get; set; } = string.Empty;

    public string GpuName { get; set; } = string.Empty;

    public string GpuModel { get; set; } = string.Empty;

    public string OwnerDisplayName { get; set; } = string.Empty;

    public RentalStatus Status { get; set; }

    public DateTime StartTime { get; set; }

    public DateTime EndTime { get; set; }

    public int DurationHours { get; set; }

    public decimal PricePerHour { get; set; }

    public decimal TotalCost { get; set; }

    public decimal PlatformFee { get; set; }

    public decimal OwnerEarnings { get; set; }
}

public class RentalReviewFormViewModel
{
    public Guid RentalId { get; set; }

    public Guid GpuId { get; set; }

    public string GpuName { get; set; } = string.Empty;

    public string GpuModel { get; set; } = string.Empty;

    public string? ImagePath { get; set; }

    public string OwnerDisplayName { get; set; } = string.Empty;

    public string ReferenceNumber { get; set; } = string.Empty;

    [Range(1, 5, ErrorMessage = "Rating must be between 1 and 5 stars.")]
    public int Rating { get; set; } = 5;

    [StringLength(1000, ErrorMessage = "Comment must be 1000 characters or fewer.")]
    public string? Comment { get; set; }
}

using System.ComponentModel.DataAnnotations;

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

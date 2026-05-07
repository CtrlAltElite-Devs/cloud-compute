using CloudCompute.Models.Enums;

namespace CloudCompute.ViewModels.Admin;

public class AdminListingFilter
{
    public GpuStatus? Status { get; set; } = GpuStatus.Pending;
    public bool All { get; set; }
    public string? OwnerQuery { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

public class AdminListingListViewModel
{
    public AdminListingFilter Filter { get; set; } = new();
    public IReadOnlyList<AdminListingRow> Rows { get; set; } = Array.Empty<AdminListingRow>();
    public int TotalCount { get; set; }
    public int TotalPages => Math.Max(1, (int)Math.Ceiling((double)TotalCount / Math.Max(1, Filter.PageSize)));
    public IReadOnlyDictionary<GpuStatus, int> CountsByStatus { get; set; } = new Dictionary<GpuStatus, int>();
}

public class AdminListingRow
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public GpuStatus Status { get; set; }
    public decimal PricePerHour { get; set; }
    public string? ImagePath { get; set; }
    public DateTime CreatedAt { get; set; }
    public Guid OwnerId { get; set; }
    public string OwnerDisplay { get; set; } = string.Empty;
}

public class AdminListingDetailViewModel
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public int VramGb { get; set; }
    public int CudaCores { get; set; }
    public decimal PricePerHour { get; set; }
    public int MinRentalHours { get; set; }
    public string? Description { get; set; }
    public string? ImagePath { get; set; }
    public GpuStatus Status { get; set; }
    public string? RejectionReason { get; set; }
    public DateTime CreatedAt { get; set; }
    public Guid OwnerId { get; set; }
    public string OwnerDisplay { get; set; } = string.Empty;
    public string OwnerEmail { get; set; } = string.Empty;
    public bool OwnerIsVerified { get; set; }
}

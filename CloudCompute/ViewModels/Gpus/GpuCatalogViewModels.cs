using CloudCompute.Models.Enums;

namespace CloudCompute.ViewModels.Gpus;

public enum GpuCatalogSort
{
    Newest = 0,
    PriceAsc = 1,
    PriceDesc = 2,
    RatingDesc = 3
}

public class GpuCatalogFilter
{
    public const int DefaultPageSize = 12;
    public const int MaxPageSize = 48;

    public string? Search { get; set; }

    public GpuCatalogSort Sort { get; set; } = GpuCatalogSort.Newest;

    public decimal? MinPrice { get; set; }

    public decimal? MaxPrice { get; set; }

    public int? MinVramGb { get; set; }

    public bool AvailableOnly { get; set; } = true;

    public int Page { get; set; } = 1;

    public int PageSize { get; set; } = DefaultPageSize;

    public bool HasActiveFilters =>
        !string.IsNullOrWhiteSpace(Search) ||
        Sort != GpuCatalogSort.Newest ||
        MinPrice.HasValue ||
        MaxPrice.HasValue ||
        MinVramGb.HasValue ||
        !AvailableOnly;
}

public class GpuCatalogViewModel
{
    public GpuCatalogFilter Filter { get; set; } = new();

    public IReadOnlyList<GpuCatalogItemViewModel> Items { get; set; } = Array.Empty<GpuCatalogItemViewModel>();

    public int TotalCount { get; set; }

    public int TotalPages { get; set; }

    public int Page => Filter.Page;

    public int PageSize => Filter.PageSize;

    public bool HasPreviousPage => Filter.Page > 1;

    public bool HasNextPage => Filter.Page < TotalPages;

    public string? Search
    {
        get => Filter.Search;
        set => Filter.Search = value;
    }
}

public class GpuCatalogItemViewModel
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Model { get; set; } = string.Empty;

    public int VramGb { get; set; }

    public int CudaCores { get; set; }

    public decimal PricePerHour { get; set; }

    public int MinRentalHours { get; set; }

    public string? ImagePath { get; set; }

    public GpuStatus Status { get; set; }

    public bool IsOwnedByCurrentUser { get; set; }

    public string OwnerDisplayName { get; set; } = string.Empty;

    public string OwnerUserName { get; set; } = string.Empty;

    public decimal AverageRating { get; set; }

    public int ReviewCount { get; set; }
}

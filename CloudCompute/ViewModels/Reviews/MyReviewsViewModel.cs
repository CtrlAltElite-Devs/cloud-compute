namespace CloudCompute.ViewModels.Reviews;

public class MyReviewsViewModel
{
    public IReadOnlyList<MyReviewItemViewModel> Items { get; set; } = Array.Empty<MyReviewItemViewModel>();
}

public class MyReviewItemViewModel
{
    public Guid Id { get; set; }

    public Guid RentalId { get; set; }

    public Guid GpuId { get; set; }

    public string GpuName { get; set; } = string.Empty;

    public string GpuModel { get; set; } = string.Empty;

    public string? ImagePath { get; set; }

    public string OwnerDisplayName { get; set; } = string.Empty;

    public string ReferenceNumber { get; set; } = string.Empty;

    public int Rating { get; set; }

    public string? Comment { get; set; }

    public DateTime CreatedAt { get; set; }
}

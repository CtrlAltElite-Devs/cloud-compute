namespace CloudCompute.ViewModels.Gpus;

public class GpuReviewItemViewModel
{
    public string RenterDisplayName { get; set; } = string.Empty;

    public int Rating { get; set; }

    public string? Comment { get; set; }

    public DateTime CreatedAt { get; set; }
}

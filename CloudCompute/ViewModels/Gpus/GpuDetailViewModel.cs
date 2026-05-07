using CloudCompute.Models.Enums;

namespace CloudCompute.ViewModels.Gpus;

public class GpuDetailViewModel
{
    public Guid Id { get; set; }

    public Guid OwnerId { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Model { get; set; } = string.Empty;

    public int VramGb { get; set; }

    public int CudaCores { get; set; }

    public decimal PricePerHour { get; set; }

    public int MinRentalHours { get; set; }

    public string? Description { get; set; }

    public string? ImagePath { get; set; }

    public GpuStatus Status { get; set; }

    public string OwnerDisplayName { get; set; } = string.Empty;

    public string OwnerUserName { get; set; } = string.Empty;

    public string? OwnerProfilePicturePath { get; set; }

    public decimal AverageRating { get; set; }

    public int ReviewCount { get; set; }

    public bool CanRent { get; set; }
}

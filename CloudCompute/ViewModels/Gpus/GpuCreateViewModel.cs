using System.ComponentModel.DataAnnotations;

namespace CloudCompute.ViewModels.Gpus;

public class GpuCreateViewModel
{
    [Required(ErrorMessage = "Brand is required.")]
    [StringLength(100, ErrorMessage = "Brand must be 100 characters or fewer.")]
    [Display(Name = "Brand")]
    public string Brand { get; set; } = string.Empty;

    [Required(ErrorMessage = "Model is required.")]
    [StringLength(100, ErrorMessage = "Model must be 100 characters or fewer.")]
    [Display(Name = "Model")]
    public string Model { get; set; } = string.Empty;

    [Range(1, 512, ErrorMessage = "VRAM must be between 1 and 512 GB.")]
    [Display(Name = "VRAM (GB)")]
    public int VramGb { get; set; }

    [Range(1, 200000, ErrorMessage = "CUDA / Stream cores must be between 1 and 200,000.")]
    [Display(Name = "CUDA / Stream Cores")]
    public int CudaCores { get; set; }

    [Range(typeof(decimal), "1", "100000", ErrorMessage = "Price per hour must be between $1 and $100,000.")]
    [Display(Name = "Price per hour ($)")]
    public decimal PricePerHour { get; set; }

    [Range(1, 168, ErrorMessage = "Min rental duration must be between 1 and 168 hours.")]
    [Display(Name = "Min rental duration (h)")]
    public int MinRentalHours { get; set; } = 1;

    [StringLength(4000, ErrorMessage = "Description must be 4000 characters or fewer.")]
    [Display(Name = "Description")]
    public string? Description { get; set; }

    [Display(Name = "Photo")]
    public IFormFile? Photo { get; set; }
}

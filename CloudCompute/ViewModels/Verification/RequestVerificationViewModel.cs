using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace CloudCompute.ViewModels.Verification;

public class RequestVerificationViewModel
{
    [Required(ErrorMessage = "Please tell us about your setup so admins can review your request.")]
    [StringLength(2000, MinimumLength = 30, ErrorMessage = "Please provide between 30 and 2000 characters.")]
    [Display(Name = "Tell us about your setup")]
    public string Justification { get; set; } = string.Empty;

    [Display(Name = "Government-issued ID photo")]
    public IFormFile? IdentityImage { get; set; }
}

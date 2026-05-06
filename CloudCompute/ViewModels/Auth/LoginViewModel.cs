using System.ComponentModel.DataAnnotations;

namespace CloudCompute.ViewModels.Auth;

public class LoginViewModel
{
    [Required(ErrorMessage = "Email or username is required.")]
    [Display(Name = "Email or username")]
    [StringLength(256, ErrorMessage = "Email or username cannot exceed 256 characters.")]
    public string LoginIdentifier { get; set; } = string.Empty;

    [Required(ErrorMessage = "Password is required.")]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    public bool RememberMe { get; set; }
}

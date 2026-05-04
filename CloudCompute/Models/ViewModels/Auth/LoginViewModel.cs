using System.ComponentModel.DataAnnotations;

namespace CloudCompute.Models.ViewModels.Auth;

public class LoginViewModel
{
    [Required]
    [Display(Name = "Username or email")]
    [StringLength(256)]
    public string LoginIdentifier { get; set; } = string.Empty;

    [Required]
    [DataType(DataType.Password)]
    [StringLength(100, MinimumLength = 8)]
    public string Password { get; set; } = string.Empty;

    [Display(Name = "Remember me")]
    public bool RememberMe { get; set; }
}

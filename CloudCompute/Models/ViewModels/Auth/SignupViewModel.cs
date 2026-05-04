using System.ComponentModel.DataAnnotations;

namespace CloudCompute.Models.ViewModels.Auth;

public class SignupViewModel
{
    [Required]
    [Display(Name = "Full name")]
    [StringLength(100)]
    public string FullName { get; set; } = string.Empty;

    [Required]
    [Display(Name = "Username")]
    [StringLength(256, MinimumLength = 3)]
    public string UserName { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    [StringLength(256)]
    public string Email { get; set; } = string.Empty;

    [Required]
    [DataType(DataType.Password)]
    [StringLength(100, MinimumLength = 8)]
    public string Password { get; set; } = string.Empty;

    [Required]
    [DataType(DataType.Password)]
    [Display(Name = "Confirm password")]
    [Compare(nameof(Password), ErrorMessage = "The password and confirmation password do not match.")]
    public string ConfirmPassword { get; set; } = string.Empty;
}

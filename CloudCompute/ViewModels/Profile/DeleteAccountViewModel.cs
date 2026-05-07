using System.ComponentModel.DataAnnotations;

namespace CloudCompute.ViewModels.Profile;

public class DeleteAccountViewModel
{
    [Required(ErrorMessage = "Enter your current password to confirm account deletion.")]
    [DataType(DataType.Password)]
    [Display(Name = "Current Password")]
    public string CurrentPassword { get; set; } = string.Empty;
}

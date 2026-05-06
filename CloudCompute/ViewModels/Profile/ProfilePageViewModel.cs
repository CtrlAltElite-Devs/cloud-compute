namespace CloudCompute.ViewModels.Profile;

public class ProfilePageViewModel
{
    public string FirstName { get; set; } = string.Empty;

    public string LastName { get; set; } = string.Empty;

    public string UserName { get; set; } = string.Empty;

    public string? ProfilePicturePath { get; set; }

    public string FullName => $"{FirstName} {LastName}".Trim();

    public ProfileEditViewModel Profile { get; set; } = new();

    public ChangePasswordViewModel Password { get; set; } = new();
}

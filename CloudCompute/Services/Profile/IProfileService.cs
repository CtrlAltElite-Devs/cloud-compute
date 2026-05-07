using CloudCompute.Services.Common;
using CloudCompute.ViewModels.Profile;

namespace CloudCompute.Services.Profile;

public interface IProfileService
{
    Task<ServiceResult> UpdateProfileAsync(Guid userId, ProfileEditViewModel model);

    Task<ServiceResult> ChangePasswordAsync(Guid userId, ChangePasswordViewModel model);

    Task<ServiceResult> UpdateAvatarAsync(Guid userId, IFormFile? file);

    Task<ServiceResult> DeleteAccountAsync(Guid userId, DeleteAccountViewModel model);
}

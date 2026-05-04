using CloudCompute.Models.ViewModels.Auth;
using CloudCompute.Services.Common;

namespace CloudCompute.Services.Auth;

public interface IAuthService
{
    Task<ServiceResult> LoginAsync(LoginViewModel model);

    Task<ServiceResult> SignupAsync(SignupViewModel model);

    Task LogoutAsync();
}

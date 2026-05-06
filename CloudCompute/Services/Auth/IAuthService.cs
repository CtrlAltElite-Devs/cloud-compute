using CloudCompute.Services.Common;
using CloudCompute.ViewModels.Auth;

namespace CloudCompute.Services.Auth;

public interface IAuthService
{
    Task<ServiceResult> LoginAsync(LoginViewModel model);

    Task<ServiceResult> AdminLoginAsync(LoginViewModel model);

    Task<ServiceResult> SignupAsync(SignupViewModel model);

    Task LogoutAsync();
}

using CloudCompute.Services.Common;
using CloudCompute.ViewModels.Admin;

namespace CloudCompute.Services.Admin;

public interface IAdminUserService
{
    Task<AdminUserListViewModel> ListAsync(AdminUserFilter filter);

    Task<AdminUserDetailViewModel?> GetDetailAsync(Guid userId);

    Task<ServiceResult> SetActiveAsync(Guid userId, bool isActive, Guid actingAdminId);

    Task<ServiceResult> SetOwnerVerifiedAsync(Guid userId, bool isVerified);
}

using CloudCompute.Services.Common;
using CloudCompute.ViewModels.Admin;

namespace CloudCompute.Services.Admin;

public interface IAdminListingService
{
    Task<AdminListingListViewModel> ListAsync(AdminListingFilter filter);

    Task<AdminListingDetailViewModel?> GetDetailAsync(Guid gpuId);

    Task<ServiceResult> ApproveAsync(Guid gpuId);

    Task<ServiceResult> RejectAsync(Guid gpuId, string? reason);
}

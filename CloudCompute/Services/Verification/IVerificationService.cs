using CloudCompute.Models.Enums;
using CloudCompute.Services.Common;
using CloudCompute.ViewModels.Verification;

namespace CloudCompute.Services.Verification;

public interface IVerificationService
{
    Task<OwnerVerificationStatus?> GetLatestRequestStatusAsync(Guid userId);

    Task<VerificationPageViewModel> GetPageViewModelAsync(Guid userId);

    Task<ServiceResult> SubmitRequestAsync(Guid userId, RequestVerificationViewModel model);

    Task<IReadOnlyList<VerificationRequestRowViewModel>> ListPendingAsync();

    Task<ServiceResult> ApproveAsync(Guid requestId, Guid adminId, string? notes);

    Task<ServiceResult> RejectAsync(Guid requestId, Guid adminId, string? notes);
}

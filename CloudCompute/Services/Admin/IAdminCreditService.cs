using CloudCompute.Services.Common;
using CloudCompute.ViewModels.Admin;

namespace CloudCompute.Services.Admin;

public interface IAdminCreditService
{
    Task<ServiceResult> GrantAsync(AdminGrantCreditViewModel model, Guid adminId);

    Task<BulkGrantResult> BulkGrantAsync(AdminBulkGrantViewModel model, Guid adminId);

    Task<ServiceResult> RevokeAsync(AdminRevokeCreditViewModel model, Guid adminId);

    Task<AdminCreditLedgerViewModel> GetLedgerAsync(AdminCreditLedgerFilter filter);
}

public sealed class BulkGrantResult
{
    public BulkGrantResult(ServiceResult result, int affectedUsers)
    {
        Result = result;
        AffectedUsers = affectedUsers;
    }

    public ServiceResult Result { get; }
    public int AffectedUsers { get; }
}

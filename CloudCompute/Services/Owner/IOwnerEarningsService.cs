using CloudCompute.ViewModels.Owner;

namespace CloudCompute.Services.Owner;

public interface IOwnerEarningsService
{
    Task<OwnerEarningsViewModel> GetEarningsAsync(Guid ownerId);
}

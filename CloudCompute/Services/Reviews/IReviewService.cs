using CloudCompute.Services.Common;
using CloudCompute.ViewModels.Rentals;
using CloudCompute.ViewModels.Reviews;

namespace CloudCompute.Services.Reviews;

public interface IReviewService
{
    Task<RentalReviewFormViewModel?> GetFormAsync(Guid renterId, Guid rentalId);

    Task<ServiceResult> CreateAsync(Guid renterId, RentalReviewFormViewModel form);

    Task<MyReviewsViewModel> GetMineAsync(Guid renterId);
}

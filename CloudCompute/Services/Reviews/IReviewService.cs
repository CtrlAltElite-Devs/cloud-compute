using CloudCompute.Services.Common;
using CloudCompute.ViewModels.Rentals;

namespace CloudCompute.Services.Reviews;

public interface IReviewService
{
    Task<RentalReviewFormViewModel?> GetFormAsync(Guid renterId, Guid rentalId);

    Task<ServiceResult> CreateAsync(Guid renterId, RentalReviewFormViewModel form);
}

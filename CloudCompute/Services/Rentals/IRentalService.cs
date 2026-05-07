using CloudCompute.Services.Common;
using CloudCompute.ViewModels.Rentals;

namespace CloudCompute.Services.Rentals;

public interface IRentalService
{
    Task<RentalConfirmViewModel?> GetConfirmationAsync(Guid renterId, Guid gpuId, int? durationHours = null);

    Task<RentalCreateResult> CreateAsync(Guid renterId, Guid gpuId, int durationHours);

    Task<ServiceResult> TerminateAsync(Guid renterId, Guid rentalId);

    Task<ActiveRentalsViewModel> GetActiveAsync(Guid renterId);

    Task<RentalHistoryViewModel> GetHistoryAsync(Guid renterId, RentalHistoryFilterViewModel filter);

    Task<RentalReceiptViewModel?> GetReceiptAsync(Guid renterId, Guid rentalId);
}

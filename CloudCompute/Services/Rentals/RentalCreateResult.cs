using CloudCompute.Services.Common;

namespace CloudCompute.Services.Rentals;

public sealed class RentalCreateResult
{
    private RentalCreateResult(ServiceResult serviceResult, Guid? rentalId)
    {
        ServiceResult = serviceResult;
        RentalId = rentalId;
    }

    public ServiceResult ServiceResult { get; }

    public Guid? RentalId { get; }

    public bool Succeeded => ServiceResult.Succeeded;

    public static RentalCreateResult Success(Guid rentalId) => new(ServiceResult.Success(), rentalId);

    public static RentalCreateResult Failed(params ServiceError[] errors) => new(ServiceResult.Failed(errors), null);

    public static RentalCreateResult FromServiceResult(ServiceResult serviceResult) => new(serviceResult, null);
}

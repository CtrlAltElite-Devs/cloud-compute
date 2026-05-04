namespace CloudCompute.Services.Common;

public class ServiceResult
{
    private ServiceResult(bool succeeded, IReadOnlyCollection<ServiceError> errors)
    {
        Succeeded = succeeded;
        Errors = errors;
    }

    public bool Succeeded { get; }

    public IReadOnlyCollection<ServiceError> Errors { get; }

    public static ServiceResult Success()
    {
        return new ServiceResult(succeeded: true, Array.Empty<ServiceError>());
    }

    public static ServiceResult Failed(params ServiceError[] errors)
    {
        return new ServiceResult(succeeded: false, errors);
    }
}

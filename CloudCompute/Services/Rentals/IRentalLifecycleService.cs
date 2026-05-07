namespace CloudCompute.Services.Rentals;

public interface IRentalLifecycleService
{
    Task<int> CompleteExpiredActiveRentalsAsync(CancellationToken cancellationToken = default);
}

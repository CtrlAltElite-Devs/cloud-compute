using CloudCompute.Constants;
using CloudCompute.Data;
using CloudCompute.Models.Enums;
using CloudCompute.Services.Notifications;
using Microsoft.EntityFrameworkCore;

namespace CloudCompute.Services.Rentals;

public class RentalLifecycleService : IRentalLifecycleService
{
    private readonly AppDbContext _dbContext;
    private readonly INotificationService _notificationService;

    public RentalLifecycleService(AppDbContext dbContext, INotificationService notificationService)
    {
        _dbContext = dbContext;
        _notificationService = notificationService;
    }

    public async Task<int> CompleteExpiredActiveRentalsAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var completed = await _dbContext.Rentals
            .Include(r => r.Gpu)
            .Include(r => r.Renter)
            .Where(r => r.Status == RentalStatus.Active && r.EndTime <= now)
            .ToListAsync(cancellationToken);

        if (completed.Count == 0)
        {
            return 0;
        }

        await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

        foreach (var rental in completed)
        {
            rental.Status = RentalStatus.Completed;

            if (rental.Gpu is not null)
            {
                rental.Gpu.Status = GpuStatus.Available;
            }

            _notificationService.Create(
                rental.RenterId,
                NotificationType.RentalCompleted,
                $"Rental {rental.ReferenceNumber} is complete.",
                "/rentals/history");

            _notificationService.Create(
                rental.OwnerId,
                NotificationType.RentalCompleted,
                $"{rental.Renter?.FullName ?? "A renter"} completed rental {rental.ReferenceNumber}.",
                NotificationConstants.Routes.MyListingsPath);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return completed.Count;
    }
}

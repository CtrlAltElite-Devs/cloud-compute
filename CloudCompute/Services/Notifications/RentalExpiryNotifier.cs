using CloudCompute.Constants;
using CloudCompute.Data;
using CloudCompute.Models.Enums;
using Microsoft.EntityFrameworkCore;

namespace CloudCompute.Services.Notifications;

public class RentalExpiryNotifier : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<RentalExpiryNotifier> _logger;
    private readonly TimeSpan _pollInterval = TimeSpan.FromSeconds(NotificationConstants.ExpiryWatcher.PollIntervalSeconds);
    private readonly TimeSpan _warningWindow = TimeSpan.FromMinutes(NotificationConstants.ExpiryWatcher.WarningWindowMinutes);

    public RentalExpiryNotifier(IServiceScopeFactory scopeFactory, ILogger<RentalExpiryNotifier> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(_pollInterval);

        do
        {
            try
            {
                await NotifyExpiringRentalsAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "RentalExpiryNotifier tick failed");
            }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    private async Task NotifyExpiringRentalsAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var notificationService = scope.ServiceProvider.GetRequiredService<INotificationService>();

        var now = DateTime.UtcNow;
        var threshold = now.Add(_warningWindow);

        var completed = await dbContext.Rentals
            .Include(r => r.Gpu)
            .Include(r => r.Renter)
            .Where(r => r.Status == RentalStatus.Active && r.EndTime <= now)
            .ToListAsync(cancellationToken);

        if (completed.Count > 0)
        {
            await using var completionTransaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

            foreach (var rental in completed)
            {
                rental.Status = RentalStatus.Completed;

                if (rental.Gpu is not null)
                {
                    rental.Gpu.Status = GpuStatus.Available;
                }

                notificationService.Create(
                    rental.RenterId,
                    NotificationType.RentalCompleted,
                    $"Rental {rental.ReferenceNumber} is complete.",
                    "/rentals/history");

                notificationService.Create(
                    rental.OwnerId,
                    NotificationType.RentalCompleted,
                    $"{rental.Renter?.FullName ?? "A renter"} completed rental {rental.ReferenceNumber}.",
                    NotificationConstants.Routes.MyListingsPath);
            }

            await dbContext.SaveChangesAsync(cancellationToken);
            await completionTransaction.CommitAsync(cancellationToken);

            _logger.LogInformation("RentalExpiryNotifier completed {Count} expired rentals", completed.Count);
        }

        var expiring = await dbContext.Rentals
            .Where(r => r.Status == RentalStatus.Active
                        && r.ExpiryNotifiedAt == null
                        && r.EndTime > now
                        && r.EndTime <= threshold)
            .ToListAsync(cancellationToken);

        if (expiring.Count == 0)
        {
            return;
        }

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

        foreach (var rental in expiring)
        {
            var message = string.Format(NotificationConstants.Messages.RentalExpiringFormat, rental.ReferenceNumber);
            notificationService.Create(
                rental.RenterId,
                NotificationType.RentalExpiring,
                message,
                NotificationConstants.Routes.ActiveRentalsPath);

            rental.ExpiryNotifiedAt = now;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        _logger.LogInformation("RentalExpiryNotifier dispatched {Count} expiry warnings", expiring.Count);
    }
}

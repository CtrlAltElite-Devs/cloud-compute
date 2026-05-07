using CloudCompute.Constants;
using CloudCompute.Data;
using CloudCompute.Models.Enums;
using CloudCompute.Services.Rentals;
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
        var lifecycleService = scope.ServiceProvider.GetRequiredService<IRentalLifecycleService>();

        var now = DateTime.UtcNow;
        var threshold = now.Add(_warningWindow);

        var completedCount = await lifecycleService.CompleteExpiredActiveRentalsAsync(cancellationToken);
        if (completedCount > 0)
        {
            _logger.LogInformation("RentalExpiryNotifier completed {Count} expired rentals", completedCount);
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

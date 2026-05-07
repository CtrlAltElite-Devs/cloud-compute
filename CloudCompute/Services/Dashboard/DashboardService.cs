using CloudCompute.Constants;
using CloudCompute.Data;
using CloudCompute.Models.Enums;
using CloudCompute.ViewModels.Dashboard;
using Microsoft.EntityFrameworkCore;

namespace CloudCompute.Services.Dashboard;

public class DashboardService : IDashboardService
{
    private readonly AppDbContext _dbContext;

    public DashboardService(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<DashboardViewModel?> GetDashboardAsync(Guid userId)
    {
        var user = await _dbContext.Users
            .AsNoTracking()
            .Where(user => user.Id == userId)
            .Select(user => new
            {
                user.FirstName,
                user.UserName,
                user.CreditBalance
            })
            .SingleOrDefaultAsync();

        if (user is null)
        {
            return null;
        }

        var now = DateTime.UtcNow;
        var monthStart = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);

        var activeRentalCount = await _dbContext.Rentals
            .AsNoTracking()
            .CountAsync(rental => rental.RenterId == userId && rental.Status == RentalStatus.Active);

        var currentMonthSpend = await _dbContext.Rentals
            .AsNoTracking()
            .Where(rental => rental.RenterId == userId && rental.CreatedAt >= monthStart)
            .SumAsync(rental => (decimal?)rental.TotalCost);

        var lifetimeHours = await _dbContext.Rentals
            .AsNoTracking()
            .Where(rental => rental.RenterId == userId)
            .SumAsync(rental => (int?)rental.DurationHours);

        var activeRentals = await _dbContext.Rentals
            .AsNoTracking()
            .Where(rental => rental.RenterId == userId && rental.Status == RentalStatus.Active)
            .OrderBy(rental => rental.EndTime)
            .Take(DashboardConstants.Preview.ActiveRentalLimit)
            .Select(rental => new DashboardRentalPreviewViewModel
            {
                Id = rental.Id,
                GpuName = rental.Gpu == null ? "GPU Rental" : rental.Gpu.Name,
                GpuModel = rental.Gpu == null ? "GPU" : rental.Gpu.Model,
                OwnerName = rental.Owner == null ? "Owner" : (rental.Owner.FirstName + " " + rental.Owner.LastName),
                ImagePath = rental.Gpu == null ? null : rental.Gpu.ImagePath,
                EndTime = rental.EndTime,
                DurationHours = rental.DurationHours,
                TotalCost = rental.TotalCost
            })
            .ToListAsync();

        var notifications = await _dbContext.Notifications
            .AsNoTracking()
            .Where(notification => notification.UserId == userId)
            .OrderByDescending(notification => notification.CreatedAt)
            .Take(DashboardConstants.Preview.NotificationLimit)
            .Select(notification => new DashboardNotificationPreviewViewModel
            {
                Id = notification.Id,
                Type = notification.Type,
                Message = notification.Message,
                IsRead = notification.IsRead,
                CreatedAt = notification.CreatedAt
            })
            .ToListAsync();

        return new DashboardViewModel
        {
            DisplayName = string.IsNullOrWhiteSpace(user.FirstName) ? user.UserName : user.FirstName,
            CreditBalance = user.CreditBalance,
            ActiveRentalCount = activeRentalCount,
            CurrentMonthSpend = currentMonthSpend ?? 0m,
            LifetimeComputeHours = lifetimeHours ?? 0,
            ActiveRentals = activeRentals,
            Notifications = notifications
        };
    }
}

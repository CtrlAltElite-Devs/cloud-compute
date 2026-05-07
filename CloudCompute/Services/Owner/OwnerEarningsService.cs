using CloudCompute.Data;
using CloudCompute.Models.Enums;
using CloudCompute.ViewModels.Owner;
using Microsoft.EntityFrameworkCore;

namespace CloudCompute.Services.Owner;

public class OwnerEarningsService : IOwnerEarningsService
{
    private readonly AppDbContext _dbContext;

    public OwnerEarningsService(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<OwnerEarningsViewModel> GetEarningsAsync(Guid ownerId)
    {
        var isOwnerVerified = await _dbContext.Users
            .AsNoTracking()
            .Where(u => u.Id == ownerId)
            .Select(u => (bool?)u.IsOwnerVerified)
            .FirstOrDefaultAsync() ?? false;

        var totalEarned = await _dbContext.CreditTransactions
            .AsNoTracking()
            .Where(t => t.UserId == ownerId && t.Type == CreditTransactionType.RentalEarning)
            .SumAsync(t => (decimal?)t.Amount) ?? 0m;

        var pendingPayouts = await _dbContext.Rentals
            .AsNoTracking()
            .Where(r => r.OwnerId == ownerId && r.Status == RentalStatus.Active)
            .SumAsync(r => (decimal?)r.OwnerEarnings) ?? 0m;

        var activeRentalCount = await _dbContext.Rentals
            .AsNoTracking()
            .CountAsync(r => r.OwnerId == ownerId && r.Status == RentalStatus.Active);

        var completedRentalCount = await _dbContext.Rentals
            .AsNoTracking()
            .CountAsync(r => r.OwnerId == ownerId && r.Status == RentalStatus.Completed);

        var perGpu = await _dbContext.Rentals
            .AsNoTracking()
            .Where(r => r.OwnerId == ownerId &&
                        (r.Status == RentalStatus.Completed || r.Status == RentalStatus.Active))
            .GroupBy(r => r.GpuId)
            .Select(g => new
            {
                GpuId = g.Key,
                TotalEarnings = g.Sum(r => r.OwnerEarnings),
                RentalCount = g.Count()
            })
            .ToListAsync();

        var gpuIds = perGpu.Select(p => p.GpuId).ToList();
        var gpuLookup = await _dbContext.Gpus
            .AsNoTracking()
            .Where(g => gpuIds.Contains(g.Id))
            .Select(g => new { g.Id, g.Name, g.Model, g.ImagePath })
            .ToDictionaryAsync(g => g.Id);

        var perGpuVms = perGpu
            .Select(p =>
            {
                gpuLookup.TryGetValue(p.GpuId, out var meta);
                return new OwnerEarningsByGpuViewModel
                {
                    GpuId = p.GpuId,
                    Name = meta?.Name ?? "Removed listing",
                    Model = meta?.Model ?? string.Empty,
                    ImagePath = meta?.ImagePath,
                    TotalEarnings = p.TotalEarnings,
                    RentalCount = p.RentalCount
                };
            })
            .OrderByDescending(p => p.TotalEarnings)
            .ToList();

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var thirtyDaysAgo = DateTime.UtcNow.Date.AddDays(-29);

        var dailyTotals = await _dbContext.CreditTransactions
            .AsNoTracking()
            .Where(t => t.UserId == ownerId &&
                        t.Type == CreditTransactionType.RentalEarning &&
                        t.CreatedAt >= thirtyDaysAgo)
            .GroupBy(t => t.CreatedAt.Date)
            .Select(g => new { Date = g.Key, Amount = g.Sum(t => t.Amount) })
            .ToListAsync();

        var dailyMap = dailyTotals.ToDictionary(d => DateOnly.FromDateTime(d.Date), d => d.Amount);

        var last30 = BuildBuckets(today, days: 30, dailyMap);
        var last7 = last30.TakeLast(7).ToList();

        return new OwnerEarningsViewModel
        {
            IsOwnerVerified = isOwnerVerified,
            TotalEarnedAllTime = totalEarned,
            PendingPayouts = pendingPayouts,
            ActiveRentalCount = activeRentalCount,
            CompletedRentalCount = completedRentalCount,
            PerGpu = perGpuVms,
            Last7Days = last7,
            Last30Days = last30
        };
    }

    private static List<OwnerEarningsChartPointViewModel> BuildBuckets(
        DateOnly today,
        int days,
        IDictionary<DateOnly, decimal> dailyMap)
    {
        var points = new List<OwnerEarningsChartPointViewModel>(days);
        for (var offset = days - 1; offset >= 0; offset--)
        {
            var date = today.AddDays(-offset);
            dailyMap.TryGetValue(date, out var amount);
            points.Add(new OwnerEarningsChartPointViewModel { Date = date, Amount = amount });
        }
        return points;
    }
}

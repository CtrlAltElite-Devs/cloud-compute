using CloudCompute.Data;
using CloudCompute.Models.Enums;
using CloudCompute.ViewModels.Admin;
using Microsoft.EntityFrameworkCore;

namespace CloudCompute.Services.Admin;

public class AdminAnalyticsService : IAdminAnalyticsService
{
    private readonly AppDbContext _dbContext;

    public AdminAnalyticsService(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<AdminAnalyticsViewModel> GetAsync()
    {
        var users = await _dbContext.Users
            .AsNoTracking()
            .Select(u => new { u.Id, u.Role, u.IsActive, u.IsOwnerVerified, u.CreditBalance })
            .ToListAsync();

        var totalUsers = users.Count;
        var adminCount = users.Count(u => u.Role == UserRole.Admin);
        var activeUsers = users.Count(u => u.IsActive);
        var suspendedUsers = users.Count(u => !u.IsActive);
        var verifiedOwners = users.Count(u => u.IsOwnerVerified);
        var totalCirculation = users.Sum(u => u.CreditBalance);

        var grantTotals = await _dbContext.CreditTransactions
            .AsNoTracking()
            .Where(t => t.Type == CreditTransactionType.AdminGrant)
            .SumAsync(t => (decimal?)t.Amount) ?? 0m;

        var revokeTotals = await _dbContext.CreditTransactions
            .AsNoTracking()
            .Where(t => t.Type == CreditTransactionType.Revoke)
            .SumAsync(t => (decimal?)t.Amount) ?? 0m;

        var earningTotals = await _dbContext.CreditTransactions
            .AsNoTracking()
            .Where(t => t.Type == CreditTransactionType.RentalEarning)
            .SumAsync(t => (decimal?)t.Amount) ?? 0m;

        var listingCounts = await _dbContext.Gpus
            .AsNoTracking()
            .GroupBy(g => g.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToListAsync();

        var topEarners = await _dbContext.CreditTransactions
            .AsNoTracking()
            .Where(t => t.Type == CreditTransactionType.RentalEarning && t.User != null)
            .GroupBy(t => new { t.UserId, t.User!.FirstName, t.User.LastName })
            .Select(g => new TopEarnerRow
            {
                OwnerId = g.Key.UserId,
                OwnerDisplay = g.Key.FirstName + " " + g.Key.LastName,
                TotalEarned = g.Sum(t => t.Amount)
            })
            .OrderByDescending(r => r.TotalEarned)
            .Take(10)
            .ToListAsync();

        return new AdminAnalyticsViewModel
        {
            TotalUsers = totalUsers,
            ActiveUsers = activeUsers,
            SuspendedUsers = suspendedUsers,
            AdminCount = adminCount,
            VerifiedOwners = verifiedOwners,
            TotalCreditsInCirculation = totalCirculation,
            TotalCreditsGranted = grantTotals,
            TotalCreditsRevoked = Math.Abs(revokeTotals),
            TotalRentalEarnings = earningTotals,
            ListingCountsByStatus = listingCounts.ToDictionary(c => c.Status, c => c.Count),
            TopEarners = topEarners
        };
    }
}

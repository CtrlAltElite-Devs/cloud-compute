using CloudCompute.Data;
using CloudCompute.Models.Enums;
using CloudCompute.ViewModels.Admin;
using Microsoft.EntityFrameworkCore;

namespace CloudCompute.Services.Admin;

public class AdminDashboardService : IAdminDashboardService
{
    private readonly AppDbContext _dbContext;

    public AdminDashboardService(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<AdminDashboardViewModel> GetAsync()
    {
        var pendingVerifications = await _dbContext.OwnerVerificationRequests
            .AsNoTracking()
            .CountAsync(r => r.Status == OwnerVerificationStatus.Pending);

        var pendingListings = await _dbContext.Gpus
            .AsNoTracking()
            .CountAsync(g => g.Status == GpuStatus.Pending);

        var activeUsers = await _dbContext.Users.AsNoTracking().CountAsync(u => u.IsActive);
        var suspendedUsers = await _dbContext.Users.AsNoTracking().CountAsync(u => !u.IsActive);

        var totalCirculation = await _dbContext.Users
            .AsNoTracking()
            .SumAsync(u => (decimal?)u.CreditBalance) ?? 0m;

        var recent = await _dbContext.CreditTransactions
            .AsNoTracking()
            .Include(t => t.User)
            .Include(t => t.Admin)
            .Where(t => t.Type == CreditTransactionType.AdminGrant || t.Type == CreditTransactionType.Revoke)
            .OrderByDescending(t => t.CreatedAt)
            .Take(5)
            .Select(t => new AdminDashboardCreditEvent
            {
                CreatedAt = t.CreatedAt,
                UserDisplay = t.User != null ? (t.User.FirstName + " " + t.User.LastName) : "(unknown)",
                Amount = t.Amount,
                Reason = t.Reason,
                AdminDisplay = t.Admin != null ? (t.Admin.FirstName + " " + t.Admin.LastName) : null
            })
            .ToListAsync();

        return new AdminDashboardViewModel
        {
            PendingVerifications = pendingVerifications,
            PendingListings = pendingListings,
            ActiveUsers = activeUsers,
            SuspendedUsers = suspendedUsers,
            TotalCreditsInCirculation = totalCirculation,
            RecentCreditEvents = recent
        };
    }
}

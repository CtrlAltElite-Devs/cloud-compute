using CloudCompute.Constants;
using CloudCompute.Data;
using CloudCompute.Models;
using CloudCompute.Models.Enums;
using CloudCompute.Services.Common;
using CloudCompute.ViewModels.Admin;
using Microsoft.EntityFrameworkCore;

namespace CloudCompute.Services.Admin;

public class AdminCreditService : IAdminCreditService
{
    private readonly AppDbContext _dbContext;

    public AdminCreditService(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<ServiceResult> GrantAsync(AdminGrantCreditViewModel model, Guid adminId)
    {
        if (model.Amount < AdminConstants.Validation.MinGrantAmount || model.Amount > AdminConstants.Validation.MaxGrantAmount)
        {
            return Fail(AdminConstants.Messages.AmountOutOfRange);
        }

        var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Id == model.UserId);
        if (user is null)
        {
            return Fail(AdminConstants.Messages.UserNotFound);
        }

        if (user.Role == UserRole.Admin)
        {
            return Fail(AdminConstants.Messages.CannotGrantToAdmin);
        }

        await using var tx = await _dbContext.Database.BeginTransactionAsync();
        try
        {
            user.CreditBalance += model.Amount;

            _dbContext.CreditTransactions.Add(new CreditTransaction
            {
                UserId = user.Id,
                Type = CreditTransactionType.AdminGrant,
                Amount = model.Amount,
                BalanceAfter = user.CreditBalance,
                AdminId = adminId,
                Reason = model.Reason.Trim()
            });

            await _dbContext.SaveChangesAsync();
            await tx.CommitAsync();
            return ServiceResult.Success();
        }
        catch (DbUpdateException)
        {
            await tx.RollbackAsync();
            return Fail(AdminConstants.Messages.SaveFailed);
        }
    }

    public async Task<BulkGrantResult> BulkGrantAsync(AdminBulkGrantViewModel model, Guid adminId)
    {
        if (model.Amount < AdminConstants.Validation.MinGrantAmount || model.Amount > AdminConstants.Validation.MaxGrantAmount)
        {
            return new BulkGrantResult(Fail(AdminConstants.Messages.AmountOutOfRange), 0);
        }

        var query = _dbContext.Users.Where(u => u.Role == UserRole.Member);
        if (model.ActiveMembersOnly)
        {
            query = query.Where(u => u.IsActive);
        }

        var users = await query.ToListAsync();
        if (users.Count == 0)
        {
            return new BulkGrantResult(ServiceResult.Success(), 0);
        }

        await using var tx = await _dbContext.Database.BeginTransactionAsync();
        try
        {
            var reason = model.Reason.Trim();
            foreach (var user in users)
            {
                user.CreditBalance += model.Amount;

                _dbContext.CreditTransactions.Add(new CreditTransaction
                {
                    UserId = user.Id,
                    Type = CreditTransactionType.AdminGrant,
                    Amount = model.Amount,
                    BalanceAfter = user.CreditBalance,
                    AdminId = adminId,
                    Reason = reason
                });
            }

            await _dbContext.SaveChangesAsync();
            await tx.CommitAsync();
            return new BulkGrantResult(ServiceResult.Success(), users.Count);
        }
        catch (DbUpdateException)
        {
            await tx.RollbackAsync();
            return new BulkGrantResult(Fail(AdminConstants.Messages.SaveFailed), 0);
        }
    }

    public async Task<ServiceResult> RevokeAsync(AdminRevokeCreditViewModel model, Guid adminId)
    {
        if (model.Amount < AdminConstants.Validation.MinGrantAmount || model.Amount > AdminConstants.Validation.MaxGrantAmount)
        {
            return Fail(AdminConstants.Messages.AmountOutOfRange);
        }

        var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Id == model.UserId);
        if (user is null)
        {
            return Fail(AdminConstants.Messages.UserNotFound);
        }

        if (user.Role == UserRole.Admin)
        {
            return Fail(AdminConstants.Messages.CannotGrantToAdmin);
        }

        if (user.CreditBalance < model.Amount)
        {
            return Fail(AdminConstants.Messages.InsufficientBalance);
        }

        await using var tx = await _dbContext.Database.BeginTransactionAsync();
        try
        {
            user.CreditBalance -= model.Amount;

            _dbContext.CreditTransactions.Add(new CreditTransaction
            {
                UserId = user.Id,
                Type = CreditTransactionType.Revoke,
                Amount = -model.Amount,
                BalanceAfter = user.CreditBalance,
                AdminId = adminId,
                Reason = model.Reason.Trim()
            });

            await _dbContext.SaveChangesAsync();
            await tx.CommitAsync();
            return ServiceResult.Success();
        }
        catch (DbUpdateException)
        {
            await tx.RollbackAsync();
            return Fail(AdminConstants.Messages.SaveFailed);
        }
    }

    public async Task<AdminCreditLedgerViewModel> GetLedgerAsync(AdminCreditLedgerFilter filter)
    {
        filter.Page = Math.Max(1, filter.Page);
        filter.PageSize = Math.Clamp(filter.PageSize, 1, AdminConstants.Pagination.MaxPageSize);

        var query = _dbContext.CreditTransactions
            .AsNoTracking()
            .Include(t => t.User)
            .Include(t => t.Admin)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(filter.UserQuery))
        {
            var q = filter.UserQuery.Trim();
            query = query.Where(t =>
                t.User != null &&
                (EF.Functions.Like(t.User.FirstName + " " + t.User.LastName, $"%{q}%") ||
                 EF.Functions.Like(t.User.UserName, $"%{q}%") ||
                 EF.Functions.Like(t.User.Email, $"%{q}%")));
        }

        if (filter.Type.HasValue)
        {
            query = query.Where(t => t.Type == filter.Type.Value);
        }

        if (filter.From.HasValue)
        {
            query = query.Where(t => t.CreatedAt >= filter.From.Value);
        }

        if (filter.To.HasValue)
        {
            var end = filter.To.Value.Date.AddDays(1);
            query = query.Where(t => t.CreatedAt < end);
        }

        var total = await query.CountAsync();

        var rows = await query
            .OrderByDescending(t => t.CreatedAt)
            .Skip((filter.Page - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .Select(t => new AdminCreditLedgerRow
            {
                Id = t.Id,
                CreatedAt = t.CreatedAt,
                UserId = t.UserId,
                UserDisplay = t.User != null ? (t.User.FirstName + " " + t.User.LastName + " · " + t.User.Email) : "(unknown)",
                Type = t.Type,
                Amount = t.Amount,
                BalanceAfter = t.BalanceAfter,
                Reason = t.Reason,
                AdminId = t.AdminId,
                AdminDisplay = t.Admin != null ? (t.Admin.FirstName + " " + t.Admin.LastName) : null
            })
            .ToListAsync();

        return new AdminCreditLedgerViewModel
        {
            Filter = filter,
            Rows = rows,
            TotalCount = total
        };
    }

    private static ServiceResult Fail(string message)
    {
        return ServiceResult.Failed(new ServiceError(AuthConstants.Validation.ModelErrorKey, message));
    }
}

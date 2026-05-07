using CloudCompute.Constants;
using CloudCompute.Data;
using CloudCompute.Models.Enums;
using CloudCompute.Services.Common;
using CloudCompute.ViewModels.Admin;
using Microsoft.EntityFrameworkCore;

namespace CloudCompute.Services.Admin;

public class AdminUserService : IAdminUserService
{
    private readonly AppDbContext _dbContext;

    public AdminUserService(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<AdminUserListViewModel> ListAsync(AdminUserFilter filter)
    {
        filter.Page = Math.Max(1, filter.Page);
        filter.PageSize = Math.Clamp(filter.PageSize, 1, AdminConstants.Pagination.MaxPageSize);

        var query = _dbContext.Users.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(filter.Query))
        {
            var q = filter.Query.Trim();
            query = query.Where(user =>
                EF.Functions.Like(user.FirstName + " " + user.LastName, $"%{q}%") ||
                EF.Functions.Like(user.UserName, $"%{q}%") ||
                EF.Functions.Like(user.Email, $"%{q}%"));
        }

        if (filter.Role.HasValue)
        {
            query = query.Where(u => u.Role == filter.Role.Value);
        }

        if (filter.IsActive.HasValue)
        {
            query = query.Where(u => u.IsActive == filter.IsActive.Value);
        }

        if (filter.IsOwnerVerified.HasValue)
        {
            query = query.Where(u => u.IsOwnerVerified == filter.IsOwnerVerified.Value);
        }

        var total = await query.CountAsync();

        var rows = await query
            .OrderByDescending(u => u.CreatedAt)
            .Skip((filter.Page - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .Select(u => new AdminUserRow
            {
                Id = u.Id,
                FullName = u.FirstName + " " + u.LastName,
                UserName = u.UserName,
                Email = u.Email,
                Role = u.Role,
                IsActive = u.IsActive,
                IsOwnerVerified = u.IsOwnerVerified,
                CreditBalance = u.CreditBalance,
                CreatedAt = u.CreatedAt
            })
            .ToListAsync();

        return new AdminUserListViewModel
        {
            Filter = filter,
            Rows = rows,
            TotalCount = total
        };
    }

    public async Task<AdminUserDetailViewModel?> GetDetailAsync(Guid userId)
    {
        var user = await _dbContext.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == userId);

        if (user is null)
        {
            return null;
        }

        var credits = await _dbContext.CreditTransactions
            .AsNoTracking()
            .Where(t => t.UserId == userId)
            .OrderByDescending(t => t.CreatedAt)
            .Take(25)
            .Select(t => new AdminUserCreditRow
            {
                CreatedAt = t.CreatedAt,
                Type = t.Type,
                Amount = t.Amount,
                BalanceAfter = t.BalanceAfter,
                Reason = t.Reason
            })
            .ToListAsync();

        var gpus = await _dbContext.Gpus
            .AsNoTracking()
            .Where(g => g.OwnerId == userId)
            .OrderByDescending(g => g.CreatedAt)
            .Select(g => new AdminUserGpuRow
            {
                Id = g.Id,
                Name = g.Name,
                Model = g.Model,
                Status = g.Status,
                PricePerHour = g.PricePerHour,
                CreatedAt = g.CreatedAt
            })
            .ToListAsync();

        return new AdminUserDetailViewModel
        {
            Id = user.Id,
            FullName = user.FullName,
            UserName = user.UserName,
            Email = user.Email,
            Bio = user.Bio,
            ProfilePicturePath = user.ProfilePicturePath,
            Role = user.Role,
            IsActive = user.IsActive,
            IsOwnerVerified = user.IsOwnerVerified,
            CreditBalance = user.CreditBalance,
            CreatedAt = user.CreatedAt,
            RecentCredits = credits,
            Gpus = gpus
        };
    }

    public async Task<ServiceResult> SetActiveAsync(Guid userId, bool isActive, Guid actingAdminId)
    {
        if (userId == actingAdminId)
        {
            return Fail(AdminConstants.Messages.CannotSuspendSelf);
        }

        var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Id == userId);
        if (user is null)
        {
            return Fail(AdminConstants.Messages.UserNotFound);
        }

        if (user.Role == UserRole.Admin)
        {
            return Fail(AdminConstants.Messages.CannotSuspendAdmin);
        }

        if (user.IsActive == isActive)
        {
            return ServiceResult.Success();
        }

        user.IsActive = isActive;
        await _dbContext.SaveChangesAsync();
        return ServiceResult.Success();
    }

    public async Task<ServiceResult> SetOwnerVerifiedAsync(Guid userId, bool isVerified)
    {
        var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Id == userId);
        if (user is null)
        {
            return Fail(AdminConstants.Messages.UserNotFound);
        }

        if (user.Role == UserRole.Admin)
        {
            return Fail(AdminConstants.Messages.CannotVerifyAdmin);
        }

        if (user.IsOwnerVerified == isVerified)
        {
            return ServiceResult.Success();
        }

        user.IsOwnerVerified = isVerified;
        await _dbContext.SaveChangesAsync();
        return ServiceResult.Success();
    }

    private static ServiceResult Fail(string message)
    {
        return ServiceResult.Failed(new ServiceError(AuthConstants.Validation.ModelErrorKey, message));
    }
}

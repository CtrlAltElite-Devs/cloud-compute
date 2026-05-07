using CloudCompute.Constants;
using CloudCompute.Data;
using CloudCompute.Models.Enums;
using CloudCompute.Services.Common;
using CloudCompute.Services.Notifications;
using CloudCompute.ViewModels.Admin;
using Microsoft.EntityFrameworkCore;

namespace CloudCompute.Services.Admin;

public class AdminListingService : IAdminListingService
{
    private readonly AppDbContext _dbContext;
    private readonly INotificationService _notificationService;

    public AdminListingService(AppDbContext dbContext, INotificationService notificationService)
    {
        _dbContext = dbContext;
        _notificationService = notificationService;
    }

    public async Task<AdminListingListViewModel> ListAsync(AdminListingFilter filter)
    {
        filter.Page = Math.Max(1, filter.Page);
        filter.PageSize = Math.Clamp(filter.PageSize, 1, AdminConstants.Pagination.MaxPageSize);

        var query = _dbContext.Gpus
            .AsNoTracking()
            .Include(g => g.Owner)
            .AsQueryable();

        if (filter.All)
        {
            filter.Status = null;
        }
        else if (filter.Status.HasValue)
        {
            query = query.Where(g => g.Status == filter.Status.Value);
        }

        if (!string.IsNullOrWhiteSpace(filter.OwnerQuery))
        {
            var q = filter.OwnerQuery.Trim();
            query = query.Where(g => g.Owner != null &&
                (EF.Functions.Like(g.Owner.FirstName + " " + g.Owner.LastName, $"%{q}%") ||
                 EF.Functions.Like(g.Owner.UserName, $"%{q}%") ||
                 EF.Functions.Like(g.Owner.Email, $"%{q}%")));
        }

        var total = await query.CountAsync();

        var rows = await query
            .OrderByDescending(g => g.CreatedAt)
            .Skip((filter.Page - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .Select(g => new AdminListingRow
            {
                Id = g.Id,
                Name = g.Name,
                Model = g.Model,
                Status = g.Status,
                PricePerHour = g.PricePerHour,
                ImagePath = g.ImagePath,
                CreatedAt = g.CreatedAt,
                OwnerId = g.OwnerId,
                OwnerDisplay = g.Owner != null ? (g.Owner.FirstName + " " + g.Owner.LastName) : "(unknown)"
            })
            .ToListAsync();

        var counts = await _dbContext.Gpus
            .AsNoTracking()
            .GroupBy(g => g.Status)
            .Select(group => new { Status = group.Key, Count = group.Count() })
            .ToListAsync();

        return new AdminListingListViewModel
        {
            Filter = filter,
            Rows = rows,
            TotalCount = total,
            CountsByStatus = counts.ToDictionary(c => c.Status, c => c.Count)
        };
    }

    public async Task<AdminListingDetailViewModel?> GetDetailAsync(Guid gpuId)
    {
        var gpu = await _dbContext.Gpus
            .AsNoTracking()
            .Include(g => g.Owner)
            .FirstOrDefaultAsync(g => g.Id == gpuId);

        if (gpu is null)
        {
            return null;
        }

        return new AdminListingDetailViewModel
        {
            Id = gpu.Id,
            Name = gpu.Name,
            Model = gpu.Model,
            VramGb = gpu.VramGb,
            CudaCores = gpu.CudaCores,
            PricePerHour = gpu.PricePerHour,
            MinRentalHours = gpu.MinRentalHours,
            Description = gpu.Description,
            ImagePath = gpu.ImagePath,
            Status = gpu.Status,
            RejectionReason = gpu.RejectionReason,
            CreatedAt = gpu.CreatedAt,
            OwnerId = gpu.OwnerId,
            OwnerDisplay = gpu.Owner != null ? gpu.Owner.FullName : "(unknown)",
            OwnerEmail = gpu.Owner?.Email ?? string.Empty,
            OwnerIsVerified = gpu.Owner?.IsOwnerVerified ?? false
        };
    }

    public async Task<ServiceResult> ApproveAsync(Guid gpuId)
    {
        var gpu = await _dbContext.Gpus.FirstOrDefaultAsync(g => g.Id == gpuId);
        if (gpu is null)
        {
            return Fail(AdminConstants.Messages.ListingNotFound);
        }

        if (gpu.Status != GpuStatus.Pending)
        {
            return Fail(AdminConstants.Messages.ListingNotPending);
        }

        gpu.Status = GpuStatus.Available;
        gpu.RejectionReason = null;
        _notificationService.Create(
            gpu.OwnerId,
            NotificationType.ListingApproved,
            string.Format(NotificationConstants.Messages.ListingApprovedFormat, gpu.Name),
            NotificationConstants.Routes.MyListingsPath);
        await _dbContext.SaveChangesAsync();
        return ServiceResult.Success();
    }

    public async Task<ServiceResult> RejectAsync(Guid gpuId, string? reason)
    {
        var gpu = await _dbContext.Gpus.FirstOrDefaultAsync(g => g.Id == gpuId);
        if (gpu is null)
        {
            return Fail(AdminConstants.Messages.ListingNotFound);
        }

        if (gpu.Status != GpuStatus.Pending)
        {
            return Fail(AdminConstants.Messages.ListingNotPending);
        }

        gpu.Status = GpuStatus.Rejected;
        gpu.RejectionReason = string.IsNullOrWhiteSpace(reason) ? null : reason.Trim();
        var rejectionMessage = string.IsNullOrWhiteSpace(gpu.RejectionReason)
            ? string.Format(NotificationConstants.Messages.ListingRejectedFormat, gpu.Name)
            : string.Format(NotificationConstants.Messages.ListingRejectedWithReasonFormat, gpu.Name, gpu.RejectionReason);
        _notificationService.Create(
            gpu.OwnerId,
            NotificationType.ListingRejected,
            rejectionMessage,
            NotificationConstants.Routes.MyListingsPath);
        await _dbContext.SaveChangesAsync();
        return ServiceResult.Success();
    }

    private static ServiceResult Fail(string message)
    {
        return ServiceResult.Failed(new ServiceError(AuthConstants.Validation.ModelErrorKey, message));
    }
}

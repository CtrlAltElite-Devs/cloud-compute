using CloudCompute.Constants;
using CloudCompute.Data;
using CloudCompute.Models;
using CloudCompute.Models.Enums;
using CloudCompute.Services.Common;
using CloudCompute.ViewModels.Verification;
using Microsoft.EntityFrameworkCore;

namespace CloudCompute.Services.Verification;

public class VerificationService : IVerificationService
{
    private readonly AppDbContext _dbContext;

    public VerificationService(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<OwnerVerificationStatus?> GetLatestRequestStatusAsync(Guid userId)
    {
        var latest = await _dbContext.OwnerVerificationRequests
            .AsNoTracking()
            .Where(request => request.UserId == userId)
            .OrderByDescending(request => request.SubmittedAt)
            .Select(request => (OwnerVerificationStatus?)request.Status)
            .FirstOrDefaultAsync();

        return latest;
    }

    public async Task<VerificationPageViewModel> GetPageViewModelAsync(Guid userId)
    {
        var latest = await _dbContext.OwnerVerificationRequests
            .AsNoTracking()
            .Where(request => request.UserId == userId)
            .OrderByDescending(request => request.SubmittedAt)
            .FirstOrDefaultAsync();

        return new VerificationPageViewModel
        {
            LatestRequestStatus = latest?.Status,
            LatestSubmittedAt = latest?.SubmittedAt,
            LatestJustification = latest?.Justification,
            LatestReviewNotes = latest?.ReviewNotes,
            Form = new RequestVerificationViewModel()
        };
    }

    public async Task<ServiceResult> SubmitRequestAsync(Guid userId, RequestVerificationViewModel model)
    {
        var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Id == userId);
        if (user is null)
        {
            return ServiceResult.Failed(CreateModelError(VerificationConstants.Messages.UserNotFound));
        }

        if (user.IsOwnerVerified)
        {
            return ServiceResult.Failed(CreateModelError(VerificationConstants.Messages.AlreadyVerified));
        }

        var hasPending = await _dbContext.OwnerVerificationRequests
            .AnyAsync(request => request.UserId == userId && request.Status == OwnerVerificationStatus.Pending);
        if (hasPending)
        {
            return ServiceResult.Failed(CreateModelError(VerificationConstants.Messages.PendingRequestExists));
        }

        var newRequest = new OwnerVerificationRequest
        {
            UserId = userId,
            Justification = model.Justification.Trim(),
            Status = OwnerVerificationStatus.Pending,
            SubmittedAt = DateTime.UtcNow
        };

        _dbContext.OwnerVerificationRequests.Add(newRequest);
        await _dbContext.SaveChangesAsync();

        return ServiceResult.Success();
    }

    public async Task<IReadOnlyList<VerificationRequestRowViewModel>> ListPendingAsync()
    {
        return await _dbContext.OwnerVerificationRequests
            .AsNoTracking()
            .Where(request => request.Status == OwnerVerificationStatus.Pending)
            .OrderBy(request => request.SubmittedAt)
            .Select(request => new VerificationRequestRowViewModel
            {
                Id = request.Id,
                UserId = request.UserId,
                UserFullName = request.User!.FirstName + " " + request.User.LastName,
                UserEmail = request.User!.Email,
                Justification = request.Justification,
                SubmittedAt = request.SubmittedAt,
                Status = request.Status
            })
            .ToListAsync();
    }

    public async Task<ServiceResult> ApproveAsync(Guid requestId, Guid adminId, string? notes)
    {
        var request = await _dbContext.OwnerVerificationRequests.FirstOrDefaultAsync(r => r.Id == requestId);
        if (request is null)
        {
            return ServiceResult.Failed(CreateModelError(VerificationConstants.Messages.RequestNotFound));
        }

        if (request.Status != OwnerVerificationStatus.Pending)
        {
            return ServiceResult.Failed(CreateModelError(VerificationConstants.Messages.RequestNotPending));
        }

        var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Id == request.UserId);
        if (user is null)
        {
            return ServiceResult.Failed(CreateModelError(VerificationConstants.Messages.UserNotFound));
        }

        user.IsOwnerVerified = true;
        request.Status = OwnerVerificationStatus.Approved;
        request.ReviewedAt = DateTime.UtcNow;
        request.ReviewedById = adminId;
        request.ReviewNotes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim();

        await _dbContext.SaveChangesAsync();
        return ServiceResult.Success();
    }

    public async Task<ServiceResult> RejectAsync(Guid requestId, Guid adminId, string? notes)
    {
        var request = await _dbContext.OwnerVerificationRequests.FirstOrDefaultAsync(r => r.Id == requestId);
        if (request is null)
        {
            return ServiceResult.Failed(CreateModelError(VerificationConstants.Messages.RequestNotFound));
        }

        if (request.Status != OwnerVerificationStatus.Pending)
        {
            return ServiceResult.Failed(CreateModelError(VerificationConstants.Messages.RequestNotPending));
        }

        request.Status = OwnerVerificationStatus.Rejected;
        request.ReviewedAt = DateTime.UtcNow;
        request.ReviewedById = adminId;
        request.ReviewNotes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim();

        await _dbContext.SaveChangesAsync();
        return ServiceResult.Success();
    }

    private static ServiceError CreateModelError(string message)
    {
        return new ServiceError(AuthConstants.Validation.ModelErrorKey, message);
    }
}

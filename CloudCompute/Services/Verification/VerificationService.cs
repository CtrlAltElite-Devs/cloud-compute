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
    private readonly IWebHostEnvironment _environment;

    public VerificationService(AppDbContext dbContext, IWebHostEnvironment environment)
    {
        _dbContext = dbContext;
        _environment = environment;
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
            LatestIdentityImagePath = latest?.IdentityImagePath,
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

        if (model.IdentityImage is not { Length: > 0 })
        {
            return ServiceResult.Failed(CreateModelError(VerificationConstants.Messages.IdentityImageRequired));
        }

        if (model.IdentityImage.Length > VerificationConstants.IdentityImage.MaxBytes)
        {
            return ServiceResult.Failed(CreateModelError(VerificationConstants.Messages.IdentityImageTooLarge));
        }

        var extension = Path.GetExtension(model.IdentityImage.FileName);
        if (string.IsNullOrEmpty(extension) || !VerificationConstants.IdentityImage.AllowedExtensions.Contains(extension))
        {
            return ServiceResult.Failed(CreateModelError(VerificationConstants.Messages.IdentityImageUnsupportedFormat));
        }

        var fileName = $"{Guid.NewGuid():N}{extension.ToLowerInvariant()}";
        var directoryPath = Path.Combine(_environment.WebRootPath, "uploads", "verification");
        Directory.CreateDirectory(directoryPath);
        var savedFullPath = Path.Combine(directoryPath, fileName);

        await using (var stream = new FileStream(savedFullPath, FileMode.Create))
        {
            await model.IdentityImage.CopyToAsync(stream);
        }

        var imagePath = $"/{VerificationConstants.IdentityImage.DirectorySegment}/{fileName}";

        var newRequest = new OwnerVerificationRequest
        {
            UserId = userId,
            Justification = model.Justification.Trim(),
            IdentityImagePath = imagePath,
            Status = OwnerVerificationStatus.Pending,
            SubmittedAt = DateTime.UtcNow
        };

        _dbContext.OwnerVerificationRequests.Add(newRequest);

        try
        {
            await _dbContext.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            TryDeletePhysicalFile(savedFullPath);
            return ServiceResult.Failed(CreateModelError(VerificationConstants.Messages.SaveFailed));
        }

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
                IdentityImagePath = request.IdentityImagePath,
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

    private static void TryDeletePhysicalFile(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return;
        }

        try
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
        catch
        {
            // Best-effort cleanup; failures here should not break the user-facing operation.
        }
    }

    private static ServiceError CreateModelError(string message)
    {
        return new ServiceError(AuthConstants.Validation.ModelErrorKey, message);
    }
}

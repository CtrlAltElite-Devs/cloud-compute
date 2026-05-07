using CloudCompute.Constants;
using CloudCompute.Data;
using CloudCompute.Models.Enums;
using CloudCompute.Services.Common;
using CloudCompute.ViewModels.Gpus;
using Microsoft.EntityFrameworkCore;
using GpuEntity = CloudCompute.Models.Gpu;

namespace CloudCompute.Services.Gpu;

public class GpuService : IGpuService
{
    private readonly AppDbContext _dbContext;
    private readonly IWebHostEnvironment _environment;

    public GpuService(AppDbContext dbContext, IWebHostEnvironment environment)
    {
        _dbContext = dbContext;
        _environment = environment;
    }

    public async Task<ServiceResult> CreateAsync(Guid ownerId, GpuCreateViewModel model)
    {
        var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Id == ownerId);
        if (user is null)
        {
            return ServiceResult.Failed(CreateModelError(GpuConstants.Messages.UserNotFound));
        }

        if (!user.IsOwnerVerified)
        {
            return ServiceResult.Failed(CreateModelError(GpuConstants.Messages.NotOwnerVerified));
        }

        var photoResult = await TrySavePhotoAsync(model.Photo);
        if (!photoResult.Succeeded)
        {
            return ServiceResult.Failed(photoResult.Errors.ToArray());
        }

        var gpu = new GpuEntity
        {
            OwnerId = ownerId,
            Name = model.Brand.Trim(),
            Model = model.Model.Trim(),
            VramGb = model.VramGb,
            CudaCores = model.CudaCores,
            PricePerHour = model.PricePerHour,
            MinRentalHours = model.MinRentalHours,
            Description = string.IsNullOrWhiteSpace(model.Description) ? null : model.Description.Trim(),
            ImagePath = photoResult.RelativePath,
            Status = GpuStatus.Pending
        };

        _dbContext.Gpus.Add(gpu);

        try
        {
            await _dbContext.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            TryDeletePhysicalFile(photoResult.AbsolutePath);
            return ServiceResult.Failed(CreateModelError(GpuConstants.Messages.SaveFailed));
        }

        return ServiceResult.Success();
    }

    public async Task<MyListingsViewModel> GetMineAsync(Guid ownerId)
    {
        var listings = await _dbContext.Gpus
            .AsNoTracking()
            .Where(g => g.OwnerId == ownerId)
            .OrderByDescending(g => g.CreatedAt)
            .Select(g => new MyListingItemViewModel
            {
                Id = g.Id,
                Name = g.Name,
                Model = g.Model,
                VramGb = g.VramGb,
                CudaCores = g.CudaCores,
                PricePerHour = g.PricePerHour,
                ImagePath = g.ImagePath,
                Status = g.Status,
                RejectionReason = g.RejectionReason,
                RentalCount = g.Rentals.Count,
                AverageRating = g.Reviews.Any()
                    ? (decimal)g.Reviews.Average(r => (double)r.Rating)
                    : g.AverageRating,
                HasReviews = g.Reviews.Any()
            })
            .ToListAsync();

        return new MyListingsViewModel { Listings = listings };
    }

    public async Task<ServiceResult> ToggleStatusAsync(Guid ownerId, Guid gpuId)
    {
        var gpu = await _dbContext.Gpus.FirstOrDefaultAsync(g => g.Id == gpuId);
        if (gpu is null || gpu.OwnerId != ownerId)
        {
            return ServiceResult.Failed(CreateModelError(GpuConstants.Messages.ListingNotFound));
        }

        if (gpu.Status is not (GpuStatus.Available or GpuStatus.Maintenance))
        {
            return ServiceResult.Failed(CreateModelError(GpuConstants.Messages.CannotToggleStatus));
        }

        gpu.Status = gpu.Status == GpuStatus.Available ? GpuStatus.Maintenance : GpuStatus.Available;

        try
        {
            await _dbContext.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            return ServiceResult.Failed(CreateModelError(GpuConstants.Messages.SaveFailed));
        }

        return ServiceResult.Success();
    }

    public async Task<ServiceResult> DeleteAsync(Guid ownerId, Guid gpuId)
    {
        var gpu = await _dbContext.Gpus
            .Include(g => g.Rentals)
            .Include(g => g.Reviews)
            .FirstOrDefaultAsync(g => g.Id == gpuId);

        if (gpu is null || gpu.OwnerId != ownerId)
        {
            return ServiceResult.Failed(CreateModelError(GpuConstants.Messages.ListingNotFound));
        }

        if (gpu.Status == GpuStatus.Rented)
        {
            return ServiceResult.Failed(CreateModelError(GpuConstants.Messages.CannotDeleteWhileRented));
        }

        if (gpu.Rentals.Any() || gpu.Reviews.Any())
        {
            return ServiceResult.Failed(CreateModelError(GpuConstants.Messages.CannotDeleteWithHistory));
        }

        var photoToDelete = gpu.ImagePath;
        _dbContext.Gpus.Remove(gpu);

        try
        {
            await _dbContext.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            return ServiceResult.Failed(CreateModelError(GpuConstants.Messages.SaveFailed));
        }

        TryDeletePhysicalFile(ResolveAbsolutePath(photoToDelete));

        return ServiceResult.Success();
    }

    public async Task<GpuEditViewModel?> GetForEditAsync(Guid ownerId, Guid gpuId)
    {
        var gpu = await _dbContext.Gpus
            .AsNoTracking()
            .FirstOrDefaultAsync(g => g.Id == gpuId);

        if (gpu is null || gpu.OwnerId != ownerId || gpu.Status == GpuStatus.Rented)
        {
            return null;
        }

        return new GpuEditViewModel
        {
            Id = gpu.Id,
            Brand = gpu.Name,
            Model = gpu.Model,
            VramGb = gpu.VramGb,
            CudaCores = gpu.CudaCores,
            PricePerHour = gpu.PricePerHour,
            MinRentalHours = gpu.MinRentalHours,
            Description = gpu.Description,
            ExistingImagePath = gpu.ImagePath,
            Status = gpu.Status
        };
    }

    public async Task<ServiceResult> UpdateAsync(Guid ownerId, GpuEditViewModel model)
    {
        var gpu = await _dbContext.Gpus.FirstOrDefaultAsync(g => g.Id == model.Id);
        if (gpu is null || gpu.OwnerId != ownerId)
        {
            return ServiceResult.Failed(CreateModelError(GpuConstants.Messages.ListingNotFound));
        }

        if (gpu.Status == GpuStatus.Rented)
        {
            return ServiceResult.Failed(CreateModelError(GpuConstants.Messages.CannotEditWhileRented));
        }

        var photoResult = await TrySavePhotoAsync(model.Photo);
        if (!photoResult.Succeeded)
        {
            return ServiceResult.Failed(photoResult.Errors.ToArray());
        }

        var previousImagePath = gpu.ImagePath;

        gpu.Name = model.Brand.Trim();
        gpu.Model = model.Model.Trim();
        gpu.VramGb = model.VramGb;
        gpu.CudaCores = model.CudaCores;
        gpu.PricePerHour = model.PricePerHour;
        gpu.MinRentalHours = model.MinRentalHours;
        gpu.Description = string.IsNullOrWhiteSpace(model.Description) ? null : model.Description.Trim();

        if (photoResult.RelativePath is not null)
        {
            gpu.ImagePath = photoResult.RelativePath;
        }

        try
        {
            await _dbContext.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            TryDeletePhysicalFile(photoResult.AbsolutePath);
            return ServiceResult.Failed(CreateModelError(GpuConstants.Messages.SaveFailed));
        }

        if (photoResult.RelativePath is not null && !string.IsNullOrWhiteSpace(previousImagePath))
        {
            TryDeletePhysicalFile(ResolveAbsolutePath(previousImagePath));
        }

        return ServiceResult.Success();
    }

    private async Task<PhotoSaveResult> TrySavePhotoAsync(IFormFile? photo)
    {
        if (photo is null || photo.Length == 0)
        {
            return PhotoSaveResult.NoPhoto();
        }

        if (photo.Length > GpuConstants.Photo.MaxBytes)
        {
            return PhotoSaveResult.Fail(CreateModelError(GpuConstants.Messages.PhotoTooLarge));
        }

        var extension = Path.GetExtension(photo.FileName);
        if (string.IsNullOrEmpty(extension) || !GpuConstants.Photo.AllowedExtensions.Contains(extension))
        {
            return PhotoSaveResult.Fail(CreateModelError(GpuConstants.Messages.PhotoUnsupportedFormat));
        }

        var fileName = $"{Guid.NewGuid():N}{extension.ToLowerInvariant()}";
        var directoryPath = Path.Combine(_environment.WebRootPath, "uploads", "gpus");
        Directory.CreateDirectory(directoryPath);
        var savedFullPath = Path.Combine(directoryPath, fileName);

        await using (var stream = new FileStream(savedFullPath, FileMode.Create))
        {
            await photo.CopyToAsync(stream);
        }

        var relativePath = $"/{GpuConstants.Photo.DirectorySegment}/{fileName}";
        return PhotoSaveResult.Saved(relativePath, savedFullPath);
    }

    private string? ResolveAbsolutePath(string? relativePath)
    {
        if (string.IsNullOrWhiteSpace(relativePath))
        {
            return null;
        }

        var trimmed = relativePath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
        return Path.Combine(_environment.WebRootPath, trimmed);
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

    private sealed class PhotoSaveResult
    {
        public bool Succeeded { get; private init; }
        public string? RelativePath { get; private init; }
        public string? AbsolutePath { get; private init; }
        public IReadOnlyList<ServiceError> Errors { get; private init; } = Array.Empty<ServiceError>();

        public static PhotoSaveResult NoPhoto() => new() { Succeeded = true };

        public static PhotoSaveResult Saved(string relativePath, string absolutePath) => new()
        {
            Succeeded = true,
            RelativePath = relativePath,
            AbsolutePath = absolutePath
        };

        public static PhotoSaveResult Fail(ServiceError error) => new()
        {
            Succeeded = false,
            Errors = new[] { error }
        };
    }
}

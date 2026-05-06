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

        string? imagePath = null;
        string? savedFullPath = null;

        if (model.Photo is { Length: > 0 })
        {
            if (model.Photo.Length > GpuConstants.Photo.MaxBytes)
            {
                return ServiceResult.Failed(CreateModelError(GpuConstants.Messages.PhotoTooLarge));
            }

            var extension = Path.GetExtension(model.Photo.FileName);
            if (string.IsNullOrEmpty(extension) || !GpuConstants.Photo.AllowedExtensions.Contains(extension))
            {
                return ServiceResult.Failed(CreateModelError(GpuConstants.Messages.PhotoUnsupportedFormat));
            }

            var fileName = $"{Guid.NewGuid():N}{extension.ToLowerInvariant()}";
            var directoryPath = Path.Combine(_environment.WebRootPath, "uploads", "gpus");
            Directory.CreateDirectory(directoryPath);
            savedFullPath = Path.Combine(directoryPath, fileName);

            await using (var stream = new FileStream(savedFullPath, FileMode.Create))
            {
                await model.Photo.CopyToAsync(stream);
            }

            imagePath = $"/{GpuConstants.Photo.DirectorySegment}/{fileName}";
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
            ImagePath = imagePath,
            Status = GpuStatus.Pending
        };

        _dbContext.Gpus.Add(gpu);

        try
        {
            await _dbContext.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            TryDeletePhysicalFile(savedFullPath);
            return ServiceResult.Failed(CreateModelError(GpuConstants.Messages.SaveFailed));
        }

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

using CloudCompute.Constants;
using CloudCompute.Data;
using CloudCompute.Models;
using CloudCompute.Models.Enums;
using CloudCompute.Services.Common;
using CloudCompute.Services.Notifications;
using CloudCompute.ViewModels.Rentals;
using CloudCompute.ViewModels.Reviews;
using Microsoft.EntityFrameworkCore;

namespace CloudCompute.Services.Reviews;

public class ReviewService : IReviewService
{
    private readonly AppDbContext _dbContext;
    private readonly INotificationService _notificationService;

    public ReviewService(AppDbContext dbContext, INotificationService notificationService)
    {
        _dbContext = dbContext;
        _notificationService = notificationService;
    }

    public Task<RentalReviewFormViewModel?> GetFormAsync(Guid renterId, Guid rentalId)
    {
        return _dbContext.Rentals
            .AsNoTracking()
            .Where(r => r.Id == rentalId
                        && r.RenterId == renterId
                        && r.Status == RentalStatus.Completed
                        && r.Review == null
                        && r.OwnerId != renterId)
            .Select(r => new RentalReviewFormViewModel
            {
                RentalId = r.Id,
                GpuId = r.GpuId,
                GpuName = r.Gpu != null ? r.Gpu.Name : string.Empty,
                GpuModel = r.Gpu != null ? r.Gpu.Model : string.Empty,
                ImagePath = r.Gpu != null ? r.Gpu.ImagePath : null,
                OwnerDisplayName = r.Owner != null ? (r.Owner.FirstName + " " + r.Owner.LastName) : "Unknown owner",
                ReferenceNumber = r.ReferenceNumber,
                Rating = 5
            })
            .FirstOrDefaultAsync();
    }

    public async Task<MyReviewsViewModel> GetMineAsync(Guid renterId)
    {
        var items = await _dbContext.Reviews
            .AsNoTracking()
            .Where(r => r.RenterId == renterId)
            .OrderByDescending(r => r.CreatedAt)
            .Select(r => new MyReviewItemViewModel
            {
                Id = r.Id,
                RentalId = r.RentalId,
                GpuId = r.GpuId,
                GpuName = r.Gpu != null ? r.Gpu.Name : string.Empty,
                GpuModel = r.Gpu != null ? r.Gpu.Model : string.Empty,
                ImagePath = r.Gpu != null ? r.Gpu.ImagePath : null,
                OwnerDisplayName = r.Gpu != null && r.Gpu.Owner != null
                    ? (r.Gpu.Owner.FirstName + " " + r.Gpu.Owner.LastName)
                    : "Unknown owner",
                ReferenceNumber = r.Rental != null ? r.Rental.ReferenceNumber : string.Empty,
                Rating = r.Rating,
                Comment = r.Comment,
                CreatedAt = r.CreatedAt
            })
            .ToListAsync();

        return new MyReviewsViewModel { Items = items };
    }

    public async Task<ServiceResult> CreateAsync(Guid renterId, RentalReviewFormViewModel form)
    {
        if (form.Rating is < 1 or > 5)
        {
            return Fail("Rating must be between 1 and 5 stars.");
        }

        if (form.Comment?.Length > 1000)
        {
            return Fail("Comment must be 1000 characters or fewer.");
        }

        await using var tx = await _dbContext.Database.BeginTransactionAsync();
        try
        {
            var rental = await _dbContext.Rentals
                .Include(r => r.Gpu)
                .Include(r => r.Renter)
                .Include(r => r.Review)
                .FirstOrDefaultAsync(r => r.Id == form.RentalId);

            if (rental is null || rental.RenterId != renterId)
            {
                await tx.RollbackAsync();
                return Fail("Rental could not be found.");
            }

            if (rental.Status != RentalStatus.Completed)
            {
                await tx.RollbackAsync();
                return Fail("Only completed rentals can be reviewed.");
            }

            if (rental.Review is not null)
            {
                await tx.RollbackAsync();
                return Fail("This rental has already been reviewed.");
            }

            if (rental.OwnerId == renterId)
            {
                await tx.RollbackAsync();
                return Fail("You cannot review your own listing.");
            }

            var review = new Review
            {
                RenterId = renterId,
                GpuId = rental.GpuId,
                RentalId = rental.Id,
                Rating = form.Rating,
                Comment = string.IsNullOrWhiteSpace(form.Comment) ? null : form.Comment.Trim(),
                CreatedAt = DateTime.UtcNow
            };

            _dbContext.Reviews.Add(review);
            await _dbContext.SaveChangesAsync();

            var averageRating = await _dbContext.Reviews
                .Where(r => r.GpuId == rental.GpuId)
                .AverageAsync(r => (decimal)r.Rating);

            if (rental.Gpu is not null)
            {
                rental.Gpu.AverageRating = decimal.Round(averageRating, 2, MidpointRounding.AwayFromZero);
            }

            _notificationService.Create(
                rental.OwnerId,
                NotificationType.ReviewReceived,
                $"{rental.Renter?.FullName ?? "A renter"} reviewed {rental.Gpu?.Model ?? "your GPU"} with {form.Rating} star(s).",
                $"/gpus/{rental.GpuId}");

            await _dbContext.SaveChangesAsync();
            await tx.CommitAsync();
            return ServiceResult.Success();
        }
        catch (DbUpdateException)
        {
            await tx.RollbackAsync();
            return Fail("We couldn't submit the review. Please try again.");
        }
    }

    private static ServiceResult Fail(string message)
    {
        return ServiceResult.Failed(new ServiceError(AuthConstants.Validation.ModelErrorKey, message));
    }
}

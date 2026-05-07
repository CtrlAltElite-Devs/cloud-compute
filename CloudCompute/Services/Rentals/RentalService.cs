using CloudCompute.Constants;
using CloudCompute.Data;
using CloudCompute.Models;
using CloudCompute.Models.Enums;
using CloudCompute.Services.Common;
using CloudCompute.Services.Notifications;
using CloudCompute.ViewModels.Rentals;
using Microsoft.EntityFrameworkCore;

namespace CloudCompute.Services.Rentals;

public class RentalService : IRentalService
{
    private const int MaxRentalHours = 168;
    private const decimal PlatformFeeRate = 0.10m;

    private readonly AppDbContext _dbContext;
    private readonly INotificationService _notificationService;

    public RentalService(AppDbContext dbContext, INotificationService notificationService)
    {
        _dbContext = dbContext;
        _notificationService = notificationService;
    }

    public async Task<RentalConfirmViewModel?> GetConfirmationAsync(Guid renterId, Guid gpuId, int? durationHours = null)
    {
        var gpu = await _dbContext.Gpus
            .AsNoTracking()
            .Include(g => g.Owner)
            .FirstOrDefaultAsync(g => g.Id == gpuId && g.Status == GpuStatus.Available);

        if (gpu is null)
        {
            return null;
        }

        var balance = await _dbContext.Users
            .AsNoTracking()
            .Where(u => u.Id == renterId)
            .Select(u => (decimal?)u.CreditBalance)
            .FirstOrDefaultAsync();

        if (balance is null)
        {
            return null;
        }

        var hours = durationHours ?? gpu.MinRentalHours;
        hours = Math.Clamp(hours, 1, MaxRentalHours);

        return new RentalConfirmViewModel
        {
            GpuId = gpu.Id,
            GpuName = gpu.Name,
            GpuModel = gpu.Model,
            ImagePath = gpu.ImagePath,
            OwnerDisplayName = gpu.Owner is not null ? gpu.Owner.FullName : "Unknown owner",
            PricePerHour = gpu.PricePerHour,
            MinRentalHours = gpu.MinRentalHours,
            CurrentBalance = balance.Value,
            DurationHours = hours,
            StartTime = DateTime.UtcNow
        };
    }

    public async Task<RentalCreateResult> CreateAsync(Guid renterId, Guid gpuId, int durationHours)
    {
        await using var tx = await _dbContext.Database.BeginTransactionAsync();
        try
        {
            var renter = await _dbContext.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == renterId);
            if (renter is null)
            {
                return Fail("Account could not be found.");
            }

            var gpu = await _dbContext.Gpus
                .AsNoTracking()
                .Include(g => g.Owner)
                .FirstOrDefaultAsync(g => g.Id == gpuId);

            if (gpu is null)
            {
                return Fail("GPU listing could not be found.");
            }

            var validation = Validate(renterId, renter.CreditBalance, gpu, durationHours);
            if (!validation.Succeeded)
            {
                return RentalCreateResult.FromServiceResult(validation);
            }

            var reservedCount = await _dbContext.Gpus
                .Where(g => g.Id == gpuId && g.Status == GpuStatus.Available && g.OwnerId != renterId)
                .ExecuteUpdateAsync(setters => setters.SetProperty(g => g.Status, GpuStatus.Rented));

            if (reservedCount == 0)
            {
                return Fail("This GPU is no longer available.");
            }

            var now = DateTime.UtcNow;
            var totalCost = gpu.PricePerHour * durationHours;
            var platformFee = decimal.Round(totalCost * PlatformFeeRate, 2, MidpointRounding.AwayFromZero);
            var ownerEarnings = totalCost - platformFee;

            var renterDebitCount = await _dbContext.Users
                .Where(u => u.Id == renterId && u.CreditBalance >= totalCost)
                .ExecuteUpdateAsync(setters => setters.SetProperty(u => u.CreditBalance, u => u.CreditBalance - totalCost));

            if (renterDebitCount == 0)
            {
                return Fail("Your credit balance is not enough for this rental.");
            }

            var renterBalanceAfter = await _dbContext.Users
                .AsNoTracking()
                .Where(u => u.Id == renterId)
                .Select(u => u.CreditBalance)
                .FirstAsync();

            var ownerCreditCount = await _dbContext.Users
                .Where(u => u.Id == gpu.OwnerId)
                .ExecuteUpdateAsync(setters => setters.SetProperty(u => u.CreditBalance, u => u.CreditBalance + ownerEarnings));

            if (ownerCreditCount == 0)
            {
                return Fail("GPU owner account could not be found.");
            }

            var ownerBalanceAfter = await _dbContext.Users
                .AsNoTracking()
                .Where(u => u.Id == gpu.OwnerId)
                .Select(u => u.CreditBalance)
                .FirstAsync();

            var rental = new Rental
            {
                ReferenceNumber = GenerateReferenceNumber(now),
                RenterId = renterId,
                GpuId = gpu.Id,
                OwnerId = gpu.OwnerId,
                StartTime = now,
                EndTime = now.AddHours(durationHours),
                DurationHours = durationHours,
                PricePerHour = gpu.PricePerHour,
                TotalCost = totalCost,
                PlatformFee = platformFee,
                OwnerEarnings = ownerEarnings,
                Status = RentalStatus.Active,
                CreatedAt = now
            };

            _dbContext.Rentals.Add(rental);

            _dbContext.CreditTransactions.Add(new CreditTransaction
            {
                UserId = renter.Id,
                Type = CreditTransactionType.RentalCharge,
                Amount = -totalCost,
                BalanceAfter = renterBalanceAfter,
                RelatedRentalId = rental.Id,
                Reason = $"Rental charge for {gpu.Model}"
            });

            _dbContext.CreditTransactions.Add(new CreditTransaction
            {
                UserId = gpu.OwnerId,
                Type = CreditTransactionType.RentalEarning,
                Amount = ownerEarnings,
                BalanceAfter = ownerBalanceAfter,
                RelatedRentalId = rental.Id,
                Reason = $"Rental earning for {gpu.Model} after 10% platform fee"
            });

            _notificationService.Create(
                renter.Id,
                NotificationType.RentalConfirmed,
                $"Rental {rental.ReferenceNumber} confirmed for {gpu.Model}.",
                "/rentals/active");

            _notificationService.Create(
                gpu.OwnerId,
                NotificationType.RentalConfirmed,
                $"{renter.FullName} rented {gpu.Model} for {durationHours} hour(s).",
                "/gpus/mine");

            await _dbContext.SaveChangesAsync();
            await tx.CommitAsync();
            return RentalCreateResult.Success(rental.Id);
        }
        catch (DbUpdateException)
        {
            await tx.RollbackAsync();
            return Fail("We couldn't create the rental. Please try again.");
        }
    }

    public async Task<ActiveRentalsViewModel> GetActiveAsync(Guid renterId)
    {
        var items = await _dbContext.Rentals
            .AsNoTracking()
            .Where(r => r.RenterId == renterId && r.Status == RentalStatus.Active)
            .OrderBy(r => r.EndTime)
            .Select(r => new ActiveRentalItemViewModel
            {
                Id = r.Id,
                ReferenceNumber = r.ReferenceNumber,
                GpuName = r.Gpu != null ? r.Gpu.Name : string.Empty,
                GpuModel = r.Gpu != null ? r.Gpu.Model : string.Empty,
                ImagePath = r.Gpu != null ? r.Gpu.ImagePath : null,
                OwnerDisplayName = r.Owner != null ? (r.Owner.FirstName + " " + r.Owner.LastName) : "Unknown owner",
                StartTime = r.StartTime,
                EndTime = r.EndTime,
                DurationHours = r.DurationHours,
                TotalCost = r.TotalCost
            })
            .ToListAsync();

        return new ActiveRentalsViewModel { Items = items };
    }

    private static ServiceResult Validate(Guid renterId, decimal balance, CloudCompute.Models.Gpu gpu, int durationHours)
    {
        if (gpu.Status != GpuStatus.Available)
        {
            return ServiceResult.Failed(ModelError("This GPU is no longer available."));
        }

        if (gpu.OwnerId == renterId)
        {
            return ServiceResult.Failed(ModelError("You cannot rent your own GPU."));
        }

        if (durationHours < gpu.MinRentalHours || durationHours > MaxRentalHours)
        {
            return ServiceResult.Failed(ModelError($"Duration must be between {gpu.MinRentalHours} and {MaxRentalHours} hours."));
        }

        var totalCost = gpu.PricePerHour * durationHours;
        if (balance < totalCost)
        {
            return ServiceResult.Failed(ModelError("Your credit balance is not enough for this rental."));
        }

        return ServiceResult.Success();
    }

    private static RentalCreateResult Fail(string message)
    {
        return RentalCreateResult.Failed(ModelError(message));
    }

    private static ServiceError ModelError(string message)
    {
        return new ServiceError(AuthConstants.Validation.ModelErrorKey, message);
    }

    private static string GenerateReferenceNumber(DateTime now)
    {
        return $"RNT-{now:yyyyMMddHHmmss}-{Random.Shared.Next(1000, 10000)}";
    }
}

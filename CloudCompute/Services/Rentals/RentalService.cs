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
    private readonly IRentalLifecycleService _rentalLifecycleService;

    public RentalService(
        AppDbContext dbContext,
        INotificationService notificationService,
        IRentalLifecycleService rentalLifecycleService)
    {
        _dbContext = dbContext;
        _notificationService = notificationService;
        _rentalLifecycleService = rentalLifecycleService;
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

            if (!renter.IsActive)
            {
                return Fail(AuthConstants.Messages.SuspendedAccountAction);
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

    public async Task<ServiceResult> TerminateAsync(Guid renterId, Guid rentalId)
    {
        await using var tx = await _dbContext.Database.BeginTransactionAsync();
        try
        {
            var rental = await _dbContext.Rentals
                .Include(r => r.Gpu)
                .Include(r => r.Renter)
                .FirstOrDefaultAsync(r => r.Id == rentalId && r.RenterId == renterId && r.Status == RentalStatus.Active);

            if (rental is null)
            {
                await tx.RollbackAsync();
                return ServiceResult.Failed(ModelError("Active rental could not be found."));
            }

            var now = DateTime.UtcNow;
            var originalDurationHours = rental.DurationHours;

            // Prorate by whole hours used (rounded up) to give renters a fair share back
            // when they terminate before the rental window ends.
            var elapsedHoursRaw = (now - rental.StartTime).TotalHours;
            var usedHours = (int)Math.Ceiling(Math.Max(elapsedHoursRaw, 0));
            usedHours = Math.Clamp(usedHours, 0, originalDurationHours);

            var newTotalCost = decimal.Round(rental.PricePerHour * usedHours, 2, MidpointRounding.AwayFromZero);
            var newPlatformFee = decimal.Round(newTotalCost * PlatformFeeRate, 2, MidpointRounding.AwayFromZero);
            var newOwnerEarnings = newTotalCost - newPlatformFee;

            var renterRefund = rental.TotalCost - newTotalCost;
            if (renterRefund < 0)
            {
                renterRefund = 0;
            }

            var ownerClawback = rental.OwnerEarnings - newOwnerEarnings;
            if (ownerClawback < 0)
            {
                ownerClawback = 0;
            }

            decimal renterBalanceAfter = 0;
            if (renterRefund > 0)
            {
                await _dbContext.Users
                    .Where(u => u.Id == rental.RenterId)
                    .ExecuteUpdateAsync(setters => setters.SetProperty(u => u.CreditBalance, u => u.CreditBalance + renterRefund));

                renterBalanceAfter = await _dbContext.Users
                    .AsNoTracking()
                    .Where(u => u.Id == rental.RenterId)
                    .Select(u => u.CreditBalance)
                    .FirstAsync();
            }

            decimal ownerBalanceAfter = 0;
            if (ownerClawback > 0)
            {
                await _dbContext.Users
                    .Where(u => u.Id == rental.OwnerId)
                    .ExecuteUpdateAsync(setters => setters.SetProperty(u => u.CreditBalance, u => u.CreditBalance - ownerClawback));

                ownerBalanceAfter = await _dbContext.Users
                    .AsNoTracking()
                    .Where(u => u.Id == rental.OwnerId)
                    .Select(u => u.CreditBalance)
                    .FirstAsync();
            }

            rental.Status = RentalStatus.Terminated;
            rental.TerminatedEarly = true;
            rental.EndTime = now;
            rental.DurationHours = Math.Max(usedHours, 0);
            rental.TotalCost = newTotalCost;
            rental.PlatformFee = newPlatformFee;
            rental.OwnerEarnings = newOwnerEarnings;

            if (rental.Gpu is not null)
            {
                rental.Gpu.Status = GpuStatus.Available;
            }

            if (renterRefund > 0)
            {
                _dbContext.CreditTransactions.Add(new CreditTransaction
                {
                    UserId = rental.RenterId,
                    Type = CreditTransactionType.Refund,
                    Amount = renterRefund,
                    BalanceAfter = renterBalanceAfter,
                    RelatedRentalId = rental.Id,
                    Reason = $"Refund for early termination of rental {rental.ReferenceNumber} ({usedHours} of {originalDurationHours} hour(s) used)"
                });
            }

            if (ownerClawback > 0)
            {
                _dbContext.CreditTransactions.Add(new CreditTransaction
                {
                    UserId = rental.OwnerId,
                    Type = CreditTransactionType.Revoke,
                    Amount = -ownerClawback,
                    BalanceAfter = ownerBalanceAfter,
                    RelatedRentalId = rental.Id,
                    Reason = $"Owner earnings adjusted for early termination of rental {rental.ReferenceNumber}"
                });
            }

            _notificationService.Create(
                rental.RenterId,
                NotificationType.RentalTerminated,
                renterRefund > 0
                    ? $"Rental {rental.ReferenceNumber} was terminated. {renterRefund:N2} credits refunded."
                    : $"Rental {rental.ReferenceNumber} was terminated.",
                "/rentals/history");

            _notificationService.Create(
                rental.OwnerId,
                NotificationType.RentalTerminated,
                $"{rental.Renter?.FullName ?? "A renter"} terminated rental {rental.ReferenceNumber}.",
                "/gpus/mine");

            await _dbContext.SaveChangesAsync();
            await tx.CommitAsync();
            return ServiceResult.Success();
        }
        catch (DbUpdateException)
        {
            await tx.RollbackAsync();
            return ServiceResult.Failed(ModelError("We couldn't terminate the rental. Please try again."));
        }
    }

    public async Task<ServiceResult> ExtendAsync(Guid renterId, Guid rentalId, int additionalHours)
    {
        if (additionalHours < 1)
        {
            return ServiceResult.Failed(ModelError("Extension must be at least 1 hour."));
        }

        await using var tx = await _dbContext.Database.BeginTransactionAsync();
        try
        {
            var rental = await _dbContext.Rentals
                .Include(r => r.Gpu)
                .Include(r => r.Renter)
                .FirstOrDefaultAsync(r => r.Id == rentalId && r.RenterId == renterId && r.Status == RentalStatus.Active);

            if (rental is null)
            {
                await tx.RollbackAsync();
                return ServiceResult.Failed(ModelError("Active rental could not be found."));
            }

            var newDuration = rental.DurationHours + additionalHours;
            if (newDuration > MaxRentalHours)
            {
                await tx.RollbackAsync();
                return ServiceResult.Failed(ModelError($"Total rental duration cannot exceed {MaxRentalHours} hours."));
            }

            var extraCost = decimal.Round(rental.PricePerHour * additionalHours, 2, MidpointRounding.AwayFromZero);
            var extraPlatformFee = decimal.Round(extraCost * PlatformFeeRate, 2, MidpointRounding.AwayFromZero);
            var extraOwnerEarnings = extraCost - extraPlatformFee;

            var renterDebitCount = await _dbContext.Users
                .Where(u => u.Id == renterId && u.CreditBalance >= extraCost)
                .ExecuteUpdateAsync(setters => setters.SetProperty(u => u.CreditBalance, u => u.CreditBalance - extraCost));

            if (renterDebitCount == 0)
            {
                await tx.RollbackAsync();
                return ServiceResult.Failed(ModelError("Your credit balance is not enough to extend this rental."));
            }

            var renterBalanceAfter = await _dbContext.Users
                .AsNoTracking()
                .Where(u => u.Id == renterId)
                .Select(u => u.CreditBalance)
                .FirstAsync();

            await _dbContext.Users
                .Where(u => u.Id == rental.OwnerId)
                .ExecuteUpdateAsync(setters => setters.SetProperty(u => u.CreditBalance, u => u.CreditBalance + extraOwnerEarnings));

            var ownerBalanceAfter = await _dbContext.Users
                .AsNoTracking()
                .Where(u => u.Id == rental.OwnerId)
                .Select(u => u.CreditBalance)
                .FirstAsync();

            rental.DurationHours = newDuration;
            rental.EndTime = rental.StartTime.AddHours(newDuration);
            rental.TotalCost += extraCost;
            rental.PlatformFee += extraPlatformFee;
            rental.OwnerEarnings += extraOwnerEarnings;
            rental.ExpiryNotifiedAt = null;

            var gpuModel = rental.Gpu?.Model ?? "GPU";

            _dbContext.CreditTransactions.Add(new CreditTransaction
            {
                UserId = rental.RenterId,
                Type = CreditTransactionType.RentalCharge,
                Amount = -extraCost,
                BalanceAfter = renterBalanceAfter,
                RelatedRentalId = rental.Id,
                Reason = $"Rental extension ({additionalHours} hr) for {gpuModel}"
            });

            _dbContext.CreditTransactions.Add(new CreditTransaction
            {
                UserId = rental.OwnerId,
                Type = CreditTransactionType.RentalEarning,
                Amount = extraOwnerEarnings,
                BalanceAfter = ownerBalanceAfter,
                RelatedRentalId = rental.Id,
                Reason = $"Rental extension earning ({additionalHours} hr) for {gpuModel} after 10% platform fee"
            });

            _notificationService.Create(
                rental.RenterId,
                NotificationType.RentalConfirmed,
                $"Rental {rental.ReferenceNumber} extended by {additionalHours} hour(s).",
                "/rentals/active");

            _notificationService.Create(
                rental.OwnerId,
                NotificationType.RentalConfirmed,
                $"{rental.Renter?.FullName ?? "A renter"} extended rental {rental.ReferenceNumber} by {additionalHours} hour(s).",
                "/gpus/mine");

            await _dbContext.SaveChangesAsync();
            await tx.CommitAsync();
            return ServiceResult.Success();
        }
        catch (DbUpdateException)
        {
            await tx.RollbackAsync();
            return ServiceResult.Failed(ModelError("We couldn't extend the rental. Please try again."));
        }
    }

    public async Task<ActiveRentalsViewModel> GetActiveAsync(Guid renterId)
    {
        await _rentalLifecycleService.CompleteExpiredActiveRentalsAsync();

        var balance = await _dbContext.Users
            .AsNoTracking()
            .Where(u => u.Id == renterId)
            .Select(u => (decimal?)u.CreditBalance)
            .FirstOrDefaultAsync() ?? 0m;

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
                PricePerHour = r.PricePerHour,
                TotalCost = r.TotalCost
            })
            .ToListAsync();

        return new ActiveRentalsViewModel { Items = items, CurrentBalance = balance };
    }

    public async Task<RentalHistoryViewModel> GetHistoryAsync(Guid renterId, RentalHistoryFilterViewModel filter)
    {
        await _rentalLifecycleService.CompleteExpiredActiveRentalsAsync();

        filter = new RentalHistoryFilterViewModel
        {
            Search = filter?.Search?.Trim(),
            Status = filter?.Status,
            GpuModel = string.IsNullOrWhiteSpace(filter?.GpuModel) ? null : filter.GpuModel.Trim(),
            DateFrom = filter?.DateFrom,
            DateTo = filter?.DateTo
        };

        var baseQuery = _dbContext.Rentals
            .AsNoTracking()
            .Where(r => r.RenterId == renterId);

        var hasAnyRentals = await baseQuery.AnyAsync();

        var availableGpuModels = await baseQuery
            .Where(r => r.Gpu != null)
            .Select(r => r.Gpu!.Model)
            .Distinct()
            .OrderBy(m => m)
            .ToListAsync();

        var query = baseQuery;

        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var search = filter.Search;
            query = query.Where(r =>
                (r.Gpu != null && EF.Functions.Like(r.Gpu.Model, $"%{search}%")) ||
                EF.Functions.Like(r.ReferenceNumber, $"%{search}%"));
        }

        if (filter.Status.HasValue)
        {
            query = query.Where(r => r.Status == filter.Status.Value);
        }

        if (!string.IsNullOrWhiteSpace(filter.GpuModel))
        {
            var modelName = filter.GpuModel;
            query = query.Where(r => r.Gpu != null && r.Gpu.Model == modelName);
        }

        if (filter.DateFrom.HasValue)
        {
            var fromUtc = DateTime.SpecifyKind(filter.DateFrom.Value.Date, DateTimeKind.Utc);
            query = query.Where(r => r.StartTime >= fromUtc);
        }

        if (filter.DateTo.HasValue)
        {
            var toUtc = DateTime.SpecifyKind(filter.DateTo.Value.Date.AddDays(1), DateTimeKind.Utc);
            query = query.Where(r => r.StartTime < toUtc);
        }

        var items = await query
            .OrderByDescending(r => r.StartTime)
            .Select(r => new RentalHistoryItemViewModel
            {
                Id = r.Id,
                ReferenceNumber = r.ReferenceNumber,
                GpuName = r.Gpu != null ? r.Gpu.Name : string.Empty,
                GpuModel = r.Gpu != null ? r.Gpu.Model : string.Empty,
                ImagePath = r.Gpu != null ? r.Gpu.ImagePath : null,
                OwnerDisplayName = r.Owner != null ? (r.Owner.FirstName + " " + r.Owner.LastName) : "Unknown owner",
                Status = r.Status,
                StartTime = r.StartTime,
                EndTime = r.EndTime,
                DurationHours = r.DurationHours,
                PricePerHour = r.PricePerHour,
                TotalCost = r.TotalCost,
                HasReview = r.Review != null
            })
            .ToListAsync();

        return new RentalHistoryViewModel
        {
            Filter = filter,
            Items = items,
            AvailableGpuModels = availableGpuModels,
            HasAnyRentals = hasAnyRentals
        };
    }

    public async Task<RentalReceiptViewModel?> GetReceiptAsync(Guid renterId, Guid rentalId)
    {
        await _rentalLifecycleService.CompleteExpiredActiveRentalsAsync();

        return await _dbContext.Rentals
            .AsNoTracking()
            .Where(r => r.Id == rentalId && r.RenterId == renterId)
            .Select(r => new RentalReceiptViewModel
            {
                Id = r.Id,
                ReferenceNumber = r.ReferenceNumber,
                GpuName = r.Gpu != null ? r.Gpu.Name : string.Empty,
                GpuModel = r.Gpu != null ? r.Gpu.Model : string.Empty,
                OwnerDisplayName = r.Owner != null ? (r.Owner.FirstName + " " + r.Owner.LastName) : "Unknown owner",
                Status = r.Status,
                StartTime = r.StartTime,
                EndTime = r.EndTime,
                DurationHours = r.DurationHours,
                PricePerHour = r.PricePerHour,
                TotalCost = r.TotalCost,
                PlatformFee = r.PlatformFee,
                OwnerEarnings = r.OwnerEarnings
            })
            .FirstOrDefaultAsync();
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

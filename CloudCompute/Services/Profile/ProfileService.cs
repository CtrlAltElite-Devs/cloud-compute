using System.Security.Claims;
using CloudCompute.Constants;
using CloudCompute.Data;
using CloudCompute.Models;
using CloudCompute.Models.Enums;
using CloudCompute.Services.Common;
using CloudCompute.ViewModels.Profile;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace CloudCompute.Services.Profile;

public class ProfileService : IProfileService
{
    private static readonly HashSet<string> AllowedAvatarExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg",
        ".jpeg",
        ".png",
        ".webp"
    };

    private const string AvatarDirectorySegment = "uploads/profiles";

    private readonly AppDbContext _dbContext;
    private readonly IPasswordHasher<ApplicationUser> _passwordHasher;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IWebHostEnvironment _environment;

    public ProfileService(
        AppDbContext dbContext,
        IPasswordHasher<ApplicationUser> passwordHasher,
        IHttpContextAccessor httpContextAccessor,
        IWebHostEnvironment environment)
    {
        _dbContext = dbContext;
        _passwordHasher = passwordHasher;
        _httpContextAccessor = httpContextAccessor;
        _environment = environment;
    }

    public async Task<ServiceResult> UpdateProfileAsync(Guid userId, ProfileEditViewModel model)
    {
        var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Id == userId);
        if (user is null)
        {
            return ServiceResult.Failed(CreateModelError(AuthConstants.Profile.UserNotFound));
        }

        var firstName = model.FirstName.Trim();
        var lastName = model.LastName.Trim();
        var userName = model.UserName.Trim();
        var email = model.Email.Trim();
        var bio = string.IsNullOrWhiteSpace(model.Bio) ? null : model.Bio.Trim();

        var normalizedEmail = email.ToUpperInvariant();
        var normalizedUserName = userName.ToUpperInvariant();
        var errors = new List<ServiceError>();

        var duplicateEmail = await _dbContext.Users
            .AnyAsync(u => u.Id != userId && u.Email.ToUpper() == normalizedEmail);
        if (duplicateEmail)
        {
            errors.Add(new ServiceError(nameof(ProfileEditViewModel.Email), AuthConstants.Profile.DuplicateEmail));
        }

        var duplicateUserName = await _dbContext.Users
            .AnyAsync(u => u.Id != userId && u.UserName.ToUpper() == normalizedUserName);
        if (duplicateUserName)
        {
            errors.Add(new ServiceError(nameof(ProfileEditViewModel.UserName), AuthConstants.Profile.DuplicateUserName));
        }

        if (errors.Count > 0)
        {
            return ServiceResult.Failed(errors.ToArray());
        }

        user.FirstName = firstName;
        user.LastName = lastName;
        user.UserName = userName;
        user.Email = email;
        user.Bio = bio;

        try
        {
            await _dbContext.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            return ServiceResult.Failed(CreateModelError(AuthConstants.Messages.DuplicateAccount));
        }

        await RefreshSignInAsync(user);

        return ServiceResult.Success();
    }

    public async Task<ServiceResult> ChangePasswordAsync(Guid userId, ChangePasswordViewModel model)
    {
        var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Id == userId);
        if (user is null)
        {
            return ServiceResult.Failed(CreateModelError(AuthConstants.Profile.UserNotFound));
        }

        var verifyResult = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, model.CurrentPassword);
        if (verifyResult == PasswordVerificationResult.Failed)
        {
            return ServiceResult.Failed(new ServiceError(
                nameof(ChangePasswordViewModel.CurrentPassword),
                AuthConstants.Profile.IncorrectCurrentPassword));
        }

        user.PasswordHash = _passwordHasher.HashPassword(user, model.NewPassword);
        await _dbContext.SaveChangesAsync();

        return ServiceResult.Success();
    }

    public async Task<ServiceResult> UpdateAvatarAsync(Guid userId, IFormFile? file)
    {
        if (file is null || file.Length == 0)
        {
            return ServiceResult.Failed(CreateModelError(AuthConstants.Profile.AvatarRequired));
        }

        if (file.Length > AuthConstants.Profile.AvatarMaxBytes)
        {
            return ServiceResult.Failed(CreateModelError(AuthConstants.Profile.AvatarTooLarge));
        }

        var extension = Path.GetExtension(file.FileName);
        if (string.IsNullOrEmpty(extension) || !AllowedAvatarExtensions.Contains(extension))
        {
            return ServiceResult.Failed(CreateModelError(AuthConstants.Profile.AvatarUnsupportedFormat));
        }

        var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Id == userId);
        if (user is null)
        {
            return ServiceResult.Failed(CreateModelError(AuthConstants.Profile.UserNotFound));
        }

        var fileName = $"{Guid.NewGuid():N}{extension.ToLowerInvariant()}";
        var directoryPath = Path.Combine(_environment.WebRootPath, "uploads", "profiles");
        Directory.CreateDirectory(directoryPath);
        var fullPath = Path.Combine(directoryPath, fileName);

        await using (var stream = new FileStream(fullPath, FileMode.Create))
        {
            await file.CopyToAsync(stream);
        }

        var previousPath = user.ProfilePicturePath;
        user.ProfilePicturePath = $"/{AvatarDirectorySegment}/{fileName}";

        try
        {
            await _dbContext.SaveChangesAsync();
        }
        catch
        {
            TryDeletePhysicalFile(fullPath);
            throw;
        }

        if (!string.IsNullOrWhiteSpace(previousPath))
        {
            TryDeletePhysicalFile(MapToPhysicalPath(previousPath));
        }

        await RefreshSignInAsync(user);

        return ServiceResult.Success();
    }

    public async Task<ServiceResult> DeleteAccountAsync(Guid userId, DeleteAccountViewModel model)
    {
        var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Id == userId);
        if (user is null)
        {
            return ServiceResult.Failed(CreateModelError(AuthConstants.Profile.UserNotFound));
        }

        if (string.IsNullOrEmpty(user.PasswordHash) ||
            _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, model.CurrentPassword) == PasswordVerificationResult.Failed)
        {
            return ServiceResult.Failed(new ServiceError(
                nameof(DeleteAccountViewModel.CurrentPassword),
                AuthConstants.Profile.IncorrectCurrentPassword));
        }

        var hasActiveRentals = await _dbContext.Rentals
            .AnyAsync(r => r.RenterId == userId && r.Status == RentalStatus.Active);
        if (hasActiveRentals)
        {
            return ServiceResult.Failed(CreateModelError(AuthConstants.Profile.AccountDeleteHasActiveRentals));
        }

        await using var tx = await _dbContext.Database.BeginTransactionAsync();
        try
        {
            await _dbContext.Gpus
                .Where(g => g.OwnerId == userId &&
                            (g.Status == GpuStatus.Available || g.Status == GpuStatus.Pending))
                .ExecuteUpdateAsync(setters => setters.SetProperty(g => g.Status, GpuStatus.Maintenance));

            var anonymizationSuffix = Guid.NewGuid().ToString("N").Substring(0, 12);
            var previousAvatarPath = user.ProfilePicturePath;

            user.IsActive = false;
            user.IsOwnerVerified = false;
            user.FirstName = "Deleted";
            user.LastName = "User";
            user.UserName = $"deleted-{anonymizationSuffix}";
            user.Email = $"deleted-{anonymizationSuffix}@deleted.local";
            user.Bio = null;
            user.ProfilePicturePath = null;
            user.PasswordHash = string.Empty;

            await _dbContext.SaveChangesAsync();
            await tx.CommitAsync();

            if (!string.IsNullOrWhiteSpace(previousAvatarPath))
            {
                TryDeletePhysicalFile(MapToPhysicalPath(previousAvatarPath));
            }
        }
        catch (DbUpdateException)
        {
            await tx.RollbackAsync();
            return ServiceResult.Failed(CreateModelError(AuthConstants.Messages.DuplicateAccount));
        }

        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

        return ServiceResult.Success();
    }

    private async Task RefreshSignInAsync(ApplicationUser user)
    {
        var httpContext = HttpContext;
        var authResult = await httpContext.AuthenticateAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        var properties = authResult.Properties ?? new AuthenticationProperties();

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.UserName),
            new(ClaimTypes.Email, user.Email),
            new(ClaimTypes.Role, user.Role.ToString()),
            new(AuthConstants.Claims.FullName, user.FullName),
            new(AuthConstants.Claims.ProfilePicturePath, user.ProfilePicturePath ?? string.Empty)
        };

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);

        await httpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal, properties);
    }

    private string? MapToPhysicalPath(string publicPath)
    {
        if (string.IsNullOrWhiteSpace(publicPath) || !publicPath.StartsWith('/'))
        {
            return null;
        }

        var relative = publicPath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
        return Path.Combine(_environment.WebRootPath, relative);
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

    private HttpContext HttpContext =>
        _httpContextAccessor.HttpContext
        ?? throw new InvalidOperationException(AuthConstants.Diagnostics.MissingHttpContext);

    private static ServiceError CreateModelError(string message)
    {
        return new ServiceError(AuthConstants.Validation.ModelErrorKey, message);
    }
}

using System.Security.Claims;
using CloudCompute.Constants;
using CloudCompute.Data;
using CloudCompute.Models;
using CloudCompute.Models.Enums;
using CloudCompute.Services.Common;
using CloudCompute.ViewModels.Auth;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace CloudCompute.Services.Auth;

public class AuthService : IAuthService
{
    private readonly AppDbContext _dbContext;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IPasswordHasher<ApplicationUser> _passwordHasher;

    public AuthService(
        AppDbContext dbContext,
        IHttpContextAccessor httpContextAccessor,
        IPasswordHasher<ApplicationUser> passwordHasher)
    {
        _dbContext = dbContext;
        _httpContextAccessor = httpContextAccessor;
        _passwordHasher = passwordHasher;
    }

    public async Task<ServiceResult> LoginAsync(LoginViewModel model)
    {
        return await LoginForRoleAsync(model, UserRole.Member, AuthConstants.Messages.InvalidCredentials);
    }

    public async Task<ServiceResult> AdminLoginAsync(LoginViewModel model)
    {
        return await LoginForRoleAsync(model, UserRole.Admin, AuthConstants.Messages.InvalidAdminCredentials);
    }

    public async Task<ServiceResult> SignupAsync(SignupViewModel model)
    {
        var email = NormalizeEmail(model.Email);
        var userName = model.UserName.Trim();
        var normalizedEmail = email.ToUpperInvariant();
        var normalizedUserName = userName.ToUpperInvariant();
        var errors = new List<ServiceError>();

        var duplicateEmail = await _dbContext.Users
            .AnyAsync(user => user.Email.ToUpper() == normalizedEmail);
        if (duplicateEmail)
        {
            errors.Add(new ServiceError(nameof(SignupViewModel.Email), AuthConstants.Messages.DuplicateEmail));
        }

        var duplicateUserName = await _dbContext.Users
            .AnyAsync(user => user.UserName.ToUpper() == normalizedUserName);
        if (duplicateUserName)
        {
            errors.Add(new ServiceError(nameof(SignupViewModel.UserName), AuthConstants.Messages.DuplicateUserName));
        }

        if (errors.Count > 0)
        {
            return ServiceResult.Failed(errors.ToArray());
        }

        var user = new ApplicationUser
        {
            FullName = model.FullName.Trim(),
            UserName = userName,
            Email = email,
            IsActive = true,
            Role = UserRole.Member
        };

        user.PasswordHash = _passwordHasher.HashPassword(user, model.Password);

        _dbContext.Users.Add(user);

        try
        {
            await _dbContext.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            return ServiceResult.Failed(CreateModelError(AuthConstants.Messages.DuplicateAccount));
        }

        return ServiceResult.Success();
    }

    public async Task LogoutAsync()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    }

    private async Task<ServiceResult> LoginForRoleAsync(
        LoginViewModel model,
        UserRole requiredRole,
        string invalidCredentialsMessage)
    {
        var loginIdentifier = model.LoginIdentifier.Trim();
        var normalizedLoginIdentifier = loginIdentifier.ToUpperInvariant();
        var user = await _dbContext.Users
            .FirstOrDefaultAsync(user =>
                user.Email.ToUpper() == normalizedLoginIdentifier ||
                user.UserName.ToUpper() == normalizedLoginIdentifier);

        if (user is null || user.Role != requiredRole)
        {
            return ServiceResult.Failed(CreateModelError(invalidCredentialsMessage));
        }

        if (!user.IsActive)
        {
            return ServiceResult.Failed(CreateModelError(AuthConstants.Messages.InactiveAccount));
        }

        if (string.IsNullOrWhiteSpace(user.PasswordHash))
        {
            return ServiceResult.Failed(CreateModelError(invalidCredentialsMessage));
        }

        var passwordResult = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, model.Password);
        if (passwordResult == PasswordVerificationResult.Failed)
        {
            return ServiceResult.Failed(CreateModelError(invalidCredentialsMessage));
        }

        await SignInUserAsync(user, model.RememberMe);

        return ServiceResult.Success();
    }

    private HttpContext HttpContext =>
        _httpContextAccessor.HttpContext
        ?? throw new InvalidOperationException(AuthConstants.Diagnostics.MissingHttpContext);

    private static ServiceError CreateModelError(string message)
    {
        return new ServiceError(AuthConstants.Validation.ModelErrorKey, message);
    }

    private async Task SignInUserAsync(ApplicationUser user, bool isPersistent)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.FullName),
            new(ClaimTypes.Email, user.Email),
            new(ClaimTypes.Role, user.Role.ToString()),
            new(AuthConstants.Claims.FullName, user.FullName)
        };

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);
        var properties = new AuthenticationProperties
        {
            IsPersistent = isPersistent,
            AllowRefresh = true,
            ExpiresUtc = isPersistent
                ? DateTimeOffset.UtcNow.AddDays(AuthConstants.Cookie.PersistentExpirationDays)
                : null
        };

        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal, properties);
    }

    private static string NormalizeEmail(string email)
    {
        return email.Trim();
    }
}

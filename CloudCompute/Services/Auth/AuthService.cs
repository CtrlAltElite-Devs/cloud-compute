using System.Security.Claims;
using CloudCompute.Constants;
using CloudCompute.Data;
using CloudCompute.Models;
using CloudCompute.Models.ViewModels.Auth;
using CloudCompute.Services.Common;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;

namespace CloudCompute.Services.Auth;

public class AuthService : IAuthService
{
    private readonly AppDbContext _context;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IPasswordHasher _passwordHasher;

    public AuthService(AppDbContext context, IHttpContextAccessor httpContextAccessor, IPasswordHasher passwordHasher)
    {
        _context = context;
        _httpContextAccessor = httpContextAccessor;
        _passwordHasher = passwordHasher;
    }

    public async Task<ServiceResult> LoginAsync(LoginViewModel model)
    {
        var loginIdentifier = model.LoginIdentifier.Trim();
        var normalizedLoginIdentifier = loginIdentifier.ToUpperInvariant();
        var user = await _context.Users
            .SingleOrDefaultAsync(user =>
                user.UserName.ToUpper() == normalizedLoginIdentifier ||
                user.Email.ToUpper() == normalizedLoginIdentifier);

        if (user is null || !_passwordHasher.VerifyPassword(model.Password, user.PasswordHash))
        {
            return ServiceResult.Failed(CreateModelError(AuthConstants.Messages.InvalidLogin));
        }

        if (!user.IsActive)
        {
            return ServiceResult.Failed(CreateModelError(AuthConstants.Messages.SuspendedAccount));
        }

        await SignInUserAsync(user, model.RememberMe);

        return ServiceResult.Success();
    }

    public async Task<ServiceResult> SignupAsync(SignupViewModel model)
    {
        var fullName = model.FullName.Trim();
        var userName = model.UserName.Trim();
        var email = model.Email.Trim();
        var normalizedUserName = userName.ToUpperInvariant();
        var normalizedEmail = email.ToUpperInvariant();
        var errors = new List<ServiceError>();

        var duplicateUserName = await _context.Users
            .AnyAsync(user => user.UserName.ToUpper() == normalizedUserName);
        if (duplicateUserName)
        {
            errors.Add(new ServiceError(nameof(SignupViewModel.UserName), AuthConstants.Messages.DuplicateUserName));
        }

        var duplicateEmail = await _context.Users
            .AnyAsync(user => user.Email.ToUpper() == normalizedEmail);
        if (duplicateEmail)
        {
            errors.Add(new ServiceError(nameof(SignupViewModel.Email), AuthConstants.Messages.DuplicateEmail));
        }

        if (errors.Count > 0)
        {
            return ServiceResult.Failed(errors.ToArray());
        }

        var user = new ApplicationUser
        {
            FullName = fullName,
            UserName = userName,
            Email = email,
            PasswordHash = _passwordHasher.HashPassword(model.Password)
        };

        _context.Users.Add(user);

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            return ServiceResult.Failed(CreateModelError(AuthConstants.Messages.DuplicateAccount));
        }

        await SignInUserAsync(user, isPersistent: false);

        return ServiceResult.Success();
    }

    public async Task LogoutAsync()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    }

    private HttpContext HttpContext =>
        _httpContextAccessor.HttpContext ?? throw new InvalidOperationException(AuthConstants.Diagnostics.MissingHttpContext);

    private static ServiceError CreateModelError(string message)
    {
        return new ServiceError(AuthConstants.Validation.ModelErrorKey, message);
    }

    private async Task SignInUserAsync(ApplicationUser user, bool isPersistent)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.UserName),
            new(ClaimTypes.Email, user.Email),
            new(AuthConstants.Claims.FullName, user.FullName)
        };

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);
        var properties = new AuthenticationProperties
        {
            IsPersistent = isPersistent,
            AllowRefresh = true
        };

        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal, properties);
    }
}

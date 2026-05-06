using CloudCompute.Constants;
using CloudCompute.Models;
using CloudCompute.Models.Enums;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace CloudCompute.Data;

public class DevelopmentAdminSeeder
{
    private readonly AppDbContext _dbContext;
    private readonly IConfiguration _configuration;
    private readonly IHostEnvironment _environment;
    private readonly IPasswordHasher<ApplicationUser> _passwordHasher;

    public DevelopmentAdminSeeder(
        AppDbContext dbContext,
        IConfiguration configuration,
        IHostEnvironment environment,
        IPasswordHasher<ApplicationUser> passwordHasher)
    {
        _dbContext = dbContext;
        _configuration = configuration;
        _environment = environment;
        _passwordHasher = passwordHasher;
    }

    public async Task SeedAsync()
    {
        if (!_environment.IsDevelopment())
        {
            return;
        }

        var fullName = _configuration["SeedAdmin:FullName"];
        var email = _configuration["SeedAdmin:Email"];
        var userName = _configuration["SeedAdmin:UserName"];
        var password = _configuration["SeedAdmin:Password"];

        if (string.IsNullOrWhiteSpace(fullName)
            || string.IsNullOrWhiteSpace(email)
            || string.IsNullOrWhiteSpace(password))
        {
            return;
        }

        email = email.Trim();
        userName = string.IsNullOrWhiteSpace(userName) ? email : userName.Trim();
        var (firstName, lastName) = SplitFullName(fullName);
        var normalizedEmail = email.ToUpperInvariant();
        var normalizedUserName = userName.ToUpperInvariant();
        var emailOwner = await _dbContext.Users
            .FirstOrDefaultAsync(user => user.Email.ToUpper() == normalizedEmail);
        var usernameOwner = await _dbContext.Users
            .FirstOrDefaultAsync(user => user.UserName.ToUpper() == normalizedUserName);

        if (emailOwner is not null && usernameOwner is not null && emailOwner.Id != usernameOwner.Id)
        {
            throw new InvalidOperationException(AuthConstants.Diagnostics.SeedAdminIdentityConflict);
        }

        var admin = emailOwner ?? usernameOwner;

        if (admin is null)
        {
            admin = new ApplicationUser
            {
                FirstName = firstName,
                LastName = lastName,
                Email = email,
                UserName = userName,
                Role = UserRole.Admin,
                IsActive = true
            };

            _dbContext.Users.Add(admin);
        }
        else
        {
            admin.FirstName = firstName;
            admin.LastName = lastName;
            admin.UserName = userName;
            admin.Role = UserRole.Admin;
            admin.IsActive = true;
        }

        admin.PasswordHash = _passwordHasher.HashPassword(admin, password);
        await _dbContext.SaveChangesAsync();
    }

    private static (string FirstName, string LastName) SplitFullName(string fullName)
    {
        var trimmed = fullName.Trim();
        var spaceIndex = trimmed.IndexOf(' ');
        if (spaceIndex < 0)
        {
            return (trimmed, string.Empty);
        }

        var first = trimmed[..spaceIndex];
        var last = trimmed[(spaceIndex + 1)..].TrimStart();
        return (first, last);
    }
}

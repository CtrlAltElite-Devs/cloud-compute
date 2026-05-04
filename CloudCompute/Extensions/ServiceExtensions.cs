using CloudCompute.Constants;
using CloudCompute.Data;
using CloudCompute.Models;
using CloudCompute.Services.Auth;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace CloudCompute.Extensions;

public static class ServiceExtensions
{
    public static void ConfigureServices(this IServiceCollection services, IConfiguration configuration)
    {
        ConfigureMvc(services);
        ConfigureDatabase(services, configuration);
        ConfigureAuthentication(services);
        ConfigureRouting(services);
        ConfigureApplicationServices(services);
    }

    private static void ConfigureMvc(IServiceCollection services)
    {
        services.AddControllersWithViews();
    }

    private static void ConfigureDatabase(IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection");

        services.AddDbContext<AppDbContext>(options =>
        {
            options.UseSqlServer(connectionString);
        });
    }

    private static void ConfigureAuthentication(IServiceCollection services)
    {
        services.AddHttpContextAccessor();

        services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
            .AddCookie(options =>
            {
                options.Cookie.Name = AuthConstants.Cookie.Name;
                options.Cookie.HttpOnly = true;
                options.Cookie.SameSite = SameSiteMode.Lax;
                options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
                options.LoginPath = AuthConstants.Routes.MemberLoginPath;
                options.LogoutPath = AuthConstants.Routes.LogoutPath;
                options.AccessDeniedPath = AuthConstants.Routes.AccessDeniedPath;
                options.SlidingExpiration = true;
                options.ExpireTimeSpan = TimeSpan.FromHours(AuthConstants.Cookie.ExpirationHours);
                options.Events = new CookieAuthenticationEvents
                {
                    OnRedirectToLogin = context =>
                    {
                        if (context.Request.Path.StartsWithSegments(AuthConstants.Routes.AdminPathPrefix))
                        {
                            context.Response.Redirect(AuthConstants.Routes.AdminLoginPath);
                            return Task.CompletedTask;
                        }

                        context.Response.Redirect(AuthConstants.Routes.MemberLoginPath);
                        return Task.CompletedTask;
                    }
                };
            });

        services.AddAuthorization();
    }

    private static void ConfigureRouting(IServiceCollection services)
    {
        services.AddRouting(options =>
        {
            options.LowercaseUrls = true;
            options.LowercaseQueryStrings = true;
        });
    }

    private static void ConfigureApplicationServices(IServiceCollection services)
    {
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<DevelopmentAdminSeeder>();
        services.AddScoped<IPasswordHasher<ApplicationUser>, PasswordHasher<ApplicationUser>>();
    }
}

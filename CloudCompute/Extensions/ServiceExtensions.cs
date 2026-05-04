using CloudCompute.Data;
using Microsoft.EntityFrameworkCore;

namespace CloudCompute.Extensions;

public static class ServiceExtensions
{
    public static void ConfigureServices(this IServiceCollection services, IConfiguration configuration)
    {
        ConfigureMvc(services);
        ConfigureDatabase(services, configuration);
        ConfigureRouting(services);
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

    private static void ConfigureRouting(IServiceCollection services)
    {
        services.AddRouting(options =>
        {
            options.LowercaseUrls = true;
            options.LowercaseQueryStrings = true;
        });
    }
}
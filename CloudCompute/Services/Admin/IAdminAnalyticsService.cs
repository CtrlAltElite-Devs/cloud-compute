using CloudCompute.ViewModels.Admin;

namespace CloudCompute.Services.Admin;

public interface IAdminAnalyticsService
{
    Task<AdminAnalyticsViewModel> GetAsync();
}

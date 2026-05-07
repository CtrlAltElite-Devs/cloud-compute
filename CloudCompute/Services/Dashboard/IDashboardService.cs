using CloudCompute.ViewModels.Dashboard;

namespace CloudCompute.Services.Dashboard;

public interface IDashboardService
{
    Task<DashboardViewModel?> GetDashboardAsync(Guid userId);
}

using CloudCompute.ViewModels.Admin;

namespace CloudCompute.Services.Admin;

public interface IAdminDashboardService
{
    Task<AdminDashboardViewModel> GetAsync();
}

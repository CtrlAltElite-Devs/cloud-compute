using CloudCompute.Services.Common;
using CloudCompute.ViewModels.Gpus;

namespace CloudCompute.Services.Gpu;

public interface IGpuService
{
    Task<ServiceResult> CreateAsync(Guid ownerId, GpuCreateViewModel model);
}

using CloudCompute.Models.Enums;
using CloudCompute.Services.Common;
using CloudCompute.ViewModels.Gpus;

namespace CloudCompute.Services.Gpu;

public interface IGpuService
{
    Task<GpuCatalogViewModel> GetCatalogAsync(Guid currentUserId, GpuCatalogFilter filter);

    Task<GpuDetailViewModel?> GetDetailAsync(Guid currentUserId, Guid gpuId);

    Task<ServiceResult> CreateAsync(Guid ownerId, GpuCreateViewModel model);

    Task<MyListingsViewModel> GetMineAsync(Guid ownerId);

    Task<RentedGpusViewModel> GetRentedAsync(Guid ownerId);

    Task<ServiceResult> SetStatusAsync(Guid ownerId, Guid gpuId, GpuStatus status);

    Task<ServiceResult> DeleteAsync(Guid ownerId, Guid gpuId);

    Task<GpuEditViewModel?> GetForEditAsync(Guid ownerId, Guid gpuId);

    Task<ServiceResult> UpdateAsync(Guid ownerId, GpuEditViewModel model);
}

namespace CloudCompute.ViewModels.Gpus;

public class MyListingsViewModel
{
    public IReadOnlyList<MyListingItemViewModel> Listings { get; init; } = Array.Empty<MyListingItemViewModel>();
}

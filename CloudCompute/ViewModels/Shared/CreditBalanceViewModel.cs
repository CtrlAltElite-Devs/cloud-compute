namespace CloudCompute.ViewModels.Shared;

public class CreditBalanceViewModel
{
    public decimal Balance { get; init; }

    public string Display => Balance.ToString("N2");
}

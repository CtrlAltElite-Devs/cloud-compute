namespace CloudCompute.ViewModels.Shared;

public class AlertViewModel
{
    public AlertType Type { get; set; } = AlertType.Info;

    public IReadOnlyList<string> Messages { get; set; } = Array.Empty<string>();

    public bool TextCenter { get; set; }
}

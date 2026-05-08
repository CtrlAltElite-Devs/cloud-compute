using System.Globalization;

namespace CloudCompute.Extensions;

public static class DateTimeExtensions
{
    private const string DateTimeFormat = "MMM d, yyyy h:mm tt";
    private const string DateFormat = "MMM d, yyyy";

    public static string ToFriendlyDateTime(this DateTime value)
        => DateTime.SpecifyKind(value, DateTimeKind.Utc).ToLocalTime().ToString(DateTimeFormat, CultureInfo.InvariantCulture);

    public static string ToFriendlyDateTime(this DateTime? value)
        => value.HasValue ? value.Value.ToFriendlyDateTime() : string.Empty;

    public static string ToFriendlyDate(this DateTime value)
        => DateTime.SpecifyKind(value, DateTimeKind.Utc).ToLocalTime().ToString(DateFormat, CultureInfo.InvariantCulture);

    public static string ToFriendlyDate(this DateTime? value)
        => value.HasValue ? value.Value.ToFriendlyDate() : string.Empty;
}

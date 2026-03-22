namespace Econyx.Dashboard.Helpers;

public static class DisplayFormatters
{
    public static string PnLClass(decimal value) =>
        value >= 0 ? "text-green" : "text-red";

    public static string FormatPnL(decimal value) =>
        value >= 0 ? $"+${value:F4}" : $"-${Math.Abs(value):F4}";

    public static string Truncate(string text, int maxLength) =>
        text.Length <= maxLength ? text : string.Concat(text.AsSpan(0, maxLength), "...");
}

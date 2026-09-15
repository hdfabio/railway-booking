using Railway.Contracts;
namespace Railway.Application.Services;

internal static class StationSearch
{
    /// <summary>Values like NDLS or MMCT — use exact code match (index-friendly).</summary>
    public static bool IsStationCode(string term)
    {
        var value = term.Trim().ToUpperInvariant();
        return value.Length is >= 2 and <= 6 && value.All(char.IsAsciiLetterOrDigit);
    }

    public static string NormalizeCode(string term) => term.Trim().ToUpperInvariant();

    public static string ToLikePattern(string term) => $"%{EscapeLike(term.Trim())}%";

    private static string EscapeLike(string value) =>
        value.Replace("\\", "\\\\", StringComparison.Ordinal)
            .Replace("%", "\\%", StringComparison.Ordinal)
            .Replace("_", "\\_", StringComparison.Ordinal);
}

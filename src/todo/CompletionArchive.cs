using System.Globalization;

namespace todo;

public static class CompletionArchive
{
    public static string RelativePath(DateOnly date) =>
        $"{date.ToString("yyyy", CultureInfo.InvariantCulture)}/{date.ToString("MMMM", CultureInfo.InvariantCulture).ToLowerInvariant()}/{date.ToString("dd-MM", CultureInfo.InvariantCulture)}";

    public static bool IsArchivePath(string path)
    {
        string[] parts = path.Split('/');
        return parts.Length == 3 &&
               DateOnly.TryParseExact($"{parts[0]}-{parts[2]}", "yyyy-dd-MM", CultureInfo.InvariantCulture,
                   DateTimeStyles.None, out DateOnly date) &&
               string.Equals(path, RelativePath(date), StringComparison.Ordinal);
    }
}

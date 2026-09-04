namespace LabManagementAPI.Services;

public static class SeedDisplayText
{
    public static string Clean(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return value ?? string.Empty;

        var text = value.TrimStart();
        var markerStart = text.IndexOf("[SEED-FULL", StringComparison.OrdinalIgnoreCase);
        if (markerStart < 0) return value;

        var markerEnd = text.IndexOf(']', markerStart);
        if (markerEnd < 0) return value;

        return $"{text[..markerStart].TrimEnd()}{text[(markerEnd + 1)..]}".Trim();
    }
}

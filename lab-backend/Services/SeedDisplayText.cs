namespace LabManagementAPI.Services;

public static class SeedDisplayText
{
    public static string Clean(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return value ?? string.Empty;

        var text = value.TrimStart();
        if (!text.StartsWith("[SEED-FULL", StringComparison.OrdinalIgnoreCase)) return value;

        var markerEnd = text.IndexOf(']');
        return markerEnd < 0 ? value : text[(markerEnd + 1)..].TrimStart();
    }
}

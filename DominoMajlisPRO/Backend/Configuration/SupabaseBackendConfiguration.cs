namespace DominoMajlisPRO.Backend.Configuration;

public static class SupabaseBackendConfiguration
{
    // Fill these values locally from Supabase Dashboard > Settings > API Keys.
    // Do not place secret/service_role keys in the client application.
    public const string ProjectUrl = "PASTE_SUPABASE_PROJECT_URL_HERE";
    public const string PublishableKey = "PASTE_SUPABASE_PUBLISHABLE_KEY_HERE";

    public static bool IsConfigured =>
        ProjectUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase) &&
        !PublishableKey.StartsWith("PASTE_", StringComparison.OrdinalIgnoreCase) &&
        !string.IsNullOrWhiteSpace(PublishableKey);
}

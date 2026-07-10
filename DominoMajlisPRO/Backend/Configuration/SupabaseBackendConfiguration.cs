namespace DominoMajlisPRO.Backend.Configuration;

public static class SupabaseBackendConfiguration
{
    // Fill these values locally from Supabase Dashboard > Settings > API Keys.
    // Do not place secret/service_role keys in the client application.
    public const string ProjectUrl = "https://iyxpfofgzmpcvlxwwocs.supabase.co";
    public const string PublishableKey = "sb_publishable_WgN1qylrDrOBwgEMRiUIBA_Iwc3OL04";

    public static bool IsConfigured =>
        ProjectUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase) &&
        !PublishableKey.StartsWith("PASTE_", StringComparison.OrdinalIgnoreCase) &&
        !string.IsNullOrWhiteSpace(PublishableKey);
}

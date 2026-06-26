using DominoMajlisPRO.LivingVisualPlatform.Models;

namespace DominoMajlisPRO.LivingVisualPlatform.Services;

public static class LivingVisualDisplayLocationCatalog
{
    public static IReadOnlyList<LivingVisualDisplayLocation> TeamEmblemLocations { get; } =
        new[]
        {
            LivingVisualDisplayLocation.StorePreview,
            LivingVisualDisplayLocation.StoreActionSheet,
            LivingVisualDisplayLocation.Inventory,
            LivingVisualDisplayLocation.CreateTeamPreview,
            LivingVisualDisplayLocation.EditTeamPreview,
            LivingVisualDisplayLocation.MainPageTeamSelector,
            LivingVisualDisplayLocation.GamePageTeamEmblem,
            LivingVisualDisplayLocation.MatchDetailsTeamEmblem,
            LivingVisualDisplayLocation.HistoryTeamEmblem,
            LivingVisualDisplayLocation.RankingsTeamSection,
            LivingVisualDisplayLocation.HallOfFameTeamSection,
            LivingVisualDisplayLocation.CertificateTeamEmblem
        };
}

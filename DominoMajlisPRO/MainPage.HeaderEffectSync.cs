using DominoMajlisPRO.GalleryEngine.Services;
using DominoMajlisPRO.Services;

namespace DominoMajlisPRO;

public partial class MainPage
{
    int headerEffectSyncVersion;

    async Task ReapplyMainHeaderEffectWithPlayerDetailsScaleAsync()
    {
        int syncVersion = Interlocked.Increment(ref headerEffectSyncVersion);

        try
        {
            var currentUser =
                await ApplicationUserService.GetCurrentUserAsync();

            string playerId = currentUser.PlayerId;
            if (string.IsNullOrWhiteSpace(playerId))
            {
                PlayerEffectEngine.Apply(
                    HeaderAvatarEffectOverlay,
                    null,
                    MainHeaderAvatarEffectScale);
                return;
            }

            var visualIdentity =
                await PlayerVisualIdentityResolver.ResolveAsync(playerId);

            if (syncVersion != headerEffectSyncVersion)
                return;

            ApplyMainHeaderAvatarIdentityVisuals(visualIdentity);
        }
        catch
        {
            PlayerEffectEngine.Apply(
                HeaderAvatarEffectOverlay,
                null,
                MainHeaderAvatarEffectScale);
        }
    }
}

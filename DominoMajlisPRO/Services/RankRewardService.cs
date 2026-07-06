using System.Text.Json;
using DominoMajlisPRO.GalleryEngine.Services;
using DominoMajlisPRO.Models;

namespace DominoMajlisPRO.Services;

/// <summary>
/// Rank reward constitution. Grants a team the reward for a rank exactly once,
/// keeps a full audit trail, honours the Hall / Anti-Cheat presumption of
/// innocence, and credits player wallets from the canonical
/// <see cref="RankRewardCatalog"/>. Rewards are never hardcoded in the UI.
/// </summary>
public static class RankRewardService
{
    const string FileName = "rank_reward_grants.json";
    static readonly SemaphoreSlim Gate = new(1, 1);

    static string StoragePath =>
        Path.Combine(FileSystem.AppDataDirectory, FileName);

    // =========================
    // PERSISTENCE
    // =========================

    public static async Task<List<RankRewardGrant>> LoadGrantsAsync()
    {
        try
        {
            if (!File.Exists(StoragePath))
                return new();

            var json = await File.ReadAllTextAsync(StoragePath);
            return JsonSerializer.Deserialize<List<RankRewardGrant>>(json) ?? new();
        }
        catch
        {
            return new();
        }
    }

    static async Task SaveGrantsAsync(List<RankRewardGrant> grants)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(StoragePath)!);
            var json = JsonSerializer.Serialize(grants, new JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync(StoragePath, json);
        }
        catch
        {
        }
    }

    // =========================
    // INTEGRITY (Anti-Cheat)
    // =========================

    /// <summary>
    /// Maps existing team integrity signals onto the reward gate. Suspicion
    /// alone is never treated as confirmed fraud (Articles 7-9, 12-14).
    /// </summary>
    public static RankRewardIntegrity EvaluateIntegrity(TeamProfileModel? team)
    {
        if (team == null)
            return RankRewardIntegrity.Clear;

        // A saturated suspicion score represents a confirmed determination that
        // could only be reached after the constitutional workflow; only this
        // blocks the reward.
        if (team.SuspiciousScore >= 100)
            return RankRewardIntegrity.Blocked;

        if (team.IsSuspicious || team.SuspiciousScore >= 60)
            return RankRewardIntegrity.Watch;

        return RankRewardIntegrity.Clear;
    }

    // =========================
    // QUERIES
    // =========================

    /// <summary>True when the team has already claimed the reward for a rank.</summary>
    public static async Task<bool> HasClaimedAsync(string teamId, string rankId)
    {
        if (string.IsNullOrWhiteSpace(teamId) || string.IsNullOrWhiteSpace(rankId))
            return false;

        var grants = await LoadGrantsAsync();
        return grants.Any(item =>
            item.ClaimedOnce &&
            !item.AuditOnly &&
            string.Equals(item.TeamId, teamId, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(item.RankId, rankId, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>Preview of the reward for the next rank the team can reach.</summary>
    public static RankRewardDefinition? PreviewNextReward(int xp) =>
        RankRewardCatalog.NextRewardFromXp(xp);

    // =========================
    // GRANTING
    // =========================

    /// <summary>
    /// Grants (once) every rank reward a team has newly earned up to its
    /// current XP. Safe to call repeatedly; already-claimed ranks are skipped.
    /// Returns the grants actually credited this call.
    /// </summary>
    public static async Task<IReadOnlyList<RankRewardGrant>> SyncTeamRewardsAsync(
        TeamProfileModel? team,
        string? oldRank = null,
        string? matchId = null)
    {
        if (team == null || string.IsNullOrWhiteSpace(team.TeamId))
            return Array.Empty<RankRewardGrant>();

        var currentRankId = RankingService.GetRankFromXP(team.XP);
        var integrity = EvaluateIntegrity(team);

        await Gate.WaitAsync();
        var credited = new List<RankRewardGrant>();
        try
        {
            var grants = await LoadGrantsAsync();
            var changed = false;

            foreach (var definition in RankRewardCatalog.All)
            {
                // Only ranks the team has actually reached.
                if (team.XP < definition.RequiredXP)
                    continue;

                var already = grants.Any(item =>
                    item.ClaimedOnce &&
                    !item.AuditOnly &&
                    string.Equals(item.TeamId, team.TeamId, StringComparison.OrdinalIgnoreCase) &&
                    string.Equals(item.RankId, definition.RankId, StringComparison.OrdinalIgnoreCase));

                if (already)
                    continue;

                if (integrity == RankRewardIntegrity.Blocked)
                {
                    // Audit only: confirmed fraud never receives currency.
                    grants.Add(BuildGrant(team, definition, oldRank, currentRankId, matchId,
                        integrity, auditOnly: true, claimed: false,
                        reason: "Blocked: Confirmed Fraud — reward withheld."));
                    changed = true;
                    continue;
                }

                await CreditTeamWalletsAsync(team, definition.CoinsReward, definition.GemsReward);

                var grant = BuildGrant(team, definition, oldRank, currentRankId, matchId,
                    integrity, auditOnly: false, claimed: true,
                    reason: integrity == RankRewardIntegrity.Watch
                        ? "Granted under Watch — audit recorded (Article 12)."
                        : "Rank reward granted.");
                grants.Add(grant);
                credited.Add(grant);
                changed = true;
            }

            if (changed)
                await SaveGrantsAsync(grants);
        }
        finally
        {
            Gate.Release();
        }

        if (credited.Count > 0)
            AppEvents.RaiseRankRewardGranted(team.TeamId);

        return credited;
    }

    /// <summary>
    /// Records a manual (developer) rank edit as audit-only. Manual edits never
    /// grant currency unless documented by a separate developer audit action.
    /// </summary>
    public static async Task RecordManualRankEditAsync(
        TeamProfileModel? team,
        string oldRank,
        string newRank,
        string reason)
    {
        if (team == null || string.IsNullOrWhiteSpace(team.TeamId))
            return;

        await Gate.WaitAsync();
        try
        {
            var grants = await LoadGrantsAsync();
            grants.Add(new RankRewardGrant
            {
                TeamId = team.TeamId,
                RankId = newRank,
                RankName = newRank,
                Tier = string.Empty,
                RequiredXP = 0,
                CoinsReward = 0,
                GemsReward = 0,
                SpecialReward = string.Empty,
                ClaimedOnce = false,
                AuditOnly = true,
                DateClaimed = DateTime.UtcNow,
                OldRank = oldRank ?? string.Empty,
                NewRank = newRank ?? string.Empty,
                MatchId = string.Empty,
                Integrity = EvaluateIntegrity(team),
                Reason = string.IsNullOrWhiteSpace(reason)
                    ? "Manual rank edit (audit only, no reward)."
                    : reason
            });
            await SaveGrantsAsync(grants);
        }
        finally
        {
            Gate.Release();
        }
    }

    static RankRewardGrant BuildGrant(
        TeamProfileModel team,
        RankRewardDefinition definition,
        string? oldRank,
        string newRank,
        string? matchId,
        RankRewardIntegrity integrity,
        bool auditOnly,
        bool claimed,
        string reason) =>
        new()
        {
            TeamId = team.TeamId,
            RankId = definition.RankId,
            RankName = definition.RankName,
            Tier = definition.Tier,
            RequiredXP = definition.RequiredXP,
            CoinsReward = definition.CoinsReward,
            GemsReward = definition.GemsReward,
            SpecialReward = definition.SpecialReward,
            ClaimedOnce = claimed,
            AuditOnly = auditOnly,
            DateClaimed = DateTime.UtcNow,
            OldRank = oldRank ?? string.Empty,
            NewRank = newRank,
            MatchId = matchId ?? string.Empty,
            Integrity = integrity,
            Reason = reason
        };

    static async Task CreditTeamWalletsAsync(TeamProfileModel team, int coins, int gems)
    {
        foreach (var playerId in new[] { team.Player1Id, team.Player2Id })
        {
            if (string.IsNullOrWhiteSpace(playerId))
                continue;

            try
            {
                await PlayerWalletService.CreditAsync(playerId, coins, gems);
                AppEvents.RaiseWalletChanged(playerId);
            }
            catch
            {
            }
        }
    }
}

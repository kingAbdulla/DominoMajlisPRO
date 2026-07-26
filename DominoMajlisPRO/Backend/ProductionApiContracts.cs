using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using DominoMajlisPRO.Backend.Authentication;
using DominoMajlisPRO.Backend.Configuration;

namespace DominoMajlisPRO.Backend;

public sealed record BackendApiResult<T>(
    bool Success,
    string Message,
    T? Data = default,
    HttpStatusCode? StatusCode = null,
    bool IsServerUnavailable = false)
{
    public static BackendApiResult<T> Disabled(string feature) =>
        new(false, $"{feature} is not available because the production backend is not configured.", default, null, true);

    public static BackendApiResult<T> Failure(string message, HttpStatusCode? statusCode = null) =>
        new(false, message, default, statusCode);
}

public sealed class ProductionBackendClient
{
    static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    readonly HttpClient httpClient;

    public ProductionBackendClient()
        : this(new HttpClient { Timeout = TimeSpan.FromSeconds(10) })
    {
    }

    public ProductionBackendClient(HttpClient httpClient)
    {
        this.httpClient = httpClient;
    }

    public async Task<BackendApiResult<TResponse>> GetAsync<TResponse>(
        string functionPath,
        string featureName,
        CancellationToken cancellationToken = default)
    {
        var request = await CreateRequestAsync<object>(HttpMethod.Get, functionPath, featureName, null, cancellationToken);
        if (request.Result != null) return request.Result.Cast<TResponse>();
        return await SendAsync<TResponse>(request.Request!, featureName, cancellationToken);
    }

    public async Task<BackendApiResult<TResponse>> PostAsync<TRequest, TResponse>(
        string functionPath,
        string featureName,
        TRequest body,
        CancellationToken cancellationToken = default)
    {
        var request = await CreateRequestAsync(HttpMethod.Post, functionPath, featureName, body, cancellationToken);
        if (request.Result != null) return request.Result.Cast<TResponse>();
        return await SendAsync<TResponse>(request.Request!, featureName, cancellationToken);
    }

    async Task<(HttpRequestMessage? Request, BackendApiResult<object>? Result)> CreateRequestAsync<TBody>(
        HttpMethod method,
        string functionPath,
        string featureName,
        TBody? body,
        CancellationToken cancellationToken)
    {
        _ = cancellationToken;

        if (!SupabaseBackendConfiguration.IsConfigured)
            return (null, BackendApiResult<object>.Disabled(featureName));

        var session = await SupabaseTokenStore.LoadAsync();
        if (session == null || string.IsNullOrWhiteSpace(session.AccessToken))
            return (null, BackendApiResult<object>.Failure("Sign in is required before using this service."));

        var request = new HttpRequestMessage(
            method,
            SupabaseBackendConfiguration.ProjectUrl.TrimEnd('/') + "/functions/v1/" + functionPath.TrimStart('/'));

        request.Headers.Add("apikey", SupabaseBackendConfiguration.PublishableKey);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", session.AccessToken.Trim());
        request.Headers.Add("x-client-idempotency", Guid.NewGuid().ToString("N"));

        if (body != null)
        {
            request.Content = new StringContent(
                JsonSerializer.Serialize(body, JsonOptions),
                Encoding.UTF8,
                "application/json");
        }

        return (request, null);
    }

    async Task<BackendApiResult<TResponse>> SendAsync<TResponse>(
        HttpRequestMessage request,
        string featureName,
        CancellationToken cancellationToken)
    {
        try
        {
            using var response = await httpClient.SendAsync(request, cancellationToken);
            var json = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
                return BackendApiResult<TResponse>.Failure(
                    SafeErrorMessage(featureName, response.StatusCode),
                    response.StatusCode);

            var payload = string.IsNullOrWhiteSpace(json)
                ? default
                : JsonSerializer.Deserialize<TResponse>(json, JsonOptions);

            return new BackendApiResult<TResponse>(true, "Synced with production backend.", payload, response.StatusCode);
        }
        catch (OperationCanceledException)
        {
            return BackendApiResult<TResponse>.Failure($"{featureName} timed out while contacting the server.");
        }
        catch (HttpRequestException ex)
        {
            return new BackendApiResult<TResponse>(
                false,
                $"{featureName} is unavailable. No local fallback mutation was performed. {ex.Message}",
                default,
                null,
                true);
        }
        catch (JsonException ex)
        {
            return BackendApiResult<TResponse>.Failure($"{featureName} returned invalid data: {ex.Message}");
        }
    }

    static string SafeErrorMessage(string featureName, HttpStatusCode statusCode) =>
        statusCode switch
        {
            HttpStatusCode.Unauthorized => "Session expired. Sign in and try again.",
            HttpStatusCode.Forbidden => "You are not allowed to perform this operation.",
            HttpStatusCode.NotFound => $"{featureName} is not published on the backend yet.",
            HttpStatusCode.TooManyRequests => "Too many requests. Wait and try again.",
            _ => $"{featureName} rejected the request. No local fallback mutation was performed."
        };
}

static class BackendResultCastExtensions
{
    public static BackendApiResult<T> Cast<T>(this BackendApiResult<object> result) =>
        new(result.Success, result.Message, default, result.StatusCode, result.IsServerUnavailable);
}

public sealed class CommerceApiClient
{
    readonly ProductionBackendClient backend;

    public CommerceApiClient()
        : this(new ProductionBackendClient())
    {
    }

    public CommerceApiClient(ProductionBackendClient backend)
    {
        this.backend = backend;
    }

    public Task<BackendApiResult<CommerceCatalogResponse>> LoadCatalogAsync(CancellationToken cancellationToken = default) =>
        backend.GetAsync<CommerceCatalogResponse>("commerce/catalog", "Commerce catalog", cancellationToken);

    public Task<BackendApiResult<PurchaseVerificationResponse>> SubmitGooglePlayPurchaseAsync(
        GooglePlayPurchaseVerificationRequest request,
        CancellationToken cancellationToken = default) =>
        backend.PostAsync<GooglePlayPurchaseVerificationRequest, PurchaseVerificationResponse>(
            "commerce/purchases/google-play",
            "Google Play verification",
            request,
            cancellationToken);

    public Task<BackendApiResult<WalletProjectionResponse>> LoadWalletAsync(CancellationToken cancellationToken = default) =>
        backend.GetAsync<WalletProjectionResponse>("wallet", "Wallet", cancellationToken);
}

public sealed record CommerceCatalogResponse(IReadOnlyList<CommerceProductDto> Products, string Version, DateTime UpdatedAtUtc);
public sealed record CommerceProductDto(string InternalProductId, string AndroidProductId, string IosProductId, string ProductType, string EntitlementType, bool Active, int SortOrder);
public sealed record GooglePlayPurchaseVerificationRequest(string PlatformProductId, string PurchaseToken, string PackageName, string IdempotencyKey, string ClientPurchaseState);
public sealed record PurchaseVerificationResponse(string InternalPurchaseId, string PurchaseState, bool EntitlementGranted, int GemsBalance, int CoinsBalance, string UserMessage);
public sealed record WalletProjectionResponse(int Coins, int Gems, string Version, DateTime UpdatedAtUtc);

public sealed class FeatureFlagApiClient
{
    readonly ProductionBackendClient backend;

    public FeatureFlagApiClient()
        : this(new ProductionBackendClient())
    {
    }

    public FeatureFlagApiClient(ProductionBackendClient backend)
    {
        this.backend = backend;
    }

    public async Task<ProductionFeatureFlags> LoadFlagsAsync(CancellationToken cancellationToken = default)
    {
        var result = await backend.GetAsync<ProductionFeatureFlags>(
            "runtime/feature-flags",
            "Feature flags",
            cancellationToken);

        return result.Success && result.Data != null
            ? result.Data
            : ProductionFeatureFlags.FailClosed;
    }
}

public sealed record ProductionFeatureFlags(
    bool RealMoneyPurchasesEnabled,
    bool FriendRequestsEnabled,
    bool PublicProfilesEnabled,
    bool OnlineXpEnabled,
    bool TeamRankEnabled,
    bool StorePublishingEnabled,
    bool BackupRestoreEnabled,
    bool LivingVisualProductsEnabled,
    string Version,
    DateTime UpdatedAtUtc)
{
    public static ProductionFeatureFlags FailClosed { get; } = new(
        RealMoneyPurchasesEnabled: false,
        FriendRequestsEnabled: false,
        PublicProfilesEnabled: false,
        OnlineXpEnabled: false,
        TeamRankEnabled: false,
        StorePublishingEnabled: false,
        BackupRestoreEnabled: false,
        LivingVisualProductsEnabled: false,
        Version: "fail-closed",
        UpdatedAtUtc: DateTime.UtcNow);
}

public sealed class FriendsApiClient
{
    readonly ProductionBackendClient backend;

    public FriendsApiClient()
        : this(new ProductionBackendClient())
    {
    }

    public FriendsApiClient(ProductionBackendClient backend)
    {
        this.backend = backend;
    }

    public Task<BackendApiResult<FriendSearchResponse>> SearchByPlayerIdAsync(string playerId, CancellationToken cancellationToken = default) =>
        backend.PostAsync<object, FriendSearchResponse>(
            "friends/search",
            "Friends search",
            new { playerId = playerId.Trim() },
            cancellationToken);

    public Task<BackendApiResult<FriendMutationResponse>> SendRequestAsync(string targetPlayerId, CancellationToken cancellationToken = default) =>
        backend.PostAsync<object, FriendMutationResponse>(
            "friends/requests",
            "Friend request",
            new { targetPlayerId = targetPlayerId.Trim() },
            cancellationToken);

    public Task<BackendApiResult<FriendListResponse>> LoadFriendsAsync(CancellationToken cancellationToken = default) =>
        backend.GetAsync<FriendListResponse>("friends/list", "Friends list", cancellationToken);
}

public sealed record FriendSearchResponse(string PlayerId, string DisplayName, string RelationshipState, bool CanSendRequest);
public sealed record FriendMutationResponse(string RelationshipState, string Message);
public sealed record FriendListResponse(IReadOnlyList<FriendSummaryDto> Friends, DateTime SyncedAtUtc);
public sealed record FriendSummaryDto(string PlayerId, string DisplayName, string RankName, string CurrentTeamName);

public sealed class ProgressionApiClient
{
    readonly ProductionBackendClient backend;

    public ProgressionApiClient()
        : this(new ProductionBackendClient())
    {
    }

    public ProgressionApiClient(ProductionBackendClient backend)
    {
        this.backend = backend;
    }

    public Task<BackendApiResult<MatchSubmissionResponse>> SubmitMatchAsync(
        MatchSubmissionRequest request,
        CancellationToken cancellationToken = default) =>
        backend.PostAsync<MatchSubmissionRequest, MatchSubmissionResponse>(
            "matches/submit",
            "Match submission",
            request,
            cancellationToken);

    public Task<BackendApiResult<ProgressionProjectionResponse>> LoadProjectionAsync(CancellationToken cancellationToken = default) =>
        backend.GetAsync<ProgressionProjectionResponse>("progression/projection", "Progression projection", cancellationToken);
}

public sealed record MatchSubmissionRequest(string MatchId, string Team1Id, string Team2Id, IReadOnlyList<ScoreEventDto> ScoreEvents, string IdempotencyKey);
public sealed record ScoreEventDto(int Round, string TeamId, int Score, DateTime ClientRecordedAtUtc);
public sealed record MatchSubmissionResponse(string MatchState, bool Accepted, string ServerMessage, ProgressionProjectionResponse? Projection);
public sealed record ProgressionProjectionResponse(string Version, DateTime UpdatedAtUtc, IReadOnlyList<PlayerProgressionDto> Players, IReadOnlyList<TeamProgressionDto> Teams);
public sealed record PlayerProgressionDto(string PlayerId, int Xp, string RankId, string RankName);
public sealed record TeamProgressionDto(string TeamId, int Xp, string RankId, string RankName);

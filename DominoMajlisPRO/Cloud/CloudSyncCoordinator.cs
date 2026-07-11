using System.Text.Json;

namespace DominoMajlisPRO.Cloud;

public sealed record CloudSyncResult(
    int Uploaded,
    int Downloaded,
    int Deleted,
    int QueueRemaining,
    DateTimeOffset CompletedAt);

public sealed class CloudSyncCoordinator
{
    private static readonly string[] Resources =
    [
        CloudResources.Players,
        CloudResources.Teams,
        CloudResources.Matches,
        CloudResources.Rankings,
        CloudResources.StoreItems,
        CloudResources.Purchases,
        CloudResources.VisualIdentities,
        CloudResources.Notifications,
        CloudResources.Profiles,
        CloudResources.Wallets,
        CloudResources.Inventory
    ];

    private readonly CloudApiClient _client;
    private readonly CloudSessionStore _sessionStore;
    private readonly CloudSyncStateStore _stateStore;
    private readonly SemaphoreSlim _syncGate = new(1, 1);
    private CancellationTokenSource? _backgroundCts;

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true
    };

    public CloudSyncCoordinator(
        CloudApiClient client,
        CloudSessionStore sessionStore,
        CloudSyncStateStore stateStore)
    {
        _client = client;
        _sessionStore = sessionStore;
        _stateStore = stateStore;
    }

    public void StartBackgroundSync(TimeSpan? interval = null)
    {
        if (_backgroundCts is not null)
            return;

        _backgroundCts = new CancellationTokenSource();
        _ = RunBackgroundLoopAsync(interval ?? TimeSpan.FromMinutes(5), _backgroundCts.Token);
    }

    public void StopBackgroundSync()
    {
        _backgroundCts?.Cancel();
        _backgroundCts?.Dispose();
        _backgroundCts = null;
    }

    public async Task<CloudSyncResult> SynchronizeAsync(CancellationToken cancellationToken = default)
    {
        if (await _sessionStore.LoadAsync(cancellationToken) is null)
            return new CloudSyncResult(0, 0, 0, 0, DateTimeOffset.UtcNow);

        await _syncGate.WaitAsync(cancellationToken);
        try
        {
            int uploaded = await FlushQueueAsync(cancellationToken);
            int downloaded = 0;
            int deleted = 0;

            foreach (string resource in Resources)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var result = await PullResourceAsync(resource, cancellationToken);
                downloaded += result.Downloaded;
                deleted += result.Deleted;
                uploaded += await PushLocalResourceAsync(resource, cancellationToken);
            }

            uploaded += await FlushQueueAsync(cancellationToken);
            int remaining = (await _stateStore.LoadQueueAsync(cancellationToken)).Count;
            return new CloudSyncResult(uploaded, downloaded, deleted, remaining, DateTimeOffset.UtcNow);
        }
        catch (CloudApiException)
        {
            int remaining = (await _stateStore.LoadQueueAsync(cancellationToken)).Count;
            return new CloudSyncResult(0, 0, 0, remaining, DateTimeOffset.UtcNow);
        }
        catch (HttpRequestException)
        {
            int remaining = (await _stateStore.LoadQueueAsync(cancellationToken)).Count;
            return new CloudSyncResult(0, 0, 0, remaining, DateTimeOffset.UtcNow);
        }
        finally
        {
            _syncGate.Release();
        }
    }

    private async Task<(int Downloaded, int Deleted)> PullResourceAsync(
        string resource,
        CancellationToken cancellationToken)
    {
        var remote = await _client.GetAllAsync<JsonElement>(
            resource,
            includeDeleted: true,
            cancellationToken: cancellationToken);

        string path = LocalPath(resource);
        List<JsonElement> local = await ReadArrayAsync(path, cancellationToken);
        var byId = local
            .Select(item => (Id: ResolveId(resource, item), Item: item))
            .Where(pair => !string.IsNullOrWhiteSpace(pair.Id))
            .ToDictionary(pair => pair.Id!, pair => pair.Item, StringComparer.OrdinalIgnoreCase);

        int downloaded = 0;
        int deleted = 0;

        foreach (var record in remote)
        {
            var state = await _stateStore.GetRecordStateAsync(resource, record.RecordId, cancellationToken);
            if (state is not null && state.Revision >= record.Revision)
                continue;

            if (record.DeletedAt is not null)
            {
                if (byId.Remove(record.RecordId))
                    deleted++;
            }
            else
            {
                byId[record.RecordId] = record.Payload;
                downloaded++;
            }

            await _stateStore.SaveRecordStateAsync(
                resource,
                record.RecordId,
                record.Revision,
                record.UpdatedAt,
                cancellationToken);
        }

        if (downloaded > 0 || deleted > 0)
            await WriteArrayAsync(path, byId.Values, cancellationToken);

        return (downloaded, deleted);
    }

    private async Task<int> PushLocalResourceAsync(string resource, CancellationToken cancellationToken)
    {
        int uploaded = 0;
        foreach (var item in await ReadArrayAsync(LocalPath(resource), cancellationToken))
        {
            string? id = ResolveId(resource, item);
            if (string.IsNullOrWhiteSpace(id))
                continue;

            try
            {
                var record = await _client.UpsertAsync(resource, id, item, cancellationToken);
                await _stateStore.SaveRecordStateAsync(
                    resource,
                    id,
                    record.Revision,
                    record.UpdatedAt,
                    cancellationToken);
                uploaded++;
            }
            catch (CloudApiException)
            {
                await _stateStore.EnqueueAsync("PUT", resource, id, item, cancellationToken);
            }
            catch (HttpRequestException)
            {
                await _stateStore.EnqueueAsync("PUT", resource, id, item, cancellationToken);
                break;
            }
        }
        return uploaded;
    }

    private async Task<int> FlushQueueAsync(CancellationToken cancellationToken)
    {
        var queue = await _stateStore.LoadQueueAsync(cancellationToken);
        if (queue.Count == 0)
            return 0;

        var remaining = new List<CloudPendingOperation>();
        int completed = 0;

        foreach (var operation in queue.OrderBy(item => item.CreatedAt))
        {
            try
            {
                if (string.Equals(operation.Method, "DELETE", StringComparison.OrdinalIgnoreCase))
                {
                    await _client.DeleteAsync(operation.Resource, operation.RecordId, cancellationToken);
                }
                else if (operation.Payload is JsonElement payload)
                {
                    var record = await _client.UpsertAsync(
                        operation.Resource,
                        operation.RecordId,
                        payload,
                        cancellationToken);
                    await _stateStore.SaveRecordStateAsync(
                        operation.Resource,
                        operation.RecordId,
                        record.Revision,
                        record.UpdatedAt,
                        cancellationToken);
                }
                completed++;
            }
            catch (Exception ex) when (ex is CloudApiException or HttpRequestException or TaskCanceledException)
            {
                remaining.Add(operation with { Attempts = operation.Attempts + 1 });
            }
        }

        await _stateStore.ReplaceQueueAsync(remaining, cancellationToken);
        return completed;
    }

    private async Task RunBackgroundLoopAsync(TimeSpan interval, CancellationToken cancellationToken)
    {
        try
        {
            await SynchronizeAsync(cancellationToken);
            using var timer = new PeriodicTimer(interval);
            while (await timer.WaitForNextTickAsync(cancellationToken))
                await SynchronizeAsync(cancellationToken);
        }
        catch (OperationCanceledException)
        {
        }
    }

    private static async Task<List<JsonElement>> ReadArrayAsync(string path, CancellationToken cancellationToken)
    {
        if (!File.Exists(path))
            return [];
        try
        {
            await using var stream = File.OpenRead(path);
            return await JsonSerializer.DeserializeAsync<List<JsonElement>>(stream, JsonOptions, cancellationToken) ?? [];
        }
        catch (JsonException)
        {
            return [];
        }
    }

    private static async Task WriteArrayAsync(
        string path,
        IEnumerable<JsonElement> items,
        CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path) ?? FileSystem.AppDataDirectory);
        string tempPath = path + ".tmp";
        await using (var stream = File.Create(tempPath))
            await JsonSerializer.SerializeAsync(stream, items.ToList(), JsonOptions, cancellationToken);
        File.Move(tempPath, path, true);
    }

    private static string LocalPath(string resource) => resource switch
    {
        CloudResources.Players => Path.Combine(FileSystem.AppDataDirectory, "players.json"),
        CloudResources.Teams => Path.Combine(FileSystem.AppDataDirectory, "teams.json"),
        CloudResources.Matches => Path.Combine(FileSystem.AppDataDirectory, "matches.json"),
        _ => Path.Combine(FileSystem.AppDataDirectory, $"cloud_{resource.Replace('-', '_')}.json")
    };

    private static string? ResolveId(string resource, JsonElement item)
    {
        string[] candidates = resource switch
        {
            CloudResources.Players => ["playerId", "PlayerId"],
            CloudResources.Teams => ["teamId", "TeamId"],
            CloudResources.Matches => ["matchId", "MatchId"],
            CloudResources.StoreItems => ["assetId", "AssetId", "id", "Id"],
            _ => ["recordId", "RecordId", "id", "Id", "applicationUserId", "ApplicationUserId"]
        };

        foreach (string candidate in candidates)
        {
            if (item.ValueKind == JsonValueKind.Object && item.TryGetProperty(candidate, out var value))
                return value.ValueKind == JsonValueKind.String ? value.GetString() : value.ToString();
        }
        return null;
    }
}

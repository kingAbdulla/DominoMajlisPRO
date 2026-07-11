using System.Collections.Concurrent;

namespace DominoMajlisPRO.Cloud;

public static class CloudSyncRuntime
{
    private static readonly ConcurrentDictionary<string, SemaphoreSlim> ResourceGates = new(StringComparer.OrdinalIgnoreCase);
    private static CloudApiClient? _client;
    private static CloudSyncStateStore? _stateStore;
    private static CloudSyncCoordinator? _coordinator;

    public static void Configure(
        CloudApiClient client,
        CloudSyncStateStore stateStore,
        CloudSyncCoordinator coordinator)
    {
        _client = client ?? throw new ArgumentNullException(nameof(client));
        _stateStore = stateStore ?? throw new ArgumentNullException(nameof(stateStore));
        _coordinator = coordinator ?? throw new ArgumentNullException(nameof(coordinator));
    }

    public static bool IsConfigured => _client is not null;

    public static Task<CloudSyncResult>? SynchronizeAsync(CancellationToken cancellationToken = default) =>
        _coordinator?.SynchronizeAsync(cancellationToken);

    public static async Task<bool> TryUpsertAsync<T>(
        string resource,
        string recordId,
        T payload,
        CancellationToken cancellationToken = default)
    {
        if (_client is null || _stateStore is null || string.IsNullOrWhiteSpace(recordId))
            return false;

        string id = recordId.Trim();
        var gate = ResourceGates.GetOrAdd(resource, _ => new SemaphoreSlim(1, 1));
        await gate.WaitAsync(cancellationToken);
        try
        {
            var record = await _client.UpsertAsync(resource, id, payload, cancellationToken);
            await _stateStore.SaveRecordStateAsync(
                resource,
                id,
                record.Revision,
                record.UpdatedAt,
                cancellationToken);
            return true;
        }
        catch (Exception ex) when (ex is CloudApiException or HttpRequestException or TaskCanceledException)
        {
            await _stateStore.EnqueueAsync("PUT", resource, id, payload, cancellationToken);
            return false;
        }
        finally
        {
            gate.Release();
        }
    }

    public static async Task<int> TryUpsertManyAsync<T>(
        string resource,
        IEnumerable<T> items,
        Func<T, string?> idSelector,
        CancellationToken cancellationToken = default)
    {
        int uploaded = 0;
        foreach (var item in items)
        {
            cancellationToken.ThrowIfCancellationRequested();
            string? id = idSelector(item)?.Trim();
            if (string.IsNullOrWhiteSpace(id))
                continue;

            if (await TryUpsertAsync(resource, id, item, cancellationToken))
                uploaded++;
        }
        return uploaded;
    }

    public static async Task<bool> TryDeleteAsync(
        string resource,
        string recordId,
        CancellationToken cancellationToken = default)
    {
        if (_client is null || _stateStore is null || string.IsNullOrWhiteSpace(recordId))
            return false;

        string id = recordId.Trim();
        try
        {
            await _client.DeleteAsync(resource, id, cancellationToken);
            return true;
        }
        catch (Exception ex) when (ex is CloudApiException or HttpRequestException or TaskCanceledException)
        {
            await _stateStore.EnqueueAsync<object?>("DELETE", resource, id, null, cancellationToken);
            return false;
        }
    }
}

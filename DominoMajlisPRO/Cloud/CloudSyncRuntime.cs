using System.Collections.Concurrent;
using System.Net;

namespace DominoMajlisPRO.Cloud;

public static class CloudSyncRuntime
{
    private static readonly ConcurrentDictionary<string, SemaphoreSlim> ResourceGates = new(StringComparer.OrdinalIgnoreCase);
    private static CloudApiClient? _client;

    public static void Configure(CloudApiClient client)
    {
        _client = client ?? throw new ArgumentNullException(nameof(client));
    }

    public static bool IsConfigured => _client is not null;

    public static async Task<bool> TryUpsertAsync<T>(
        string resource,
        string recordId,
        T payload,
        CancellationToken cancellationToken = default)
    {
        if (_client is null || string.IsNullOrWhiteSpace(recordId))
            return false;

        var gate = ResourceGates.GetOrAdd(resource, _ => new SemaphoreSlim(1, 1));
        await gate.WaitAsync(cancellationToken);
        try
        {
            await _client.UpsertAsync(resource, recordId.Trim(), payload, cancellationToken);
            return true;
        }
        catch (CloudApiException ex) when (ex.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.ServiceUnavailable)
        {
            return false;
        }
        catch (HttpRequestException)
        {
            return false;
        }
        catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
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
        if (_client is null || string.IsNullOrWhiteSpace(recordId))
            return false;

        try
        {
            await _client.DeleteAsync(resource, recordId.Trim(), cancellationToken);
            return true;
        }
        catch (CloudApiException ex) when (ex.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.ServiceUnavailable)
        {
            return false;
        }
        catch (HttpRequestException)
        {
            return false;
        }
        catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return false;
        }
    }
}

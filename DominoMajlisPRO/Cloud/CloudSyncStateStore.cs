using System.Text.Json;

namespace DominoMajlisPRO.Cloud;

public sealed record CloudRecordState(long Revision, DateTimeOffset UpdatedAt);

public sealed record CloudPendingOperation(
    string OperationId,
    string Method,
    string Resource,
    string RecordId,
    JsonElement? Payload,
    DateTimeOffset CreatedAt,
    int Attempts = 0);

public sealed class CloudSyncStateStore
{
    private const string StateFileName = "cloud_sync_state.json";
    private const string QueueFileName = "cloud_sync_queue.json";
    private readonly SemaphoreSlim _gate = new(1, 1);
    private readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true
    };

    private static string StatePath => Path.Combine(FileSystem.AppDataDirectory, StateFileName);
    private static string QueuePath => Path.Combine(FileSystem.AppDataDirectory, QueueFileName);

    public async Task<Dictionary<string, CloudRecordState>> LoadStatesAsync(CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            return await ReadAsync<Dictionary<string, CloudRecordState>>(StatePath, cancellationToken)
                ?? new(StringComparer.OrdinalIgnoreCase);
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task SaveRecordStateAsync(
        string resource,
        string recordId,
        long revision,
        DateTimeOffset updatedAt,
        CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            var states = await ReadAsync<Dictionary<string, CloudRecordState>>(StatePath, cancellationToken)
                ?? new(StringComparer.OrdinalIgnoreCase);
            states[Key(resource, recordId)] = new CloudRecordState(revision, updatedAt);
            await WriteAsync(StatePath, states, cancellationToken);
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<CloudRecordState?> GetRecordStateAsync(
        string resource,
        string recordId,
        CancellationToken cancellationToken = default)
    {
        var states = await LoadStatesAsync(cancellationToken);
        return states.TryGetValue(Key(resource, recordId), out var state) ? state : null;
    }

    public async Task EnqueueAsync<T>(
        string method,
        string resource,
        string recordId,
        T? payload,
        CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            var queue = await ReadAsync<List<CloudPendingOperation>>(QueuePath, cancellationToken) ?? [];
            JsonElement? element = payload is null
                ? null
                : JsonSerializer.SerializeToElement(payload, _jsonOptions);

            queue.RemoveAll(item =>
                string.Equals(item.Resource, resource, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(item.RecordId, recordId, StringComparison.OrdinalIgnoreCase));

            queue.Add(new CloudPendingOperation(
                Guid.NewGuid().ToString("N"),
                method,
                resource,
                recordId,
                element,
                DateTimeOffset.UtcNow));

            await WriteAsync(QueuePath, queue, cancellationToken);
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<List<CloudPendingOperation>> LoadQueueAsync(CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            return await ReadAsync<List<CloudPendingOperation>>(QueuePath, cancellationToken) ?? [];
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task ReplaceQueueAsync(
        IEnumerable<CloudPendingOperation> operations,
        CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            await WriteAsync(QueuePath, operations.ToList(), cancellationToken);
        }
        finally
        {
            _gate.Release();
        }
    }

    private async Task<T?> ReadAsync<T>(string path, CancellationToken cancellationToken)
    {
        if (!File.Exists(path))
            return default;
        try
        {
            await using var stream = File.OpenRead(path);
            return await JsonSerializer.DeserializeAsync<T>(stream, _jsonOptions, cancellationToken);
        }
        catch (JsonException)
        {
            return default;
        }
    }

    private async Task WriteAsync<T>(string path, T value, CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(FileSystem.AppDataDirectory);
        string tempPath = path + ".tmp";
        await using (var stream = File.Create(tempPath))
            await JsonSerializer.SerializeAsync(stream, value, _jsonOptions, cancellationToken);
        File.Move(tempPath, path, true);
    }

    private static string Key(string resource, string recordId) =>
        $"{resource.Trim().ToLowerInvariant()}::{recordId.Trim()}";
}

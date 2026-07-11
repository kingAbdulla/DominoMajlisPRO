using System.Text.Json;

namespace DominoMajlisPRO.Cloud;

public sealed record CloudSession(
    string AccessToken,
    string RefreshToken,
    DateTimeOffset ExpiresAt,
    DateTimeOffset RefreshExpiresAt,
    string ApplicationUserId,
    string PlayerId,
    string DisplayName,
    string DeviceId)
{
    public bool IsAccessExpired => ExpiresAt <= DateTimeOffset.UtcNow.AddMinutes(1);
    public bool IsRefreshExpired => RefreshExpiresAt <= DateTimeOffset.UtcNow;
}

public sealed class CloudSessionStore
{
    private const string SessionFileName = "cloud_session.json";
    private readonly SemaphoreSlim _gate = new(1, 1);
    private readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true
    };

    private static string SessionFilePath =>
        Path.Combine(FileSystem.AppDataDirectory, SessionFileName);

    public async Task<CloudSession?> LoadAsync(CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            if (!File.Exists(SessionFilePath))
                return null;

            await using var stream = File.OpenRead(SessionFilePath);
            var session = await JsonSerializer.DeserializeAsync<CloudSession>(
                stream,
                _jsonOptions,
                cancellationToken);

            if (session is null || session.IsRefreshExpired)
            {
                DeleteUnsafe();
                return null;
            }

            return session;
        }
        catch (JsonException)
        {
            DeleteUnsafe();
            return null;
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task SaveAsync(CloudSession session, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(session);

        await _gate.WaitAsync(cancellationToken);
        try
        {
            Directory.CreateDirectory(FileSystem.AppDataDirectory);
            string tempPath = SessionFilePath + ".tmp";

            await using (var stream = File.Create(tempPath))
            {
                await JsonSerializer.SerializeAsync(
                    stream,
                    session,
                    _jsonOptions,
                    cancellationToken);
            }

            File.Move(tempPath, SessionFilePath, overwrite: true);
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task ClearAsync(CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            DeleteUnsafe();
        }
        finally
        {
            _gate.Release();
        }
    }

    private static void DeleteUnsafe()
    {
        if (File.Exists(SessionFilePath))
            File.Delete(SessionFilePath);
    }
}

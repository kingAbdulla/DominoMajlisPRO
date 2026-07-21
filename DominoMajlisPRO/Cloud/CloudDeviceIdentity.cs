namespace DominoMajlisPRO.Cloud;

public sealed class CloudDeviceIdentity
{
    private const string DeviceFileName = "cloud_device_id.txt";
    private readonly SemaphoreSlim _gate = new(1, 1);
    private string? _cached;

    private static string DeviceFilePath =>
        Path.Combine(FileSystem.AppDataDirectory, DeviceFileName);

    public async Task<string> GetAsync(CancellationToken cancellationToken = default)
    {
        if (!string.IsNullOrWhiteSpace(_cached))
            return _cached;

        await _gate.WaitAsync(cancellationToken);
        try
        {
            if (!string.IsNullOrWhiteSpace(_cached))
                return _cached;

            if (File.Exists(DeviceFilePath))
            {
                string existing = (await File.ReadAllTextAsync(DeviceFilePath, cancellationToken)).Trim();
                if (!string.IsNullOrWhiteSpace(existing))
                    return _cached = existing;
            }

            Directory.CreateDirectory(FileSystem.AppDataDirectory);
            _cached = $"DEV-{Guid.NewGuid():N}".ToUpperInvariant();
            await File.WriteAllTextAsync(DeviceFilePath, _cached, cancellationToken);
            return _cached;
        }
        finally
        {
            _gate.Release();
        }
    }
}

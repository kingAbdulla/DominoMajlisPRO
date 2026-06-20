using System.Collections.Concurrent;
using System.Text.Encodings.Web;
using System.Text.Json;

namespace DominoMajlisPRO.GalleryEngine.Admin.Core;

public static class StoreCmsJsonRepository
{
    private static readonly ConcurrentDictionary<string, SemaphoreSlim>
        SaveLocks = new(StringComparer.OrdinalIgnoreCase);

    public static readonly JsonSerializerOptions Options = new() { WriteIndented = true, Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping };

    public static async Task<List<T>> LoadListAsync<T>(string path, Func<JsonElement, List<T>>? migrate = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        try
        {
            if (!File.Exists(path)) return new();
            await using var stream = File.OpenRead(path);
            if (stream.Length == 0) return new();
            using var document = await JsonDocument.ParseAsync(stream);
            return document.RootElement.ValueKind == JsonValueKind.Array
                ? document.RootElement.Deserialize<List<T>>(Options) ?? new()
                : migrate?.Invoke(document.RootElement) ?? new();
        }
        catch (JsonException) { return new(); }
        catch (IOException) { return new(); }
        catch (UnauthorizedAccessException) { return new(); }
    }

    public static async Task SaveListAsync<T>(string path, IReadOnlyList<T> records)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        ArgumentNullException.ThrowIfNull(records);

        var filePath = Path.GetFullPath(path);
        var directory = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrWhiteSpace(directory))
            Directory.CreateDirectory(directory);

        var saveLock = SaveLocks.GetOrAdd(
            filePath,
            static _ => new SemaphoreSlim(1, 1));
        await saveLock.WaitAsync();

        var temporaryPath =
            $"{filePath}.{Guid.NewGuid():N}.tmp";
        try
        {
            await using (var stream = new FileStream(
                             temporaryPath,
                             FileMode.CreateNew,
                             FileAccess.Write,
                             FileShare.None,
                             4096,
                             FileOptions.Asynchronous | FileOptions.WriteThrough))
            {
                await JsonSerializer.SerializeAsync(
                    stream,
                    records,
                    Options);
                await stream.FlushAsync();
                stream.Flush(flushToDisk: true);
            }

            if (!File.Exists(temporaryPath))
            {
                throw new IOException(
                    $"JSON save failed because the temporary file was not created. " +
                    $"Destination: '{filePath}'. Temporary file: '{temporaryPath}'.");
            }

            await ValidateTemporaryJsonArrayAsync(temporaryPath);
            PreserveCorruptTarget(filePath);

            // On Android this maps to an atomic rename in the same directory.
            // The previous valid file remains untouched until the completed,
            // validated temporary file is ready to replace it.
            File.Move(temporaryPath, filePath, overwrite: true);
        }
        finally
        {
            try
            {
                if (File.Exists(temporaryPath))
                    File.Delete(temporaryPath);
            }
            finally
            {
                saveLock.Release();
            }
        }
    }

    private static async Task ValidateTemporaryJsonArrayAsync(string temporaryPath)
    {
        await using var stream = File.OpenRead(temporaryPath);
        if (stream.Length == 0)
            throw new InvalidDataException("The temporary JSON file is empty.");

        using var document = await JsonDocument.ParseAsync(stream);
        if (document.RootElement.ValueKind != JsonValueKind.Array)
            throw new InvalidDataException("The temporary JSON file must contain an array.");
    }

    private static void PreserveCorruptTarget(string filePath)
    {
        if (!File.Exists(filePath) || new FileInfo(filePath).Length == 0)
            return;

        try
        {
            using var stream = File.OpenRead(filePath);
            using var _ = JsonDocument.Parse(stream);
        }
        catch (JsonException)
        {
            var backupPath = $"{filePath}.corrupt.{Guid.NewGuid():N}.bak";
            File.Copy(filePath, backupPath, overwrite: false);
        }
    }
}

using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace DominoMajlisPRO.Cloud;

public sealed record CloudUser(
    string ApplicationUserId,
    string PlayerId,
    string DisplayName);

public sealed record CloudAuthResponse(
    string AccessToken,
    DateTimeOffset ExpiresAt,
    CloudUser User);

public sealed record CloudSyncRecord<T>(
    string RecordId,
    T Payload,
    long Revision,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    DateTimeOffset? DeletedAt);

public sealed class CloudApiException : Exception
{
    public CloudApiException(HttpStatusCode statusCode, string message)
        : base(message)
    {
        StatusCode = statusCode;
    }

    public HttpStatusCode StatusCode { get; }
}

public sealed class CloudApiClient
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly HttpClient _httpClient;
    private readonly CloudSessionStore _sessionStore;

    public CloudApiClient(HttpClient httpClient, CloudSessionStore sessionStore)
    {
        _httpClient = httpClient;
        _sessionStore = sessionStore;
    }

    public async Task<bool> IsHealthyAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            using var response = await _httpClient.GetAsync("api/health", cancellationToken);
            return response.IsSuccessStatusCode;
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

    public Task<CloudSession> RegisterAsync(
        string username,
        string password,
        CancellationToken cancellationToken = default) =>
        AuthenticateAsync("api/preview/register", username, password, cancellationToken);

    public Task<CloudSession> LoginAsync(
        string username,
        string password,
        CancellationToken cancellationToken = default) =>
        AuthenticateAsync("api/preview/login", username, password, cancellationToken);

    public async Task LogoutAsync(CancellationToken cancellationToken = default)
    {
        var session = await _sessionStore.LoadAsync(cancellationToken);
        if (session is not null)
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, "api/preview/logout");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", session.AccessToken);

            try
            {
                using var response = await _httpClient.SendAsync(request, cancellationToken);
                if (!response.IsSuccessStatusCode && response.StatusCode != HttpStatusCode.Unauthorized)
                    await ThrowApiExceptionAsync(response, cancellationToken);
            }
            catch (HttpRequestException)
            {
                // Local logout must still succeed when the device is offline.
            }
        }

        await _sessionStore.ClearAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<CloudSyncRecord<T>>> GetAllAsync<T>(
        string resource,
        DateTimeOffset? since = null,
        bool includeDeleted = false,
        CancellationToken cancellationToken = default)
    {
        string path = $"api/v1/{NormalizeResource(resource)}";
        var query = new List<string>();
        if (since.HasValue)
            query.Add($"since={Uri.EscapeDataString(since.Value.UtcDateTime.ToString("O"))}");
        if (includeDeleted)
            query.Add("includeDeleted=true");
        if (query.Count > 0)
            path += "?" + string.Join("&", query);

        using var request = await CreateAuthorizedRequestAsync(HttpMethod.Get, path, null, cancellationToken);
        using var response = await _httpClient.SendAsync(request, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);

        return await response.Content.ReadFromJsonAsync<List<CloudSyncRecord<T>>>(JsonOptions, cancellationToken)
            ?? [];
    }

    public async Task<CloudSyncRecord<T>?> GetAsync<T>(
        string resource,
        string recordId,
        CancellationToken cancellationToken = default)
    {
        using var request = await CreateAuthorizedRequestAsync(
            HttpMethod.Get,
            $"api/v1/{NormalizeResource(resource)}/{Uri.EscapeDataString(recordId)}",
            null,
            cancellationToken);
        using var response = await _httpClient.SendAsync(request, cancellationToken);

        if (response.StatusCode == HttpStatusCode.NotFound)
            return null;

        await EnsureSuccessAsync(response, cancellationToken);
        return await response.Content.ReadFromJsonAsync<CloudSyncRecord<T>>(JsonOptions, cancellationToken);
    }

    public async Task<CloudSyncRecord<T>> CreateAsync<T>(
        string resource,
        string recordId,
        T payload,
        CancellationToken cancellationToken = default)
    {
        var body = new { recordId, payload };
        using var request = await CreateAuthorizedRequestAsync(
            HttpMethod.Post,
            $"api/v1/{NormalizeResource(resource)}",
            body,
            cancellationToken);
        using var response = await _httpClient.SendAsync(request, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);

        return (await response.Content.ReadFromJsonAsync<CloudSyncRecord<T>>(JsonOptions, cancellationToken))
            ?? throw new CloudApiException(response.StatusCode, "Cloud API returned an empty response.");
    }

    public async Task<CloudSyncRecord<T>> UpsertAsync<T>(
        string resource,
        string recordId,
        T payload,
        CancellationToken cancellationToken = default)
    {
        var body = new { payload };
        using var request = await CreateAuthorizedRequestAsync(
            HttpMethod.Put,
            $"api/v1/{NormalizeResource(resource)}/{Uri.EscapeDataString(recordId)}",
            body,
            cancellationToken);
        using var response = await _httpClient.SendAsync(request, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);

        return (await response.Content.ReadFromJsonAsync<CloudSyncRecord<T>>(JsonOptions, cancellationToken))
            ?? throw new CloudApiException(response.StatusCode, "Cloud API returned an empty response.");
    }

    public async Task DeleteAsync(
        string resource,
        string recordId,
        CancellationToken cancellationToken = default)
    {
        using var request = await CreateAuthorizedRequestAsync(
            HttpMethod.Delete,
            $"api/v1/{NormalizeResource(resource)}/{Uri.EscapeDataString(recordId)}",
            null,
            cancellationToken);
        using var response = await _httpClient.SendAsync(request, cancellationToken);

        if (response.StatusCode == HttpStatusCode.NotFound)
            return;

        await EnsureSuccessAsync(response, cancellationToken);
    }

    private async Task<CloudSession> AuthenticateAsync(
        string path,
        string username,
        string password,
        CancellationToken cancellationToken)
    {
        using var response = await _httpClient.PostAsJsonAsync(
            path,
            new { username = username.Trim(), password },
            JsonOptions,
            cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);

        var result = await response.Content.ReadFromJsonAsync<CloudAuthResponse>(JsonOptions, cancellationToken)
            ?? throw new CloudApiException(response.StatusCode, "Cloud API returned an empty authentication response.");

        var session = new CloudSession(
            result.AccessToken,
            result.ExpiresAt,
            result.User.ApplicationUserId,
            result.User.PlayerId,
            result.User.DisplayName);
        await _sessionStore.SaveAsync(session, cancellationToken);
        return session;
    }

    private async Task<HttpRequestMessage> CreateAuthorizedRequestAsync(
        HttpMethod method,
        string path,
        object? body,
        CancellationToken cancellationToken)
    {
        var session = await _sessionStore.LoadAsync(cancellationToken)
            ?? throw new CloudApiException(HttpStatusCode.Unauthorized, "No active cloud session is available.");

        var request = new HttpRequestMessage(method, path);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", session.AccessToken);
        if (body is not null)
            request.Content = JsonContent.Create(body, options: JsonOptions);
        return request;
    }

    private async Task EnsureSuccessAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        if (response.IsSuccessStatusCode)
            return;

        if (response.StatusCode == HttpStatusCode.Unauthorized)
            await _sessionStore.ClearAsync(cancellationToken);

        await ThrowApiExceptionAsync(response, cancellationToken);
    }

    private static async Task ThrowApiExceptionAsync(
        HttpResponseMessage response,
        CancellationToken cancellationToken)
    {
        string message = response.ReasonPhrase ?? "Cloud API request failed.";
        try
        {
            using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync(cancellationToken));
            if (document.RootElement.TryGetProperty("message", out var element))
                message = element.GetString() ?? message;
        }
        catch (JsonException)
        {
        }

        throw new CloudApiException(response.StatusCode, message);
    }

    private static string NormalizeResource(string resource)
    {
        if (string.IsNullOrWhiteSpace(resource))
            throw new ArgumentException("Resource name is required.", nameof(resource));

        return resource.Trim().ToLowerInvariant();
    }
}

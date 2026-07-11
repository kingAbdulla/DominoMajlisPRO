using System.Security.Cryptography;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);
var allowedOrigins = builder.Configuration.GetSection("AllowedOrigins").Get<string[]>() ??
    ["https://kingabdulla.github.io"];

builder.Services.AddCors(options => options.AddDefaultPolicy(policy =>
    policy.WithOrigins(allowedOrigins).AllowAnyHeader().AllowAnyMethod()));
builder.Services.AddSingleton<PreviewStore>();
builder.Services.AddSingleton<PreviewSessionService>();

var app = builder.Build();
app.UseDefaultFiles();
app.UseStaticFiles();
app.UseCors();

app.MapGet("/api/health", () => Results.Ok(new
{
    service = "DominoMajlisPRO.Api",
    status = "healthy",
    persistence = "json-file",
    authentication = "bearer-preview-token",
    webPreview = "same-origin",
    utc = DateTimeOffset.UtcNow
}));

app.MapPost("/api/preview/register", async (RegisterRequest request, PreviewStore store, PreviewSessionService sessions) =>
{
    var username = request.Username?.Trim() ?? string.Empty;
    if (username.Length < 3 || string.IsNullOrWhiteSpace(request.Password) || request.Password.Length < 8)
        return Results.BadRequest(new { message = "اسم المستخدم يجب أن يكون 3 أحرف على الأقل وكلمة المرور 8 أحرف على الأقل." });

    var user = await store.RegisterAsync(username, request.Password);
    return user is null
        ? Results.Conflict(new { message = "اسم المستخدم مستخدم بالفعل." })
        : Results.Ok(sessions.Create(user));
});

app.MapPost("/api/preview/login", async (LoginRequest request, PreviewStore store, PreviewSessionService sessions) =>
{
    var user = await store.ValidateCredentialsAsync(request.Username?.Trim() ?? string.Empty, request.Password ?? string.Empty);
    return user is null
        ? Results.Json(new { message = "بيانات الدخول غير صحيحة." }, statusCode: StatusCodes.Status401Unauthorized)
        : Results.Ok(sessions.Create(user));
});

app.MapPost("/api/preview/logout", (HttpRequest request, PreviewSessionService sessions) =>
{
    var token = PreviewSessionService.ReadBearerToken(request);
    if (token is null) return Results.Unauthorized();
    sessions.Revoke(token);
    return Results.NoContent();
});

app.MapGet("/api/preview/me/teams", async (HttpRequest request, PreviewSessionService sessions, PreviewStore store) =>
{
    var user = sessions.Resolve(request);
    return user is null
        ? Results.Unauthorized()
        : Results.Ok(await store.GetTeamsAsync(user.ApplicationUserId));
});

app.MapPost("/api/preview/me/teams", async (HttpRequest request, CreateTeamRequest body, PreviewSessionService sessions, PreviewStore store) =>
{
    var user = sessions.Resolve(request);
    if (user is null) return Results.Unauthorized();
    if (string.IsNullOrWhiteSpace(body.Name))
        return Results.BadRequest(new { message = "اسم الفريق مطلوب." });

    var team = await store.CreateTeamAsync(user.ApplicationUserId, body.Name.Trim());
    return Results.Created($"/api/preview/me/teams/{team.TeamId}", team);
});

app.Run();

sealed class PreviewSessionService
{
    private readonly Dictionary<string, PreviewSession> _sessions = new(StringComparer.Ordinal);
    private readonly object _gate = new();
    private static readonly TimeSpan Lifetime = TimeSpan.FromHours(12);

    public SessionResponse Create(PreviewUser user)
    {
        var token = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
        lock (_gate)
        {
            RemoveExpiredUnsafe();
            _sessions[token] = new PreviewSession(user, DateTimeOffset.UtcNow.Add(Lifetime));
        }
        return new SessionResponse(token, DateTimeOffset.UtcNow.Add(Lifetime), user);
    }

    public PreviewUser? Resolve(HttpRequest request)
    {
        var token = ReadBearerToken(request);
        if (token is null) return null;
        lock (_gate)
        {
            RemoveExpiredUnsafe();
            return _sessions.TryGetValue(token, out var session) ? session.User : null;
        }
    }

    public void Revoke(string token)
    {
        lock (_gate) _sessions.Remove(token);
    }

    public static string? ReadBearerToken(HttpRequest request)
    {
        var value = request.Headers.Authorization.ToString();
        const string prefix = "Bearer ";
        return value.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)
            ? value[prefix.Length..].Trim()
            : null;
    }

    private void RemoveExpiredUnsafe()
    {
        var now = DateTimeOffset.UtcNow;
        foreach (var token in _sessions.Where(x => x.Value.ExpiresAt <= now).Select(x => x.Key).ToList())
            _sessions.Remove(token);
    }
}

sealed class PreviewStore
{
    private readonly SemaphoreSlim _gate = new(1, 1);
    private readonly string _filePath;
    private readonly JsonSerializerOptions _json = new(JsonSerializerDefaults.Web) { WriteIndented = true };

    public PreviewStore(IWebHostEnvironment environment, IConfiguration configuration)
    {
        var dataDirectory = configuration["DataDirectory"];
        if (string.IsNullOrWhiteSpace(dataDirectory))
            dataDirectory = Path.Combine(environment.ContentRootPath, "App_Data");
        Directory.CreateDirectory(dataDirectory);
        _filePath = Path.Combine(dataDirectory, "preview-store.json");
    }

    public async Task<PreviewUser?> RegisterAsync(string username, string password)
    {
        await _gate.WaitAsync();
        try
        {
            var state = await LoadAsync();
            if (state.Users.Any(x => string.Equals(x.Username, username, StringComparison.OrdinalIgnoreCase)))
                return null;

            var salt = RandomNumberGenerator.GetBytes(16);
            var hash = Rfc2898DeriveBytes.Pbkdf2(password, salt, 120_000, HashAlgorithmName.SHA256, 32);
            var user = new StoredUser(
                $"USR-{Guid.NewGuid():N}".ToUpperInvariant(),
                $"PLY-{Guid.NewGuid():N}".ToUpperInvariant(),
                username,
                Convert.ToBase64String(salt),
                Convert.ToBase64String(hash));
            state.Users.Add(user);
            await SaveAsync(state);
            return ToPreviewUser(user);
        }
        finally { _gate.Release(); }
    }

    public async Task<PreviewUser?> ValidateCredentialsAsync(string username, string password)
    {
        await _gate.WaitAsync();
        try
        {
            var state = await LoadAsync();
            var user = state.Users.FirstOrDefault(x => string.Equals(x.Username, username, StringComparison.OrdinalIgnoreCase));
            if (user is null) return null;
            var salt = Convert.FromBase64String(user.PasswordSalt);
            var expected = Convert.FromBase64String(user.PasswordHash);
            var actual = Rfc2898DeriveBytes.Pbkdf2(password, salt, 120_000, HashAlgorithmName.SHA256, 32);
            return CryptographicOperations.FixedTimeEquals(expected, actual) ? ToPreviewUser(user) : null;
        }
        finally { _gate.Release(); }
    }

    public async Task<IReadOnlyList<PreviewTeam>> GetTeamsAsync(string applicationUserId)
    {
        await _gate.WaitAsync();
        try
        {
            var state = await LoadAsync();
            return state.Teams.Where(x => x.ApplicationUserId == applicationUserId)
                .Select(x => new PreviewTeam(x.TeamId, x.Name, x.CreatedAt)).ToList();
        }
        finally { _gate.Release(); }
    }

    public async Task<PreviewTeam> CreateTeamAsync(string applicationUserId, string name)
    {
        await _gate.WaitAsync();
        try
        {
            var state = await LoadAsync();
            var stored = new StoredTeam($"TEAM-{Guid.NewGuid():N}".ToUpperInvariant(), applicationUserId, name, DateTimeOffset.UtcNow);
            state.Teams.Add(stored);
            await SaveAsync(state);
            return new PreviewTeam(stored.TeamId, stored.Name, stored.CreatedAt);
        }
        finally { _gate.Release(); }
    }

    private async Task<StoreState> LoadAsync()
    {
        if (!File.Exists(_filePath)) return new StoreState();
        await using var stream = File.OpenRead(_filePath);
        return await JsonSerializer.DeserializeAsync<StoreState>(stream, _json) ?? new StoreState();
    }

    private async Task SaveAsync(StoreState state)
    {
        var temp = _filePath + ".tmp";
        await using (var stream = File.Create(temp))
            await JsonSerializer.SerializeAsync(stream, state, _json);
        File.Move(temp, _filePath, true);
    }

    private static PreviewUser ToPreviewUser(StoredUser user) =>
        new(user.ApplicationUserId, user.PlayerId, user.Username);
}

sealed class StoreState
{
    public List<StoredUser> Users { get; set; } = [];
    public List<StoredTeam> Teams { get; set; } = [];
}

record RegisterRequest(string? Username, string? Password);
record LoginRequest(string? Username, string? Password);
record CreateTeamRequest(string? Name);
record StoredUser(string ApplicationUserId, string PlayerId, string Username, string PasswordSalt, string PasswordHash);
record StoredTeam(string TeamId, string ApplicationUserId, string Name, DateTimeOffset CreatedAt);
record PreviewUser(string ApplicationUserId, string PlayerId, string DisplayName);
record PreviewTeam(string TeamId, string Name, DateTimeOffset CreatedAt);
record PreviewSession(PreviewUser User, DateTimeOffset ExpiresAt);
record SessionResponse(string AccessToken, DateTimeOffset ExpiresAt, PreviewUser User);
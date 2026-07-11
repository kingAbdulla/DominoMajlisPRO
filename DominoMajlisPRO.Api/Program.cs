using System.Security.Cryptography;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);
var allowedOrigins = builder.Configuration.GetSection("AllowedOrigins").Get<string[]>() ??
    ["https://kingabdulla.github.io"];

builder.Services.AddCors(options => options.AddDefaultPolicy(policy =>
    policy.WithOrigins(allowedOrigins).AllowAnyHeader().AllowAnyMethod()));
builder.Services.AddSingleton<PreviewStore>();

var app = builder.Build();
app.UseCors();

app.MapGet("/api/health", () => Results.Ok(new
{
    service = "DominoMajlisPRO.Api",
    status = "healthy",
    persistence = "json-file",
    utc = DateTimeOffset.UtcNow
}));

app.MapPost("/api/preview/register", async (RegisterRequest request, PreviewStore store) =>
{
    var username = request.Username?.Trim() ?? string.Empty;
    if (username.Length < 3 || string.IsNullOrWhiteSpace(request.Password) || request.Password.Length < 8)
        return Results.BadRequest(new { message = "اسم المستخدم يجب أن يكون 3 أحرف على الأقل وكلمة المرور 8 أحرف على الأقل." });

    var result = await store.RegisterAsync(username, request.Password);
    return result is null
        ? Results.Conflict(new { message = "اسم المستخدم مستخدم بالفعل." })
        : Results.Ok(result);
});

app.MapPost("/api/preview/login", async (LoginRequest request, PreviewStore store) =>
{
    var session = await store.LoginAsync(request.Username?.Trim() ?? string.Empty, request.Password ?? string.Empty);
    return session is null
        ? Results.Unauthorized()
        : Results.Ok(session);
});

app.MapGet("/api/preview/users/{applicationUserId}/teams", async (string applicationUserId, PreviewStore store) =>
    Results.Ok(await store.GetTeamsAsync(applicationUserId)));

app.MapPost("/api/preview/users/{applicationUserId}/teams", async (string applicationUserId, CreateTeamRequest request, PreviewStore store) =>
{
    if (string.IsNullOrWhiteSpace(request.Name))
        return Results.BadRequest(new { message = "اسم الفريق مطلوب." });

    var team = await store.CreateTeamAsync(applicationUserId, request.Name.Trim());
    return team is null
        ? Results.NotFound(new { message = "الحساب التجريبي غير موجود." })
        : Results.Created($"/api/preview/users/{applicationUserId}/teams/{team.TeamId}", team);
});

app.Run();

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

    public async Task<SessionResponse?> RegisterAsync(string username, string password)
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
            return CreateSession(user);
        }
        finally { _gate.Release(); }
    }

    public async Task<SessionResponse?> LoginAsync(string username, string password)
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
            return CryptographicOperations.FixedTimeEquals(expected, actual) ? CreateSession(user) : null;
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

    public async Task<PreviewTeam?> CreateTeamAsync(string applicationUserId, string name)
    {
        await _gate.WaitAsync();
        try
        {
            var state = await LoadAsync();
            if (!state.Users.Any(x => x.ApplicationUserId == applicationUserId)) return null;
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

    private static SessionResponse CreateSession(StoredUser user) => new(
        Convert.ToHexString(RandomNumberGenerator.GetBytes(32)),
        new PreviewUser(user.ApplicationUserId, user.PlayerId, user.Username));
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
record SessionResponse(string AccessToken, PreviewUser User);

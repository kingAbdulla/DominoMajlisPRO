using System.Collections.Concurrent;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddCors(options => options.AddDefaultPolicy(policy =>
    policy.AllowAnyHeader().AllowAnyMethod().SetIsOriginAllowed(_ => true)));

var app = builder.Build();
app.UseHttpsRedirection();
app.UseCors();

var users = new ConcurrentDictionary<string, PreviewUser>(StringComparer.OrdinalIgnoreCase);
var teams = new ConcurrentDictionary<string, List<PreviewTeam>>(StringComparer.OrdinalIgnoreCase);

app.MapGet("/api/health", () => Results.Ok(new
{
    service = "DominoMajlisPRO.Api",
    status = "healthy",
    utc = DateTimeOffset.UtcNow
}));

app.MapPost("/api/preview/register", (RegisterRequest request) =>
{
    var username = request.Username?.Trim() ?? string.Empty;
    if (username.Length < 3 || string.IsNullOrWhiteSpace(request.Password))
        return Results.BadRequest(new { message = "بيانات التسجيل غير مكتملة." });

    var user = users.GetOrAdd(username, _ => new PreviewUser(
        $"USR-{Guid.NewGuid():N}".ToUpperInvariant(),
        $"PLY-{Guid.NewGuid():N}".ToUpperInvariant(),
        username));

    teams.TryAdd(user.ApplicationUserId, []);
    return Results.Ok(new SessionResponse(Guid.NewGuid().ToString("N"), user));
});

app.MapPost("/api/preview/login", (LoginRequest request) =>
{
    var username = request.Username?.Trim() ?? string.Empty;
    if (!users.TryGetValue(username, out var user))
        return Results.NotFound(new { message = "الحساب التجريبي غير موجود." });

    return Results.Ok(new SessionResponse(Guid.NewGuid().ToString("N"), user));
});

app.MapGet("/api/preview/users/{applicationUserId}/teams", (string applicationUserId) =>
    Results.Ok(teams.GetOrAdd(applicationUserId, [])));

app.MapPost("/api/preview/users/{applicationUserId}/teams", (string applicationUserId, CreateTeamRequest request) =>
{
    if (string.IsNullOrWhiteSpace(request.Name))
        return Results.BadRequest(new { message = "اسم الفريق مطلوب." });

    var list = teams.GetOrAdd(applicationUserId, []);
    var team = new PreviewTeam($"TEAM-{Guid.NewGuid():N}".ToUpperInvariant(), request.Name.Trim(), DateTimeOffset.UtcNow);
    lock (list) list.Add(team);
    return Results.Created($"/api/preview/users/{applicationUserId}/teams/{team.TeamId}", team);
});

app.Run();

record RegisterRequest(string? Username, string? Password);
record LoginRequest(string? Username, string? Password);
record CreateTeamRequest(string? Name);
record PreviewUser(string ApplicationUserId, string PlayerId, string DisplayName);
record PreviewTeam(string TeamId, string Name, DateTimeOffset CreatedAt);
record SessionResponse(string AccessToken, PreviewUser User);

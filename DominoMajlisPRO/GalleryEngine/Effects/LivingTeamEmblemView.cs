using DominoMajlisPRO.GalleryEngine.Services;
using DominoMajlisPRO.Services;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;

namespace DominoMajlisPRO.GalleryEngine.Effects;

// ──────────────────────────────────────────────────────────────────
// EmblemAnimProfile  — per-emblem calm breathing/glow parameters
// ──────────────────────────────────────────────────────────────────
internal sealed record EmblemAnimProfile(
    Color PrimaryGlow,
    Color SecondaryGlow,
    float GlowAlpha,      // 0-1  (calm 80 %)
    float PulseAmplitude, // 0-1  (subtle)
    float Speed,          // breathing cycles / second
    string FutureBehavior)
{
    // Emblem-specific mapping — future behaviours noted but NOT implemented
    public static EmblemAnimProfile For(string? emblemAssetId)
    {
        var id = emblemAssetId?.Trim().ToLowerInvariant() ?? string.Empty;

        if (id.Contains("dragon"))
            return new(Color.FromArgb("#FF6B00"), Color.FromArgb("#FFD700"),
                0.22f, 0.12f, 0.70f, "FireBreath");

        if (id.Contains("lion"))
            return new(Color.FromArgb("#FFD700"), Color.FromArgb("#FFA500"),
                0.20f, 0.10f, 0.65f, "Roar");

        if (id.Contains("eagle"))
            return new(Color.FromArgb("#00BFFF"), Color.FromArgb("#FFFFFF"),
                0.18f, 0.09f, 0.80f, "WingPulse");

        if (id.Contains("wolf"))
            return new(Color.FromArgb("#9ECFFF"), Color.FromArgb("#FFFFFF"),
                0.17f, 0.09f, 0.75f, "FrostBreath");

        if (id.Contains("crown"))
            return new(Color.FromArgb("#FFD700"), Color.FromArgb("#E8C060"),
                0.24f, 0.13f, 0.60f, "RoyalSparkle");

        // shield or unknown
        return new(Color.FromArgb("#C0C8D8"), Color.FromArgb("#FFD700"),
            0.16f, 0.08f, 0.70f, "DefensivePulse");
    }
}

// ──────────────────────────────────────────────────────────────────
// LivingEmblemDrawable  — procedural breathing glow (MAUI-native)
// 80 % calm / 20 % motion, safe for Realme C33
// ──────────────────────────────────────────────────────────────────
internal sealed class LivingEmblemDrawable : IDrawable
{
    public EmblemAnimProfile? Profile { get; set; }
    public float Phase { get; set; }

    public void Draw(ICanvas canvas, RectF rect)
    {
        var p = Profile;
        if (p == null || rect.Width <= 1) return;

        canvas.SaveState();

        var cx = rect.Center.X;
        var cy = rect.Center.Y;
        var r = MathF.Min(rect.Width, rect.Height) * 0.38f;

        // Breathing pulse — very subtle
        var breath = 1f + p.PulseAmplitude * MathF.Sin(Phase * MathF.Tau);

        // Outer soft aura
        var outerR = r * 1.30f * breath;
        canvas.FillColor = p.PrimaryGlow.WithAlpha(p.GlowAlpha * 0.45f);
        canvas.FillCircle(cx, cy, outerR);

        // Inner glow ring
        var innerR = r * 1.05f * breath;
        canvas.FillColor = p.PrimaryGlow.WithAlpha(p.GlowAlpha * 0.75f);
        canvas.FillCircle(cx, cy, innerR);

        // Thin accent ring
        var ringAlpha = p.GlowAlpha * 0.90f * (0.70f + 0.30f * MathF.Abs(MathF.Sin(Phase * MathF.Tau * 0.5f)));
        canvas.StrokeColor = p.SecondaryGlow.WithAlpha(ringAlpha);
        canvas.StrokeSize = 1.4f;
        canvas.DrawCircle(cx, cy, innerR);

        canvas.RestoreState();
    }
}

// ──────────────────────────────────────────────────────────────────
// LivingTeamEmblemView  — self-animating GraphicsView overlay
// Wraps around any existing Image emblem. No XAML required.
// ──────────────────────────────────────────────────────────────────
public sealed class LivingTeamEmblemView : GraphicsView
{
    private readonly LivingEmblemDrawable _drawable = new();
    private bool _running;
    private long _started;

    public LivingTeamEmblemView()
    {
        Drawable = _drawable;
        InputTransparent = true;
        BackgroundColor = Colors.Transparent;
        IsVisible = false;
        Loaded += (_, _) => StartIfReady();
        Unloaded += (_, _) => _running = false;
    }

    public void SetEmblem(string? emblemAssetId)
    {
        var profile = EmblemAnimProfile.For(emblemAssetId);
        var key = emblemAssetId?.Trim() ?? string.Empty;

        if (_drawable.Profile?.FutureBehavior == profile.FutureBehavior &&
            string.Equals(_drawable.Profile?.PrimaryGlow.ToHex(), profile.PrimaryGlow.ToHex()))
            return;

        _drawable.Profile = profile;
        _drawable.Phase = 0f;
        _started = Environment.TickCount64;
        IsVisible = true;
        StartIfReady();
        Invalidate();
    }

    public void ClearEmblem()
    {
        _running = false;
        _drawable.Profile = null;
        IsVisible = false;
        Invalidate();
    }

    private void StartIfReady()
    {
        if (_running || _drawable.Profile == null || !IsLoaded)
            return;

        _running = true;

        // 15 fps on lightweight — safe for Realme C33
        Dispatcher.StartTimer(TimeSpan.FromMilliseconds(66), () =>
        {
            if (!_running || _drawable.Profile == null || !IsLoaded)
                return false;

            var elapsed = (Environment.TickCount64 - _started) / 1000f;
            _drawable.Phase = elapsed * _drawable.Profile.Speed;
            Invalidate();
            return true;
        });
    }
}

// ──────────────────────────────────────────────────────────────────
// LivingEmblemBehavior  — attached property, mirrors TeamEffectBehavior
// Usage (code-behind): LivingEmblemBehavior.Attach(myImage, teamId)
// ──────────────────────────────────────────────────────────────────
public static class LivingEmblemBehavior
{
    // Attached properties
    public static readonly BindableProperty TeamIdProperty =
        BindableProperty.CreateAttached(
            "TeamId", typeof(string), typeof(LivingEmblemBehavior), string.Empty,
            propertyChanged: OnTeamIdChanged);

    public static string GetTeamId(BindableObject v) => (string)v.GetValue(TeamIdProperty);
    public static void SetTeamId(BindableObject v, string val) => v.SetValue(TeamIdProperty, val);

    // Convenience: call once in code-behind after setting Image.Source
    public static void Attach(Image image, string? teamId)
    {
        if (string.IsNullOrWhiteSpace(teamId)) return;
        SetTeamId(image, teamId.Trim());
    }

    private static void OnTeamIdChanged(BindableObject bindable, object oldValue, object newValue)
    {
        if (bindable is not Image image) return;
        EnsureHooked(image);
        _ = RefreshAsync(image);
    }

    private static void EnsureHooked(Image image)
    {
        if (image.GetValue(IsHookedProperty) is true) return;
        image.SetValue(IsHookedProperty, true);
        image.Loaded += OnLoaded;
        image.Unloaded += OnUnloaded;

        Action<string> handler = changedTeamId =>
        {
            var myId = GetTeamId(image);
            if (string.IsNullOrWhiteSpace(myId) ||
                !string.Equals(myId, changedTeamId, StringComparison.OrdinalIgnoreCase))
                return;
            MainThread.BeginInvokeOnMainThread(() => _ = RefreshAsync(image));
        };

        image.SetValue(RefreshHandlerProperty, handler);
        AppEvents.TeamAssetsChanged += handler;
    }

    private static async Task RefreshAsync(Image image)
    {
        if (!image.IsLoaded) return;

        var teamId = GetTeamId(image);
        if (string.IsNullOrWhiteSpace(teamId)) return;

        try
        {
            var identity = await TeamIdentityResolver.ResolveAsync(teamId);
            var emblemAssetId = identity.EmblemAssetId;

            await MainThread.InvokeOnMainThreadAsync(() =>
                ApplyOrCreate(image, emblemAssetId));
        }
        catch { /* identity resolution failed — skip animation */ }
    }

    private static void ApplyOrCreate(Image image, string? emblemAssetId)
    {
        var holder = GetHolder(image);

        // Create view lazily
        if (holder.View == null)
        {
            if (image.Parent is not Layout parent) return;

            var view = new LivingTeamEmblemView
            {
                HorizontalOptions = LayoutOptions.Center,
                VerticalOptions = LayoutOptions.Center,
                Margin = image.Margin,
                InputTransparent = true,
                BackgroundColor = Colors.Transparent
            };

            SyncSize(image, view, 1.32);

            if (parent is Grid)
            {
                Grid.SetRow(view, Grid.GetRow(image));
                Grid.SetColumn(view, Grid.GetColumn(image));
                Grid.SetRowSpan(view, Grid.GetRowSpan(image));
                Grid.SetColumnSpan(view, Grid.GetColumnSpan(image));
            }

            view.ZIndex = Math.Max(0, image.ZIndex - 1);
            parent.Children.Add(view);
            holder.View = view;
        }

        SyncSize(image, holder.View, 1.32);
        holder.View.SetEmblem(emblemAssetId);
    }

    private static void SyncSize(Image source, LivingTeamEmblemView view, double scale)
    {
        var w = source.WidthRequest > 0 ? source.WidthRequest : source.Width;
        var h = source.HeightRequest > 0 ? source.HeightRequest : source.Height;
        if (w <= 1) w = 64;
        if (h <= 1) h = 64;
        view.WidthRequest = w * scale;
        view.HeightRequest = h * scale;
        view.MinimumWidthRequest = view.WidthRequest;
        view.MinimumHeightRequest = view.HeightRequest;
    }

    private static void OnLoaded(object? sender, EventArgs e)
    {
        if (sender is Image img) _ = RefreshAsync(img);
    }

    private static void OnUnloaded(object? sender, EventArgs e)
    {
        if (sender is not Image img) return;
        img.Loaded -= OnLoaded;
        img.Unloaded -= OnUnloaded;

        var handler = img.GetValue(RefreshHandlerProperty) as Action<string>;
        if (handler != null) AppEvents.TeamAssetsChanged -= handler;

        img.SetValue(RefreshHandlerProperty, null);
        img.SetValue(IsHookedProperty, false);

        var holder = GetHolder(img);
        holder.View?.ClearEmblem();
        holder.View = null;
    }

    // ---- storage helpers ----
    private static readonly BindableProperty IsHookedProperty =
        BindableProperty.CreateAttached("IsHooked", typeof(bool), typeof(LivingEmblemBehavior), false);

    private static readonly BindableProperty RefreshHandlerProperty =
        BindableProperty.CreateAttached("RefreshHandler", typeof(Action<string>), typeof(LivingEmblemBehavior), null);

    private static readonly BindableProperty HolderProperty =
        BindableProperty.CreateAttached("Holder", typeof(ViewHolder), typeof(LivingEmblemBehavior), null);

    private static ViewHolder GetHolder(Image image)
    {
        var h = image.GetValue(HolderProperty) as ViewHolder;
        if (h == null)
        {
            h = new ViewHolder();
            image.SetValue(HolderProperty, h);
        }
        return h;
    }

    private sealed class ViewHolder
    {
        public LivingTeamEmblemView? View;
    }
}

#if ANDROID
using Android.Content;
using Android.Graphics;
using Android.Util;
using Android.Views;
using Java.Nio;
using Com.Google.Android.Filament;
using Com.Google.Android.Filament.Gltfio;
using Com.Google.Android.Filament.Utils;
using Engine = Com.Google.Android.Filament.Engine;
using FCamera = Com.Google.Android.Filament.Camera;
using FView = Com.Google.Android.Filament.View;
using Surface = Android.Views.Surface;

namespace DominoMajlisPRO.Platforms.Android;

public sealed class FilamentRenderSurfaceView :
    SurfaceView,
    ISurfaceHolderCallback,
    Choreographer.IFrameCallback
{
    private const string LogTag = "LivingVisualFilament";
    private readonly Context _context;
    private readonly FilamentRenderer _filament = new();
    private string _assetPath = string.Empty;
    private bool _surfaceReady;
    private bool _paused = true;
    private bool _framePosted;
    private bool _assetLoadAttempted;
    private long _firstFrameNanos;

    public FilamentRenderSurfaceView(Context context)
        : base(context)
    {
        _context = context;
        Holder?.AddCallback(this);
        Holder?.SetFormat(Format.Rgba8888);
        SetZOrderOnTop(false);
        SetBackgroundColor(global::Android.Graphics.Color.Transparent);
        Log.Info(LogTag, "SurfaceView constructed.");
    }

    public void SetAssetPath(string? assetPath)
    {
        _assetPath = assetPath?.Trim() ?? string.Empty;
        _assetLoadAttempted = false;
        Log.Info(LogTag, $"Asset path set to '{_assetPath}'.");
        TryLoadAsset();
    }

    public void SetPaused(bool paused)
    {
        _paused = paused;
        Log.Info(LogTag, $"Paused={_paused}.");
        if (_paused)
            return;

        EnsureRenderer();
        TryLoadAsset();
        PostFrame();
    }

    public void SurfaceCreated(ISurfaceHolder holder)
    {
        _surfaceReady = true;
        _assetLoadAttempted = false;
        Log.Info(LogTag, "SurfaceCreated.");
        EnsureRenderer();
        TryLoadAsset();
        PostFrame();
    }

    public void SurfaceChanged(ISurfaceHolder holder, Format format, int width, int height)
    {
        Log.Info(LogTag, $"SurfaceChanged width={width}, height={height}, format={format}.");
        EnsureRenderer();
        _filament.Resize(width, height);
        TryLoadAsset(forceReload: true);
        PostFrame();
    }

    public void SurfaceDestroyed(ISurfaceHolder holder)
    {
        Log.Info(LogTag, "SurfaceDestroyed.");
        _surfaceReady = false;
        _framePosted = false;
        _filament.DestroySwapChain();
    }

    public void DoFrame(long frameTimeNanos)
    {
        _framePosted = false;
        if (_paused || !_surfaceReady)
            return;

        try
        {
            _firstFrameNanos = _firstFrameNanos == 0 ? frameTimeNanos : _firstFrameNanos;
            var seconds = (frameTimeNanos - _firstFrameNanos) / 1_000_000_000.0;
            _filament.Render(frameTimeNanos, seconds);
        }
        catch (Java.Lang.Throwable ex)
        {
            Log.Error(LogTag, ex, "Render frame failed.");
        }
        catch (System.Exception ex)
        {
            Log.Error(LogTag, ex.ToString());
        }

        PostFrame();
    }

    public void DisposeRenderer()
    {
        Log.Info(LogTag, "DisposeRenderer.");
        _paused = true;
        _framePosted = false;
        _surfaceReady = false;
        _filament.Dispose();
    }

    private void EnsureRenderer()
    {
        if (!_surfaceReady || Holder?.Surface == null || !Holder.Surface.IsValid)
        {
            Log.Info(LogTag, "EnsureRenderer skipped; surface not ready.");
            return;
        }

        _filament.Ensure(Holder.Surface, Width > 0 ? Width : MeasuredWidth, Height > 0 ? Height : MeasuredHeight);
    }

    private void TryLoadAsset(bool forceReload = false)
    {
        if (string.IsNullOrWhiteSpace(_assetPath))
            return;

        if (_assetLoadAttempted && !forceReload)
            return;

        try
        {
            EnsureRenderer();
            if (!_filament.IsReady)
            {
                Log.Info(LogTag, "Asset load deferred; Filament is not ready.");
                return;
            }

            using var stream = _context.Assets?.Open(_assetPath);
            if (stream == null)
                throw new InvalidOperationException($"Living visual package '{_assetPath}' was not found in app assets.");

            using var memory = new MemoryStream();
            stream.CopyTo(memory);
            var bytes = memory.ToArray();
            Log.Info(LogTag, $"Loading GLB '{_assetPath}', bytes={bytes.Length}.");
            _filament.LoadGlb(bytes);
            _assetLoadAttempted = true;
        }
        catch (Java.Lang.Throwable ex)
        {
            _assetLoadAttempted = false;
            Log.Error(LogTag, ex, $"GLB load failed for '{_assetPath}'.");
        }
        catch (System.Exception ex)
        {
            _assetLoadAttempted = false;
            Log.Error(LogTag, ex.ToString());
        }
    }

    private void PostFrame()
    {
        if (_paused || !_surfaceReady || _framePosted)
            return;

        _framePosted = true;
        Choreographer.Instance?.PostFrameCallback(this);
    }

    private sealed class FilamentRenderer : IDisposable
    {
        private Engine? _engine;
        private Renderer? _renderer;
        private Scene? _scene;
        private FView? _view;
        private FCamera? _camera;
        private SwapChain? _swapChain;
        private UbershaderProvider? _materialProvider;
        private AssetLoader? _assetLoader;
        private ResourceLoader? _resourceLoader;
        private FilamentAsset? _asset;
        private Animator? _animator;
        private int _cameraEntity;
        private int _lightEntity;
        private int _width = 1;
        private int _height = 1;
        private double _targetX;
        private double _targetY = 0.15;
        private double _targetZ;
        private double _distance = 3.0;
        private bool _firstRenderLogged;

        public bool IsReady =>
            _engine != null &&
            _renderer != null &&
            _scene != null &&
            _view != null &&
            _camera != null;

        public void Ensure(Surface surface, int width, int height)
        {
            if (_engine == null)
                CreateEngine();

            if (_swapChain == null)
            {
                _swapChain = _engine!.CreateSwapChain(surface);
                Log.Info(LogTag, _swapChain == null ? "SwapChain creation returned null." : "SwapChain created.");
            }

            Resize(width, height);
        }

        public void Resize(int width, int height)
        {
            _width = System.Math.Max(1, width);
            _height = System.Math.Max(1, height);
            if (_view == null)
                return;

            _view.Viewport = new Viewport(0, 0, _width, _height);
            Log.Info(LogTag, $"Viewport={_width}x{_height}.");
        }

        public void LoadGlb(byte[] bytes)
        {
            if (!IsReady)
                throw new InvalidOperationException("Filament is not ready for GLB loading.");

            DestroyAsset();
            DestroyGltfioLoaders();

            var entityManager = EntityManager.Get();
            _materialProvider = new UbershaderProvider(_engine!);
            _assetLoader = new AssetLoader(_engine!, _materialProvider, entityManager);
            _resourceLoader = new ResourceLoader(_engine!);

            Log.Info(LogTag, "Loading GLB with AssetLoader.CreateAsset(Java.Nio.Buffer).");
            var buffer = ByteBuffer.AllocateDirect(bytes.Length);
            buffer.Put(bytes);
            buffer.Rewind();

            _asset = _assetLoader.CreateAsset(buffer);
            if (_asset == null)
                throw new InvalidOperationException("AssetLoader.CreateAsset returned null.");

            Log.Info(LogTag, "Asset created.");
            _resourceLoader.LoadResources(_asset);
            Log.Info(LogTag, "Resources loaded.");
            _asset.ReleaseSourceData();

            _animator = _asset.Instance?.Animator;
            Log.Info(LogTag, _animator == null
                ? "Animator not present."
                : $"Animator ready: animations={_animator.AnimationCount}.");

            var entities = _asset.GetEntities();
            var entityCount = entities?.Length ?? 0;
            if (entityCount <= 0)
                throw new InvalidOperationException("GLB loaded but has zero entities.");

            var renderableCount = CountRenderables(entities);
            if (renderableCount <= 0)
                throw new InvalidOperationException($"GLB loaded with {entityCount} entities but zero renderables.");

            ReadAssetBounds();
            _scene!.AddEntities(entities!);
            Log.Info(LogTag, $"Scene entities added: entities={entityCount}, renderables={renderableCount}.");
        }

        public void Render(long frameTimeNanos, double seconds)
        {
            if (!IsReady || _swapChain == null)
                return;

            if (_animator != null && _animator.AnimationCount > 0)
            {
                var duration = System.Math.Max(0.001f, _animator.GetAnimationDuration(0));
                _animator.ApplyAnimation(0, (float)(seconds % duration));
                _animator.UpdateBoneMatrices();
            }

            OrbitCamera(seconds);
            var ok = _renderer!.BeginFrame(_swapChain, frameTimeNanos);
            if (!_firstRenderLogged)
            {
                _firstRenderLogged = true;
                Log.Info(LogTag, $"Frame rendering: beginFrame={ok}, viewport={_width}x{_height}.");
            }

            if (!ok)
                return;

            _renderer.Render(_view!);
            _renderer.EndFrame();
        }

        public void DestroySwapChain()
        {
            if (_engine != null && _swapChain != null)
            {
                _engine.DestroySwapChain(_swapChain);
                _swapChain.Dispose();
                _swapChain = null;
            }
        }

        public void Dispose()
        {
            DestroySwapChain();
            DestroyAsset();
            DestroyGltfioLoaders();

            if (_engine != null)
            {
                if (_lightEntity != 0)
                    _engine.DestroyEntity(_lightEntity);
                if (_cameraEntity != 0)
                    _engine.DestroyCameraComponent(_cameraEntity);
                if (_renderer != null)
                    _engine.DestroyRenderer(_renderer);
                if (_scene != null)
                    _engine.DestroyScene(_scene);
                if (_view != null)
                    _engine.DestroyView(_view);

                _engine.Destroy();
            }

            _renderer = null;
            _scene = null;
            _view = null;
            _camera = null;
            _engine = null;
        }

        private void CreateEngine()
        {
            TryInitializeFilamentAndroid();
            _engine = Engine.Create();
            if (_engine == null)
                throw new InvalidOperationException("Engine.Create returned null.");

            Log.Info(LogTag, "Engine created.");
            _renderer = _engine.CreateRenderer();
            _scene = _engine.CreateScene();
            _view = _engine.CreateView();

            var entityManager = EntityManager.Get();
            _cameraEntity = entityManager.Create();
            _camera = _engine.CreateCamera(_cameraEntity);
            _lightEntity = entityManager.Create();

            _view.Scene = _scene;
            _view.Camera = _camera;
            CreateLight();
            OrbitCamera(0);
            Resize(_width, _height);
            Log.Info(LogTag, "Renderer, scene, view, camera created.");
        }

        private static void TryInitializeFilamentAndroid()
        {
            try
            {
                Utils.Instance.Init();
                Gltfio.Init();
                Log.Info(LogTag, "Filament Utils and GLTFIO initialized.");
            }
            catch (Java.Lang.Throwable ex)
            {
                Log.Warn(LogTag, ex, "Filament initialization was unavailable or failed; continuing.");
            }
        }

        private void CreateLight()
        {
            try
            {
                new LightManager.Builder(LightManager.Type.Directional)
                    .Color(1f, 0.92f, 0.72f)
                    .Intensity(65000f)
                    .Direction(-0.4f, -1f, -0.65f)
                    .CastShadows(false)
                    .Build(_engine!, _lightEntity);
                _scene!.AddEntity(_lightEntity);
                Log.Info(LogTag, "Directional light created.");
            }
            catch (Java.Lang.Throwable ex)
            {
                Log.Warn(LogTag, ex, "Directional light creation failed.");
            }
        }

        private int CountRenderables(int[]? entities)
        {
            if (entities == null || entities.Length == 0 || _engine == null)
                return 0;

            var renderableManager = _engine.RenderableManager;
            var renderables = 0;
            foreach (var entity in entities)
            {
                if (renderableManager.HasComponent(entity))
                    renderables++;
            }

            return renderables;
        }

        private void ReadAssetBounds()
        {
            try
            {
                var box = _asset!.BoundingBox;
                var center = box.GetCenter();
                var halfExtent = box.GetHalfExtent();
                if (center.Length < 3 || halfExtent.Length < 3)
                    return;

                _targetX = center[0];
                _targetY = center[1];
                _targetZ = center[2];
                var hx = System.Math.Abs(halfExtent[0]);
                var hy = System.Math.Abs(halfExtent[1]);
                var hz = System.Math.Abs(halfExtent[2]);
                var radius = System.Math.Max(0.25, System.Math.Sqrt(hx * hx + hy * hy + hz * hz));
                _distance = System.Math.Clamp(radius * 3.1, 1.2, 8.0);
                Log.Info(LogTag, $"Bounding box calculated: center=({_targetX:F2},{_targetY:F2},{_targetZ:F2}), radius={radius:F2}.");
            }
            catch (Java.Lang.Throwable ex)
            {
                Log.Warn(LogTag, ex, "Asset bounds failed; default camera kept.");
            }
        }

        private void OrbitCamera(double seconds)
        {
            if (_camera == null)
                return;

            var aspect = System.Math.Max(0.2, _width / (double)System.Math.Max(1, _height));
            var angle = seconds * 0.55;
            var eyeX = _targetX + System.Math.Sin(angle) * _distance;
            var eyeZ = _targetZ + System.Math.Cos(angle) * _distance;
            var eyeY = _targetY + _distance * 0.42;

            _camera.LookAt(
                eyeX, eyeY, eyeZ,
                _targetX, _targetY, _targetZ,
                0, 1, 0);
            _camera.SetProjection(42, aspect, 0.025, 100, FCamera.Fov.Vertical);

            if (!_firstRenderLogged)
                Log.Info(LogTag, $"Camera positioned: eye=({eyeX:F2},{eyeY:F2},{eyeZ:F2}), target=({_targetX:F2},{_targetY:F2},{_targetZ:F2}).");
        }

        private void DestroyAsset()
        {
            if (_asset == null)
                return;

            try
            {
                var entities = _asset.GetEntities();
                if (entities.Length > 0 && _scene != null)
                    _scene.RemoveEntities(entities);

                _assetLoader?.DestroyAsset(_asset);
            }
            catch (Java.Lang.Throwable ex)
            {
                Log.Warn(LogTag, ex, "Asset removal failed.");
            }

            _asset.Dispose();
            _asset = null;
            _animator = null;
        }

        private void DestroyGltfioLoaders()
        {
            _resourceLoader?.Destroy();
            _resourceLoader?.Dispose();
            _resourceLoader = null;

            _assetLoader?.Destroy();
            _assetLoader?.Dispose();
            _assetLoader = null;

            _materialProvider?.DestroyMaterials();
            _materialProvider?.Destroy();
            _materialProvider?.Dispose();
            _materialProvider = null;
        }
    }
}
#endif

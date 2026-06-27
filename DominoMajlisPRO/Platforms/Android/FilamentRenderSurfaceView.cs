#if ANDROID
using Android.Content;
using Android.Graphics;
using Android.Util;
using Android.Views;
using Java.Lang;
using Java.Nio;
using JClass = Java.Lang.Class;
using JDouble = Java.Lang.Double;
using JFloat = Java.Lang.Float;
using JInteger = Java.Lang.Integer;
using JLong = Java.Lang.Long;
using JObject = Java.Lang.Object;

namespace DominoMajlisPRO.Platforms.Android;

public sealed class FilamentRenderSurfaceView :
    SurfaceView,
    ISurfaceHolderCallback,
    Choreographer.IFrameCallback
{
    private const string LogTag = "LivingVisualFilament";
    private readonly Context _context;
    private readonly FilamentReflector _filament = new();
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
        catch (Exception ex)
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
        catch (Exception ex)
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

    private sealed class FilamentReflector : IDisposable
    {
        private JObject? _engine;
        private JObject? _renderer;
        private JObject? _scene;
        private JObject? _view;
        private JObject? _camera;
        private JObject? _swapChain;
        private JObject? _assetLoader;
        private JObject? _resourceLoader;
        private JObject? _asset;
        private int _cameraEntity;
        private int _lightEntity;
        private int _width = 1;
        private int _height = 1;
        private double _targetX;
        private double _targetY = 0.15;
        private double _targetZ;
        private double _distance = 3.0;
        private bool _firstRenderLogged;

        public bool IsReady => _engine != null && _renderer != null && _scene != null && _view != null && _camera != null;

        public void Ensure(Surface surface, int width, int height)
        {
            if (_engine == null)
                CreateEngine();

            if (_swapChain == null)
            {
                _swapChain = Invoke(_engine!, "createSwapChain", Types("android.view.Surface"), surface);
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

            var viewportClass = Class("com.google.android.filament.Viewport");
            using var viewport = viewportClass.GetConstructor(new[] { JInteger.Type, JInteger.Type, JInteger.Type, JInteger.Type })
                ?.NewInstance(new JObject[] { new JInteger(0), new JInteger(0), new JInteger(_width), new JInteger(_height) });
            if (viewport != null)
                InvokeVoid(_view, "setViewport", new[] { viewportClass }, viewport);
            Log.Info(LogTag, $"Viewport={_width}x{_height}.");
        }

        public void LoadGlb(byte[] bytes)
        {
            if (!IsReady)
                throw new InvalidOperationException("Filament is not ready for GLB loading.");

            DestroyAsset();
            var entityManager = InvokeStatic("com.google.android.filament.EntityManager", "get", Array.Empty<JClass>());
            var materialProvider = New("com.google.android.filament.gltfio.UbershaderProvider", new[] { Class("com.google.android.filament.Engine") }, _engine!);
            _assetLoader = New(
                "com.google.android.filament.gltfio.AssetLoader",
                new[] { Class("com.google.android.filament.Engine"), Class("com.google.android.filament.gltfio.MaterialProvider"), Class("com.google.android.filament.EntityManager") },
                _engine!, materialProvider, entityManager);
            _resourceLoader = New("com.google.android.filament.gltfio.ResourceLoader", new[] { Class("com.google.android.filament.Engine") }, _engine!);

            var buffer = ByteBuffer.AllocateDirect(bytes.Length);
            buffer.Put(bytes);
            buffer.Rewind();

            _asset = Invoke(_assetLoader!, "createAssetFromBinary", new[] { Class("java.nio.ByteBuffer") }, buffer);
            if (_asset == null)
                throw new InvalidOperationException("createAssetFromBinary returned null.");

            InvokeVoid(_resourceLoader!, "loadResources", new[] { Class("com.google.android.filament.gltfio.FilamentAsset") }, _asset);
            InvokeVoid(_asset, "releaseSourceData", Array.Empty<JClass>());

            var entities = Invoke(_asset, "getEntities", Array.Empty<JClass>());
            var entityCount = GetArrayLength(entities);
            if (entityCount <= 0)
                throw new InvalidOperationException("GLB loaded but has zero entities.");

            var renderableCount = CountRenderables(entities);
            if (renderableCount <= 0)
                throw new InvalidOperationException($"GLB loaded with {entityCount} entities but zero renderables.");

            ReadAssetBounds();
            InvokeVoid(_scene!, "addEntities", new[] { entities!.Class }, entities);
            Log.Info(LogTag, $"GLB accepted: entities={entityCount}, renderables={renderableCount}, target=({_targetX:F2},{_targetY:F2},{_targetZ:F2}), distance={_distance:F2}.");
        }

        public void Render(long frameTimeNanos, double seconds)
        {
            if (!IsReady || _swapChain == null)
                return;

            OrbitCamera(seconds);
            var began = Invoke(_renderer!, "beginFrame", new[] { Class("com.google.android.filament.SwapChain"), JLong.Type }, _swapChain, new JLong(frameTimeNanos));
            var ok = began is Java.Lang.Boolean value && value.BooleanValue();
            if (!_firstRenderLogged)
            {
                _firstRenderLogged = true;
                Log.Info(LogTag, $"First beginFrame={ok}, viewport={_width}x{_height}.");
            }

            if (!ok)
                return;

            InvokeVoid(_renderer!, "render", new[] { Class("com.google.android.filament.View") }, _view!);
            InvokeVoid(_renderer!, "endFrame", Array.Empty<JClass>());
        }

        public void DestroySwapChain()
        {
            if (_engine != null && _swapChain != null)
            {
                InvokeVoid(_engine, "destroySwapChain", new[] { Class("com.google.android.filament.SwapChain") }, _swapChain);
                _swapChain.Dispose();
                _swapChain = null;
            }
        }

        public void Dispose()
        {
            DestroySwapChain();
            DestroyAsset();
            _assetLoader?.Dispose();
            _assetLoader = null;
            _resourceLoader?.Dispose();
            _resourceLoader = null;
            _renderer?.Dispose();
            _renderer = null;
            _scene?.Dispose();
            _scene = null;
            _view?.Dispose();
            _view = null;
            _camera?.Dispose();
            _camera = null;
            _engine?.Dispose();
            _engine = null;
        }

        private void CreateEngine()
        {
            TryInitializeFilamentAndroid();
            _engine = InvokeStatic("com.google.android.filament.Engine", "create", Array.Empty<JClass>());
            if (_engine == null)
                throw new InvalidOperationException("Engine.create returned null.");

            _renderer = Invoke(_engine, "createRenderer", Array.Empty<JClass>());
            _scene = Invoke(_engine, "createScene", Array.Empty<JClass>());
            _view = Invoke(_engine, "createView", Array.Empty<JClass>());
            var entityManager = InvokeStatic("com.google.android.filament.EntityManager", "get", Array.Empty<JClass>());
            _cameraEntity = CreateEntity(entityManager);
            _camera = Invoke(_engine, "createCamera", new[] { JInteger.Type }, new JInteger(_cameraEntity));
            _lightEntity = CreateEntity(entityManager);

            InvokeVoid(_view!, "setScene", new[] { Class("com.google.android.filament.Scene") }, _scene!);
            InvokeVoid(_view!, "setCamera", new[] { Class("com.google.android.filament.Camera") }, _camera!);
            CreateLight();
            OrbitCamera(0);
            Resize(_width, _height);
            Log.Info(LogTag, "Engine, renderer, scene, view, camera created.");
        }

        private static void TryInitializeFilamentAndroid()
        {
            try
            {
                InvokeStatic("com.google.android.filament.utils.Utils", "init", Array.Empty<JClass>());
                Log.Info(LogTag, "Filament Utils.init completed.");
            }
            catch (Java.Lang.Throwable ex)
            {
                Log.Warn(LogTag, ex, "Filament Utils.init was unavailable or failed; continuing.");
            }
        }

        private void CreateLight()
        {
            try
            {
                var typeClass = Class("com.google.android.filament.LightManager$Type");
                var directional = InvokeStatic("com.google.android.filament.LightManager$Type", "valueOf", new[] { Class("java.lang.String") }, new Java.Lang.String("DIRECTIONAL"));
                var builder = New("com.google.android.filament.LightManager$Builder", new[] { typeClass }, directional);
                Invoke(builder!, "color", new[] { JFloat.Type, JFloat.Type, JFloat.Type }, new JFloat(1f), new JFloat(0.92f), new JFloat(0.72f));
                Invoke(builder!, "intensity", new[] { JFloat.Type }, new JFloat(65000f));
                Invoke(builder!, "direction", new[] { JFloat.Type, JFloat.Type, JFloat.Type }, new JFloat(-0.4f), new JFloat(-1f), new JFloat(-0.65f));
                Invoke(builder!, "castShadows", new[] { Java.Lang.Boolean.Type }, new Java.Lang.Boolean(false));
                InvokeVoid(builder!, "build", new[] { Class("com.google.android.filament.Engine"), JInteger.Type }, _engine!, new JInteger(_lightEntity));
                InvokeVoid(_scene!, "addEntity", new[] { JInteger.Type }, new JInteger(_lightEntity));
                Log.Info(LogTag, "Directional light created.");
            }
            catch (Java.Lang.Throwable ex)
            {
                Log.Warn(LogTag, ex, "Directional light creation failed.");
            }
        }

        private int CountRenderables(JObject? entities)
        {
            var count = GetArrayLength(entities);
            if (count <= 0 || _engine == null)
                return 0;

            try
            {
                var renderableManager = Invoke(_engine, "getRenderableManager", Array.Empty<JClass>());
                if (renderableManager == null)
                    return 0;

                var renderables = 0;
                for (var i = 0; i < count; i++)
                {
                    var entity = Java.Lang.Reflect.Array.GetInt(entities!, i);
                    var hasComponent = Invoke(renderableManager, "hasComponent", new[] { JInteger.Type }, new JInteger(entity));
                    if (hasComponent is Java.Lang.Boolean present && present.BooleanValue())
                        renderables++;
                }

                return renderables;
            }
            catch (Java.Lang.Throwable ex)
            {
                Log.Warn(LogTag, ex, "Renderable count failed; falling back to entity count.");
                return count;
            }
        }

        private void ReadAssetBounds()
        {
            try
            {
                var box = Invoke(_asset!, "getBoundingBox", Array.Empty<JClass>());
                var center = Invoke(box!, "getCenter", Array.Empty<JClass>());
                var halfExtent = Invoke(box!, "getHalfExtent", Array.Empty<JClass>());
                if (GetArrayLength(center) < 3 || GetArrayLength(halfExtent) < 3)
                    return;

                _targetX = Java.Lang.Reflect.Array.GetFloat(center!, 0);
                _targetY = Java.Lang.Reflect.Array.GetFloat(center!, 1);
                _targetZ = Java.Lang.Reflect.Array.GetFloat(center!, 2);
                var hx = System.Math.Abs(Java.Lang.Reflect.Array.GetFloat(halfExtent!, 0));
                var hy = System.Math.Abs(Java.Lang.Reflect.Array.GetFloat(halfExtent!, 1));
                var hz = System.Math.Abs(Java.Lang.Reflect.Array.GetFloat(halfExtent!, 2));
                var radius = System.Math.Max(0.25, System.Math.Sqrt(hx * hx + hy * hy + hz * hz));
                _distance = System.Math.Clamp(radius * 3.1, 1.2, 8.0);
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

            InvokeVoid(
                _camera,
                "lookAt",
                new[] { JDouble.Type, JDouble.Type, JDouble.Type, JDouble.Type, JDouble.Type, JDouble.Type, JDouble.Type, JDouble.Type, JDouble.Type },
                new JDouble(eyeX), new JDouble(eyeY), new JDouble(eyeZ),
                new JDouble(_targetX), new JDouble(_targetY), new JDouble(_targetZ),
                new JDouble(0), new JDouble(1), new JDouble(0));

            try
            {
                var fovClass = Class("com.google.android.filament.Camera$Fov");
                var vertical = InvokeStatic("com.google.android.filament.Camera$Fov", "valueOf", new[] { Class("java.lang.String") }, new Java.Lang.String("VERTICAL"));
                InvokeVoid(_camera, "setProjection", new[] { JDouble.Type, JDouble.Type, JDouble.Type, JDouble.Type, fovClass }, new JDouble(42), new JDouble(aspect), new JDouble(0.025), new JDouble(100), vertical!);
            }
            catch (Java.Lang.Throwable ex)
            {
                Log.Warn(LogTag, ex, "Camera projection failed.");
            }
        }

        private void DestroyAsset()
        {
            if (_asset == null)
                return;

            try
            {
                var entities = Invoke(_asset, "getEntities", Array.Empty<JClass>());
                if (entities != null && _scene != null)
                    InvokeVoid(_scene, "removeEntities", new[] { entities.Class }, entities);
            }
            catch (Java.Lang.Throwable ex)
            {
                Log.Warn(LogTag, ex, "Asset removal failed.");
            }

            _asset.Dispose();
            _asset = null;
        }

        private static int CreateEntity(JObject? entityManager)
        {
            var entity = Invoke(entityManager!, "create", Array.Empty<JClass>());
            return entity is JInteger value ? value.IntValue() : 0;
        }

        private static int GetArrayLength(JObject? array)
        {
            if (array == null)
                return 0;

            try
            {
                return Java.Lang.Reflect.Array.GetLength(array);
            }
            catch
            {
                return 0;
            }
        }

        private static JClass Class(string name) => JClass.ForName(name);
        private static JClass[] Types(params string[] names) => names.Select(Class).ToArray();
        private static JObject? New(string className, JClass[] parameterTypes, params JObject?[] args) => Class(className).GetConstructor(parameterTypes)?.NewInstance(args);
        private static JObject? InvokeStatic(string className, string methodName, JClass[] parameterTypes, params JObject?[] args) => Class(className).GetMethod(methodName, parameterTypes)?.Invoke(null, args);
        private static JObject? Invoke(JObject target, string methodName, JClass[] parameterTypes, params JObject?[] args) => target.Class?.GetMethod(methodName, parameterTypes)?.Invoke(target, args);
        private static void InvokeVoid(JObject target, string methodName, JClass[] parameterTypes, params JObject?[] args) => target.Class?.GetMethod(methodName, parameterTypes)?.Invoke(target, args)?.Dispose();
    }
}
#endif

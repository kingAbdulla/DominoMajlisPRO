#if ANDROID
using Android.Content;
using Android.Graphics;
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
    private readonly Context _context;
    private readonly FilamentReflector _filament = new();
    private string _assetPath = string.Empty;
    private bool _surfaceReady;
    private bool _paused = true;
    private bool _framePosted;
    private long _firstFrameNanos;

    public FilamentRenderSurfaceView(Context context)
        : base(context)
    {
        _context = context;
        Holder?.AddCallback(this);
        Holder?.SetFormat(Format.Rgba8888);
        SetZOrderOnTop(true);
        SetBackgroundColor(global::Android.Graphics.Color.Transparent);
    }

    public void SetAssetPath(string? assetPath)
    {
        _assetPath = assetPath?.Trim() ?? string.Empty;
        TryLoadAsset();
    }

    public void SetPaused(bool paused)
    {
        _paused = paused;
        if (_paused)
            return;

        EnsureRenderer();
        PostFrame();
    }

    public void SurfaceCreated(ISurfaceHolder holder)
    {
        _surfaceReady = true;
        EnsureRenderer();
        TryLoadAsset();
        PostFrame();
    }

    public void SurfaceChanged(
        ISurfaceHolder holder,
        Format format,
        int width,
        int height)
    {
        EnsureRenderer();
        _filament.Resize(width, height);
        TryLoadAsset();
        PostFrame();
    }

    public void SurfaceDestroyed(ISurfaceHolder holder)
    {
        _surfaceReady = false;
        _framePosted = false;
        _filament.DestroySwapChain();
    }

    public void DoFrame(long frameTimeNanos)
    {
        _framePosted = false;
        if (_paused || !_surfaceReady)
            return;

        _firstFrameNanos = _firstFrameNanos == 0 ? frameTimeNanos : _firstFrameNanos;
        var seconds = (frameTimeNanos - _firstFrameNanos) / 1_000_000_000.0;
        _filament.Render(frameTimeNanos, seconds);
        PostFrame();
    }

    public void DisposeRenderer()
    {
        _paused = true;
        _framePosted = false;
        _surfaceReady = false;
        _filament.Dispose();
    }

    private void EnsureRenderer()
    {
        if (!_surfaceReady || Holder?.Surface == null || !Holder.Surface.IsValid)
            return;

        _filament.Ensure(
            Holder.Surface,
            Width > 0 ? Width : MeasuredWidth,
            Height > 0 ? Height : MeasuredHeight);
    }

    private void TryLoadAsset()
    {
        if (string.IsNullOrWhiteSpace(_assetPath))
            return;

        EnsureRenderer();
        if (!_filament.IsReady)
            return;

        using var stream = _context.Assets?.Open(_assetPath);
        if (stream == null)
            throw new InvalidOperationException($"Living visual package '{_assetPath}' was not found in app assets.");

        using var memory = new MemoryStream();
        stream.CopyTo(memory);
        _filament.LoadGlb(memory.ToArray());
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

        public bool IsReady => _engine != null && _renderer != null && _scene != null && _view != null && _camera != null;

        public void Ensure(Surface surface, int width, int height)
        {
            if (_engine == null)
                CreateEngine();

            if (_swapChain == null)
                _swapChain = Invoke(_engine!, "createSwapChain", Types("android.view.Surface"), surface);

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
        }

        public void LoadGlb(byte[] bytes)
        {
            if (!IsReady)
                return;

            DestroyAsset();

            var entityManager = InvokeStatic("com.google.android.filament.EntityManager", "get", Array.Empty<JClass>());
            var materialProvider = New(
                "com.google.android.filament.gltfio.UbershaderProvider",
                new[] { Class("com.google.android.filament.Engine") },
                _engine!);

            _assetLoader = New(
                "com.google.android.filament.gltfio.AssetLoader",
                new[]
                {
                    Class("com.google.android.filament.Engine"),
                    Class("com.google.android.filament.gltfio.MaterialProvider"),
                    Class("com.google.android.filament.EntityManager")
                },
                _engine!,
                materialProvider,
                entityManager);

            _resourceLoader = New(
                "com.google.android.filament.gltfio.ResourceLoader",
                new[] { Class("com.google.android.filament.Engine") },
                _engine!);

            var buffer = ByteBuffer.AllocateDirect(bytes.Length);
            buffer.Put(bytes);
            buffer.Rewind();

            _asset = Invoke(
                _assetLoader!,
                "createAssetFromBinary",
                new[] { Class("java.nio.ByteBuffer") },
                buffer);

            if (_asset == null)
                throw new InvalidOperationException("Filament could not create an asset from the GLB package.");

            InvokeVoid(_resourceLoader!, "loadResources", new[] { Class("com.google.android.filament.gltfio.FilamentAsset") }, _asset);
            InvokeVoid(_asset, "releaseSourceData", Array.Empty<JClass>());
            var entities = Invoke(_asset, "getEntities", Array.Empty<JClass>());
            if (entities != null)
                InvokeVoid(_scene!, "addEntities", new[] { entities.Class }, entities);
        }

        public void Render(long frameTimeNanos, double seconds)
        {
            if (!IsReady || _swapChain == null)
                return;

            OrbitCamera(seconds);
            var began = Invoke(
                _renderer!,
                "beginFrame",
                new[] { Class("com.google.android.filament.SwapChain"), JLong.Type },
                _swapChain,
                new JLong(frameTimeNanos));

            if (began is Java.Lang.Boolean ok && ok.BooleanValue())
            {
                InvokeVoid(_renderer!, "render", new[] { Class("com.google.android.filament.View") }, _view!);
                InvokeVoid(_renderer!, "endFrame", Array.Empty<JClass>());
            }
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
            _engine = InvokeStatic("com.google.android.filament.Engine", "create", Array.Empty<JClass>());
            if (_engine == null)
                throw new InvalidOperationException("Filament Engine.create returned null.");

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
        }

        private void CreateLight()
        {
            try
            {
                var typeClass = Class("com.google.android.filament.LightManager$Type");
                var directional = InvokeStatic("com.google.android.filament.LightManager$Type", "valueOf", new[] { Class("java.lang.String") }, new Java.Lang.String("DIRECTIONAL"));
                var builder = New(
                    "com.google.android.filament.LightManager$Builder",
                    new[] { typeClass },
                    directional);
                Invoke(builder!, "color", new[] { JFloat.Type, JFloat.Type, JFloat.Type }, new JFloat(1f), new JFloat(0.92f), new JFloat(0.72f));
                Invoke(builder!, "intensity", new[] { JFloat.Type }, new JFloat(65000f));
                Invoke(builder!, "direction", new[] { JFloat.Type, JFloat.Type, JFloat.Type }, new JFloat(-0.4f), new JFloat(-1f), new JFloat(-0.65f));
                Invoke(builder!, "castShadows", new[] { Java.Lang.Boolean.Type }, new Java.Lang.Boolean(false));
                InvokeVoid(builder!, "build", new[] { Class("com.google.android.filament.Engine"), JInteger.Type }, _engine!, new JInteger(_lightEntity));
                InvokeVoid(_scene!, "addEntity", new[] { JInteger.Type }, new JInteger(_lightEntity));
            }
            catch (Java.Lang.Throwable)
            {
            }
        }

        private void OrbitCamera(double seconds)
        {
            if (_camera == null)
                return;

            var aspect = System.Math.Max(0.2, _width / (double)System.Math.Max(1, _height));
            var angle = seconds * 0.55;
            var eyeX = System.Math.Sin(angle) * 3.0;
            var eyeZ = System.Math.Cos(angle) * 3.0;

            InvokeVoid(
                _camera,
                "lookAt",
                new[]
                {
                    JDouble.Type, JDouble.Type, JDouble.Type,
                    JDouble.Type, JDouble.Type, JDouble.Type,
                    JDouble.Type, JDouble.Type, JDouble.Type
                },
                new JDouble(eyeX), new JDouble(1.25), new JDouble(eyeZ),
                new JDouble(0), new JDouble(0.35), new JDouble(0),
                new JDouble(0), new JDouble(1), new JDouble(0));

            try
            {
                var fovClass = Class("com.google.android.filament.Camera$Fov");
                var vertical = InvokeStatic("com.google.android.filament.Camera$Fov", "valueOf", new[] { Class("java.lang.String") }, new Java.Lang.String("VERTICAL"));
                InvokeVoid(
                    _camera,
                    "setProjection",
                    new[] { JDouble.Type, JDouble.Type, JDouble.Type, JDouble.Type, fovClass },
                    new JDouble(42), new JDouble(aspect), new JDouble(0.05), new JDouble(100), vertical!);
            }
            catch (Java.Lang.Throwable)
            {
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
            catch (Java.Lang.Throwable)
            {
            }

            _asset.Dispose();
            _asset = null;
        }

        private static int CreateEntity(JObject? entityManager)
        {
            var entity = Invoke(entityManager!, "create", Array.Empty<JClass>());
            return entity is JInteger value ? value.IntValue() : 0;
        }

        private static JClass Class(string name) => JClass.ForName(name);

        private static JClass[] Types(params string[] names) =>
            names.Select(Class).ToArray();

        private static JObject? New(string className, JClass[] parameterTypes, params JObject?[] args) =>
            Class(className).GetConstructor(parameterTypes)?.NewInstance(args);

        private static JObject? InvokeStatic(string className, string methodName, JClass[] parameterTypes, params JObject?[] args) =>
            Class(className).GetMethod(methodName, parameterTypes)?.Invoke(null, args);

        private static JObject? Invoke(JObject target, string methodName, JClass[] parameterTypes, params JObject?[] args) =>
            target.Class?.GetMethod(methodName, parameterTypes)?.Invoke(target, args);

        private static void InvokeVoid(JObject target, string methodName, JClass[] parameterTypes, params JObject?[] args) =>
            target.Class?.GetMethod(methodName, parameterTypes)?.Invoke(target, args)?.Dispose();
    }
}
#endif

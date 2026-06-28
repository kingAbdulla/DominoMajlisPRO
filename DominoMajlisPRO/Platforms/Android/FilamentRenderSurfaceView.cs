#if ANDROID
using Android.Content;
using Android.Graphics;
using Android.Opengl;
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

    public void SetMotionCommand(string? command)
    {
        _filament.SetMotionCommand(command);
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
            _filament.LoadGlb(bytes, _assetPath);
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
        private readonly Dictionary<string, RigNode> _rigNodes = new(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, MotionOverride> _motionOverrides = new(StringComparer.OrdinalIgnoreCase);
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
        private const double CameraDistanceMultiplier = 1.82;
        private const double CameraVerticalFovDegrees = 34.0;
        private const double CameraFrontSign = 1.0;
        private int _width = 1;
        private int _height = 1;
        private double _targetX;
        private double _targetY = 0.15;
        private double _targetZ;
        private double _distance = 3.0;
        private double _boundsCenterX;
        private double _boundsCenterY = 0.15;
        private double _boundsCenterZ;
        private double _boundsHalfX;
        private double _boundsHalfY;
        private double _boundsHalfZ;
        private double _boundsRadius = 1.0;
        private bool _firstRenderLogged;
        private bool _rigLogged;
        private double _nextBehaviorLogSeconds;
        private bool _devTestMotionEnabled;
        private readonly LivingBehaviorController _livingBehavior = new();

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

        public void LoadGlb(byte[] bytes, string assetPath)
        {
            if (!IsReady)
                throw new InvalidOperationException("Filament is not ready for GLB loading.");

            DestroyAsset();
            DestroyGltfioLoaders();
            _rigNodes.Clear();
            _rigLogged = false;
            _nextBehaviorLogSeconds = 0;
            _devTestMotionEnabled = IsProductionPreviewAsset(assetPath);
            _livingBehavior.SetDevTestMotion(_devTestMotionEnabled);
            Log.Info(LogTag, $"Living behavior DEV_TEST motion={_devTestMotionEnabled} asset='{assetPath}'.");

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
            CaptureRigNode("Root");
            CaptureRigNode("Bone");
            CaptureRigNode("Jaw");
            LogAssetRenderDiagnostics(entities, entityCount, renderableCount);
            _scene!.AddEntities(entities!);
            Log.Info(LogTag, $"Scene entities added: entities={entityCount}, renderables={renderableCount}, rigNodes={_rigNodes.Count}.");
        }

        public void SetMotionCommand(string? command)
        {
            var parsed = MotionOverride.TryParse(command);
            if (parsed == null)
                return;

            _motionOverrides[parsed.Target] = parsed;
            Log.Info(LogTag, $"Motion command accepted: target={parsed.Target}, axis={parsed.Axis}, value={parsed.ValueDegrees:F1}.");
        }

        public void Render(long frameTimeNanos, double seconds)
        {
            if (!IsReady || _swapChain == null)
                return;

            ApplyProceduralRig(seconds);
            PositionCamera();
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
            PositionCamera();
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

                _boundsCenterX = center[0];
                _boundsCenterY = center[1];
                _boundsCenterZ = center[2];
                _boundsHalfX = System.Math.Abs(halfExtent[0]);
                _boundsHalfY = System.Math.Abs(halfExtent[1]);
                _boundsHalfZ = System.Math.Abs(halfExtent[2]);
                _boundsRadius = System.Math.Max(
                    0.25,
                    System.Math.Sqrt((_boundsHalfX * _boundsHalfX) + (_boundsHalfY * _boundsHalfY) + (_boundsHalfZ * _boundsHalfZ)));
                _targetX = _boundsCenterX;
                _targetY = _boundsCenterY + (_boundsRadius * 0.05);
                _targetZ = _boundsCenterZ;
                _distance = System.Math.Clamp(_boundsRadius * CameraDistanceMultiplier, 0.65, 5.5);
                Log.Info(
                    LogTag,
                    $"Bounding box calculated: center=({_boundsCenterX:F2},{_boundsCenterY:F2},{_boundsCenterZ:F2}), " +
                    $"halfExtent=({_boundsHalfX:F2},{_boundsHalfY:F2},{_boundsHalfZ:F2}), radius={_boundsRadius:F2}, " +
                    $"cameraDistance={_distance:F2}, cameraFov={CameraVerticalFovDegrees:F1}, cameraFrontSign={CameraFrontSign:F1}.");
            }
            catch (Java.Lang.Throwable ex)
            {
                Log.Warn(LogTag, ex, "Asset bounds failed; default camera kept.");
            }
        }

        private void LogAssetRenderDiagnostics(int[]? entities, int entityCount, int renderableCount)
        {
            try
            {
                var materialSlotCount = CountMaterialSlots(entities);
                Log.Info(
                    LogTag,
                    "Asset render diagnostics: " +
                    $"boundsCenter=({_boundsCenterX:F2},{_boundsCenterY:F2},{_boundsCenterZ:F2}) " +
                    $"halfExtent=({_boundsHalfX:F2},{_boundsHalfY:F2},{_boundsHalfZ:F2}) " +
                    $"radius={_boundsRadius:F2} cameraDistance={_distance:F2} cameraFov={CameraVerticalFovDegrees:F1} " +
                    $"entityCount={entityCount} renderableCount={renderableCount} rigNodeCount={_rigNodes.Count} materialSlotCount={materialSlotCount}.");

                Log.Info(
                    LogTag,
                    "Distortion R&D hint: if rear ghost geometry remains visible with stable camera and shadows disabled, " +
                    "primary suspects are duplicated/intersecting GLB surfaces, hidden rear shell geometry, inverted normals, or double-sided/transparent material behavior. " +
                    "Rig remains secondary because the artifact existed before bone motion.");
            }
            catch (Java.Lang.Throwable ex)
            {
                Log.Warn(LogTag, ex, "Asset render diagnostics failed; continuing render.");
            }
            catch (System.Exception ex)
            {
                Log.Warn(LogTag, $"Asset render diagnostics failed: {ex}");
            }
        }

        private int CountMaterialSlots(int[]? entities)
        {
            if (entities == null || entities.Length == 0 || _engine == null)
                return 0;

            try
            {
                var renderableManager = _engine.RenderableManager;
                var materialSlots = 0;
                foreach (var entity in entities)
                {
                    if (!renderableManager.HasComponent(entity))
                        continue;

                    var instance = renderableManager.GetInstance(entity);
                    if (instance == 0)
                        continue;

                    materialSlots += System.Math.Max(0, renderableManager.GetPrimitiveCount(instance));
                }

                return materialSlots;
            }
            catch (Java.Lang.Throwable ex)
            {
                Log.Warn(LogTag, ex, "Material slot count unavailable through Filament binding.");
                return -1;
            }
        }

        private void CaptureRigNode(string nodeName)
        {
            if (_asset == null || _engine == null)
            {
                Log.Warn(LogTag, $"Rig discovery: {nodeName} found=false entity=0 transformInstance=0 reason=asset-or-engine-missing.");
                return;
            }

            try
            {
                var entity = _asset.GetFirstEntityByName(nodeName);
                if (entity == 0)
                {
                    Log.Warn(LogTag, $"Rig discovery: {nodeName} found=false entity=0 transformInstance=0 reason=node-not-found.");
                    return;
                }

                var transformManager = _engine.TransformManager;
                var instance = transformManager.GetInstance(entity);
                if (instance == 0)
                {
                    Log.Warn(LogTag, $"Rig discovery: {nodeName} found=false entity={entity} transformInstance=0 reason=no-transform-component.");
                    return;
                }

                var baseTransform = new float[16];
                transformManager.GetTransform(instance, baseTransform);
                _rigNodes[nodeName] = new RigNode(nodeName, entity, instance, baseTransform);
                Log.Info(LogTag, $"Rig discovery: {nodeName} found=true entity={entity} transformInstance={instance}.");
            }
            catch (Java.Lang.Throwable ex)
            {
                Log.Warn(LogTag, ex, $"Rig discovery: {nodeName} found=false entity=unknown transformInstance=unknown reason=exception.");
            }
        }

        private void ApplyProceduralRig(double seconds)
        {
            if (_engine == null || _rigNodes.Count == 0)
                return;

            if (!_rigLogged)
            {
                _rigLogged = true;
                Log.Info(LogTag, $"Runtime rig controller active. Nodes={string.Join(',', _rigNodes.Keys)}.");
            }

            var pose = _livingBehavior.Update(seconds);
            var headYaw = pose.HeadYawDegrees;
            var headPitch = pose.HeadPitchDegrees;
            var headTilt = pose.HeadTiltDegrees;
            var jawOpen = pose.JawOpenDegrees;

            if (_motionOverrides.TryGetValue("Bone", out var headOverride))
            {
                if (headOverride.Axis.Equals("Z", StringComparison.OrdinalIgnoreCase))
                    headYaw = headOverride.ValueDegrees;
                else
                    headPitch = headOverride.ValueDegrees;
            }

            if (_motionOverrides.TryGetValue("Jaw", out var jawOverride))
                jawOpen = jawOverride.ValueDegrees;

            var boneApplied = ApplyRigRotation("Bone", headPitch, headYaw, headTilt);
            var jawApplied = ApplyRigRotation("Jaw", jawOpen, 0, 0);
            var boneMatricesUpdated = UpdateSkinningBoneMatrices();

            if (seconds >= _nextBehaviorLogSeconds)
            {
                _nextBehaviorLogSeconds = seconds + 2.0;
                Log.Info(
                    LogTag,
                    "LivingBehavior tick active " +
                    $"devTest={_devTestMotionEnabled} " +
                    $"boneYaw={headYaw:F1} bonePitch={headPitch:F1} jawAngle={jawOpen:F1} " +
                    $"boneFound={_rigNodes.ContainsKey("Bone")} jawFound={_rigNodes.ContainsKey("Jaw")} " +
                    $"boneApplied={boneApplied} jawApplied={jawApplied} skinningMatricesUpdated={boneMatricesUpdated}.");

                if ((boneApplied || jawApplied) && !boneMatricesUpdated)
                {
                    Log.Warn(
                        LogTag,
                        "Rig transforms were applied through TransformManager, but no Animator.UpdateBoneMatrices path was available. " +
                        "If the mesh remains visually static, this GLB may require a Filament skinning/bone-matrix API path beyond node transforms.");
                }
            }
        }

        private bool ApplyRigRotation(string nodeName, double degreesX, double degreesY, double degreesZ)
        {
            if (_engine == null || !_rigNodes.TryGetValue(nodeName, out var node))
                return false;

            try
            {
                var transformManager = _engine.TransformManager;
                Android.Opengl.Matrix.SetIdentityM(node.Rotation, 0);
                Android.Opengl.Matrix.RotateM(node.Rotation, 0, (float)degreesX, 1, 0, 0);
                Android.Opengl.Matrix.RotateM(node.Rotation, 0, (float)degreesY, 0, 1, 0);
                Android.Opengl.Matrix.RotateM(node.Rotation, 0, (float)degreesZ, 0, 0, 1);
                Android.Opengl.Matrix.MultiplyMM(node.WorkingTransform, 0, node.BaseTransform, 0, node.Rotation, 0);
                transformManager.SetTransform(node.Instance, node.WorkingTransform);
                return true;
            }
            catch (Java.Lang.Throwable ex)
            {
                Log.Warn(LogTag, ex, $"Failed to apply rig rotation to '{nodeName}'.");
                return false;
            }
        }

        private bool UpdateSkinningBoneMatrices()
        {
            if (_animator == null)
                return false;

            try
            {
                _animator.UpdateBoneMatrices();
                return true;
            }
            catch (Java.Lang.Throwable ex)
            {
                Log.Warn(LogTag, ex, "Animator.UpdateBoneMatrices failed after procedural rig transform update.");
                return false;
            }
        }

        private void PositionCamera()
        {
            if (_camera == null)
                return;

            var aspect = System.Math.Max(0.2, _width / (double)System.Math.Max(1, _height));
            var eyeX = _boundsCenterX;
            var eyeZ = _boundsCenterZ + (CameraFrontSign * _distance);
            var eyeY = _boundsCenterY + (_boundsRadius * 0.10);

            _camera.LookAt(
                eyeX, eyeY, eyeZ,
                _targetX, _targetY, _targetZ,
                0, 1, 0);
            _camera.SetProjection(CameraVerticalFovDegrees, aspect, 0.01, 50, FCamera.Fov.Vertical);

            if (!_firstRenderLogged)
                Log.Info(
                    LogTag,
                    $"Stable close-up camera positioned: eye=({eyeX:F2},{eyeY:F2},{eyeZ:F2}), " +
                    $"target=({_targetX:F2},{_targetY:F2},{_targetZ:F2}), distance={_distance:F2}, fov={CameraVerticalFovDegrees:F1}.");
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
            _rigNodes.Clear();
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

        private static bool IsProductionPreviewAsset(string assetPath) =>
            assetPath.Contains("production_default", StringComparison.OrdinalIgnoreCase);

        private sealed class RigNode
        {
            public RigNode(string name, int entity, int instance, float[] baseTransform)
            {
                Name = name;
                Entity = entity;
                Instance = instance;
                BaseTransform = baseTransform;
            }

            public string Name { get; }

            public int Entity { get; }

            public int Instance { get; }

            public float[] BaseTransform { get; }

            public float[] Rotation { get; } = new float[16];

            public float[] WorkingTransform { get; } = new float[16];
        }

        private sealed class LivingBehaviorController
        {
            private readonly Random _random = new();
            private readonly IdleLookBehavior _idleLook = new();
            private readonly InterestLookBehavior _interestLook = new();
            private readonly JawBreathBehavior _jawBreath = new();
            private LivingEmotionState _emotionState = LivingEmotionState.Neutral;
            private bool _devTestMotion;

            public void SetDevTestMotion(bool enabled)
            {
                _devTestMotion = enabled;
                _idleLook.SetDevTestMotion(enabled);
                _jawBreath.SetDevTestMotion(enabled);
            }

            public LivingPose Update(double seconds)
            {
                var context = new LivingBehaviorContext(seconds, _random, _emotionState, _devTestMotion);
                var pose = LivingPose.Neutral;

                _idleLook.Apply(context, ref pose);
                _interestLook.Apply(context, ref pose);
                _jawBreath.Apply(context, ref pose);

                return pose;
            }
        }

        private interface ILivingBehavior
        {
            void Apply(LivingBehaviorContext context, ref LivingPose pose);
        }

        private sealed class IdleLookBehavior : ILivingBehavior
        {
            private const double YawLimit = 12.0;
            private const double PitchLimit = 5.0;
            private const double TiltLimit = 4.0;
            private const double DevTestYawLimit = 25.0;
            private const double DevTestPitchLimit = 10.0;
            private bool _devTestMotion;
            private double _lastSeconds = -1;
            private double _nextDecisionSeconds;
            private double _yawCurrent;
            private double _pitchCurrent;
            private double _tiltCurrent;
            private double _yawStart;
            private double _pitchStart;
            private double _tiltStart;
            private double _yawTarget;
            private double _pitchTarget;
            private double _tiltTarget;
            private double _transitionStartSeconds;
            private double _transitionDurationSeconds = 2.5;

            public void SetDevTestMotion(bool enabled)
            {
                _devTestMotion = enabled;
            }

            public void Apply(LivingBehaviorContext context, ref LivingPose pose)
            {
                if (_devTestMotion)
                {
                    pose = pose with
                    {
                        HeadYawDegrees = pose.HeadYawDegrees + (System.Math.Sin(context.Seconds * 0.85) * DevTestYawLimit),
                        HeadPitchDegrees = pose.HeadPitchDegrees + (System.Math.Sin(context.Seconds * 0.55) * DevTestPitchLimit)
                    };
                    return;
                }

                if (_lastSeconds < 0)
                {
                    _lastSeconds = context.Seconds;
                    ScheduleNextDecision(context, allowIdlePause: true);
                }

                if (context.Seconds >= _nextDecisionSeconds)
                    ScheduleNextDecision(context, allowIdlePause: true);

                var progress = LivingMath.EaseInOut(LivingMath.Saturate(
                    (context.Seconds - _transitionStartSeconds) / _transitionDurationSeconds));

                _yawCurrent = LivingMath.Lerp(_yawStart, _yawTarget, progress);
                _pitchCurrent = LivingMath.Lerp(_pitchStart, _pitchTarget, progress);
                _tiltCurrent = LivingMath.Lerp(_tiltStart, _tiltTarget, progress);
                _lastSeconds = context.Seconds;

                pose = pose with
                {
                    HeadYawDegrees = pose.HeadYawDegrees + _yawCurrent,
                    HeadPitchDegrees = pose.HeadPitchDegrees + _pitchCurrent,
                    HeadTiltDegrees = pose.HeadTiltDegrees + _tiltCurrent
                };
            }

            private void ScheduleNextDecision(LivingBehaviorContext context, bool allowIdlePause)
            {
                _yawStart = _yawCurrent;
                _pitchStart = _pitchCurrent;
                _tiltStart = _tiltCurrent;
                _transitionStartSeconds = context.Seconds;
                _transitionDurationSeconds = context.Range(1.7, 4.4);
                _nextDecisionSeconds = context.Seconds + _transitionDurationSeconds + context.Range(0.4, 3.8);

                if (allowIdlePause && context.Random.NextDouble() < 0.28)
                {
                    _yawTarget = 0;
                    _pitchTarget = 0;
                    _tiltTarget = 0;
                    _nextDecisionSeconds += context.Range(1.2, 4.5);
                    return;
                }

                _yawTarget = context.Range(-YawLimit, YawLimit);
                _pitchTarget = context.Range(-PitchLimit, PitchLimit);
                _tiltTarget = context.Range(-TiltLimit, TiltLimit);
            }
        }

        private sealed class JawBreathBehavior : ILivingBehavior
        {
            private const double JawOpenMin = -14.0;
            private const double JawOpenMax = -18.0;
            private const double DevTestJawOpen = -28.0;
            private bool _devTestMotion;
            private double _nextPulseSeconds = 1.8;
            private double _pulseStartSeconds = -100;
            private double _pulseDurationSeconds = 1.4;
            private double _pulseOpenDegrees = -15.0;

            public void SetDevTestMotion(bool enabled)
            {
                _devTestMotion = enabled;
            }

            public void Apply(LivingBehaviorContext context, ref LivingPose pose)
            {
                if (_devTestMotion)
                {
                    var openClose = (System.Math.Sin(context.Seconds * 0.7) + 1.0) * 0.5;
                    pose = pose with
                    {
                        JawOpenDegrees = pose.JawOpenDegrees + (DevTestJawOpen * LivingMath.EaseInOut(openClose))
                    };
                    return;
                }

                if (context.Seconds >= _nextPulseSeconds)
                    SchedulePulse(context);

                pose = pose with
                {
                    JawOpenDegrees = pose.JawOpenDegrees + ComputeJaw(context.Seconds)
                };
            }

            private void SchedulePulse(LivingBehaviorContext context)
            {
                _pulseStartSeconds = context.Seconds;
                _pulseDurationSeconds = context.Range(1.1, 2.4);
                _pulseOpenDegrees = context.Range(JawOpenMax, JawOpenMin);
                _nextPulseSeconds = context.Seconds + _pulseDurationSeconds + context.Range(2.5, 8.0);
            }

            private double ComputeJaw(double seconds)
            {
                var progress = (seconds - _pulseStartSeconds) / _pulseDurationSeconds;
                if (progress < 0 || progress > 1)
                    return 0;

                var openClose = System.Math.Sin(progress * System.Math.PI);
                return _pulseOpenDegrees * LivingMath.EaseInOut(openClose);
            }
        }

        private sealed class InterestLookBehavior : ILivingBehavior
        {
            public void Apply(LivingBehaviorContext context, ref LivingPose pose)
            {
                if (context.EmotionState == LivingEmotionState.Neutral)
                    return;
            }
        }

        private readonly record struct LivingBehaviorContext(
            double Seconds,
            Random Random,
            LivingEmotionState EmotionState,
            bool DevTestMotion)
        {
            public double Range(double min, double max) =>
                min + (Random.NextDouble() * (max - min));
        }

        private enum LivingEmotionState
        {
            Neutral,
            Interested
        }

        private static class LivingMath
        {
            public static double Lerp(double start, double end, double amount) =>
                start + ((end - start) * amount);

            public static double Saturate(double value) =>
                System.Math.Clamp(value, 0, 1);

            public static double EaseInOut(double value) =>
                value * value * (3 - (2 * value));
        }

        private readonly record struct LivingPose(
            double HeadYawDegrees,
            double HeadPitchDegrees,
            double HeadTiltDegrees,
            double JawOpenDegrees)
        {
            public static LivingPose Neutral { get; } = new(0, 0, 0, 0);
        }

        private sealed record MotionOverride(string Target, string Axis, double ValueDegrees)
        {
            public static MotionOverride? TryParse(string? serialized)
            {
                if (string.IsNullOrWhiteSpace(serialized))
                    return null;

                var parts = serialized.Split('|');
                if (parts.Length < 5)
                    return null;

                var target = parts[1].Trim();
                if (string.IsNullOrWhiteSpace(target))
                    return null;

                if (!double.TryParse(parts[2], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var value))
                    return null;

                var axis = string.IsNullOrWhiteSpace(parts[4]) ? "X" : parts[4].Trim();
                return new MotionOverride(target, axis, value);
            }
        }
    }
}
#endif

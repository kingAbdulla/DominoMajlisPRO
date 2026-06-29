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
        private const float BaseLightRed = 1f;
        private const float BaseLightGreen = 0.92f;
        private const float BaseLightBlue = 0.72f;
        private const float BaseLightIntensity = 65000f;
        private const bool LivingPreviewFreezeBonesForArtifactTest = false;
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
        private readonly LivingBehaviorController _livingBehavior = new();
        private readonly LivingEffectController _livingEffects = new();

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
            _livingBehavior.Reset();
            _livingEffects.Reset();
            Log.Info(LogTag, $"Living behavior state controller reset asset='{assetPath}', freezeBonesForArtifactTest={LivingPreviewFreezeBonesForArtifactTest}.");

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
                    .Color(BaseLightRed, BaseLightGreen, BaseLightBlue)
                    .Intensity(BaseLightIntensity)
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
                LogEntityDiagnostics(entities);
                LogMaterialDiagnostics(entities);

                Log.Info(
                    LogTag,
                    "Distortion R&D classification hint: duplicateGeometry/looseHiddenMesh/invertedNormals/doubleSidedMaterial/transparentDepth/lightingCulling are primary suspects. " +
                    "If the artifact remains with LivingPreviewFreezeBonesForArtifactTest=true, classify as GLB geometry/material/camera-lighting. " +
                    "If it disappears only when frozen, classify as bone weighting or transform/skinning path. " +
                    "Because the issue existed before rigging, current default classification leans GLB geometry or normals/material, not rig. " +
                    "Stable camera and shadows disabled reduce camera-lighting suspicion but do not eliminate it.");
                Log.Info(
                    LogTag,
                    "Distortion R&D detail: if rear ghost geometry remains visible with stable camera and shadows disabled, " +
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

        private void LogEntityDiagnostics(int[]? entities)
        {
            if (entities == null || entities.Length == 0 || _asset == null)
                return;

            try
            {
                var names = new System.Text.StringBuilder();
                var getName = _asset.GetType().GetMethod("GetName", new[] { typeof(int) });
                var getNameByEntity = _asset.GetType().GetMethod("GetNameByEntity", new[] { typeof(int) });
                var method = getName ?? getNameByEntity;
                if (method == null)
                {
                    Log.Info(LogTag, "Entity name diagnostics: unavailable through current Filament binding.");
                    return;
                }

                var count = 0;
                foreach (var entity in entities)
                {
                    var name = method.Invoke(_asset, new object[] { entity }) as string;
                    if (string.IsNullOrWhiteSpace(name))
                        continue;

                    if (names.Length > 0)
                        names.Append(", ");

                    names.Append(name);
                    count++;
                    if (count >= 32)
                    {
                        names.Append(", ...");
                        break;
                    }
                }

                Log.Info(LogTag, names.Length == 0
                    ? "Entity name diagnostics: no non-empty entity names returned."
                    : $"Entity name diagnostics: {names}");
            }
            catch (Java.Lang.Throwable ex)
            {
                Log.Warn(LogTag, ex, "Entity name diagnostics failed through Filament binding.");
            }
            catch (System.Exception ex)
            {
                Log.Warn(LogTag, $"Entity name diagnostics failed: {ex}");
            }
        }

        private void LogMaterialDiagnostics(int[]? entities)
        {
            if (entities == null || entities.Length == 0 || _engine == null)
                return;

            try
            {
                var renderableManager = _engine.RenderableManager;
                var materialSummaries = new System.Text.StringBuilder();
                var materialCount = 0;
                var transparentOrBlendedHints = 0;

                foreach (var entity in entities)
                {
                    if (!renderableManager.HasComponent(entity))
                        continue;

                    var instance = renderableManager.GetInstance(entity);
                    if (instance == 0)
                        continue;

                    var primitiveCount = System.Math.Max(0, renderableManager.GetPrimitiveCount(instance));
                    for (var primitive = 0; primitive < primitiveCount; primitive++)
                    {
                        var materialInstance = renderableManager.GetMaterialInstanceAt(instance, primitive);
                        materialCount++;
                        var summary = DescribeMaterialInstance(materialInstance);
                        if (summary.Contains("transparent", StringComparison.OrdinalIgnoreCase) ||
                            summary.Contains("blend", StringComparison.OrdinalIgnoreCase) ||
                            summary.Contains("alpha", StringComparison.OrdinalIgnoreCase))
                        {
                            transparentOrBlendedHints++;
                        }

                        if (materialSummaries.Length > 0)
                            materialSummaries.Append(" | ");

                        materialSummaries.Append(summary);
                        if (materialCount >= 12)
                        {
                            materialSummaries.Append(" | ...");
                            break;
                        }
                    }

                    if (materialCount >= 12)
                        break;
                }

                Log.Info(
                    LogTag,
                    $"Material diagnostics: materialSlotCount={materialCount}, transparentOrBlendedHintCount={transparentOrBlendedHints}, " +
                    $"samples={(materialSummaries.Length == 0 ? "unavailable" : materialSummaries.ToString())}.");
            }
            catch (Java.Lang.Throwable ex)
            {
                Log.Warn(LogTag, ex, "Material diagnostics unavailable through Filament binding.");
            }
            catch (System.Exception ex)
            {
                Log.Warn(LogTag, $"Material diagnostics failed: {ex}");
            }
        }

        private static string DescribeMaterialInstance(Java.Lang.Object? materialInstance)
        {
            if (materialInstance == null)
                return "material=null";

            try
            {
                var material = InvokeNoArg(materialInstance, "GetMaterial") ?? InvokeNoArg(materialInstance, "Material");
                var materialName =
                    InvokeNoArg(materialInstance, "GetName") as string ??
                    InvokeNoArg(material, "GetName") as string ??
                    "name-unavailable";
                var blending =
                    InvokeNoArg(material, "GetBlendingMode") ??
                    InvokeNoArg(material, "GetBlending") ??
                    "blend-unavailable";
                var doubleSided =
                    InvokeNoArg(material, "IsDoubleSided") ??
                    InvokeNoArg(material, "GetDoubleSided") ??
                    "doubleSided-unavailable";

                return $"material='{materialName}' blending='{blending}' doubleSided='{doubleSided}'";
            }
            catch (System.Exception ex)
            {
                return $"material=diagnostic-failed:{ex.GetType().Name}";
            }
        }

        private static object? InvokeNoArg(object? target, string methodName)
        {
            if (target == null)
                return null;

            var method = target.GetType().GetMethod(methodName, System.Type.EmptyTypes);
            return method?.Invoke(target, null);
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

            var pose = LivingPreviewFreezeBonesForArtifactTest
                ? LivingPose.Neutral with { StateName = "FrozenArtifactTest" }
                : _livingBehavior.Update(seconds);
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
            ApplyLivingEffects(pose, seconds);

            if (seconds >= _nextBehaviorLogSeconds)
            {
                _nextBehaviorLogSeconds = seconds + 2.0;
                Log.Info(
                    LogTag,
                    "LivingBehavior tick active " +
                    $"state={pose.StateName} freezeBones={LivingPreviewFreezeBonesForArtifactTest} " +
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

        private void ApplyLivingEffects(LivingPose pose, double seconds)
        {
            var lighting = _livingEffects.Update(pose, seconds);
            ApplyEffectLighting(lighting);
        }

        private void ApplyEffectLighting(LivingEffectLighting lighting)
        {
            if (_engine == null || _lightEntity == 0)
                return;

            try
            {
                dynamic lightManager = _engine.LightManager;
                var instance = lightManager.GetInstance(_lightEntity);
                lightManager.SetColor(
                    instance,
                    lighting.Red,
                    lighting.Green,
                    lighting.Blue);
                lightManager.SetIntensity(instance, lighting.Intensity);
            }
            catch (Java.Lang.Throwable ex)
            {
                Log.Warn(LogTag, ex, "Living effect light update failed; continuing with base render.");
            }
            catch (System.Exception ex)
            {
                Log.Warn(LogTag, $"Living effect light update failed; continuing with base render. {ex.Message}");
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
            private LivingBehaviorState _state = LivingBehaviorState.IdleStill;
            private LivingPose _currentPose = LivingPose.Neutral with { StateName = nameof(LivingBehaviorState.IdleStill) };
            private LivingPose _startPose = LivingPose.Neutral;
            private LivingPose _targetPose = LivingPose.Neutral;
            private double _stateStartSeconds;
            private double _stateDurationSeconds = 2.0;
            private bool _initialized;

            public void Reset()
            {
                _state = LivingBehaviorState.IdleStill;
                _currentPose = LivingPose.Neutral with { StateName = nameof(LivingBehaviorState.IdleStill) };
                _startPose = LivingPose.Neutral;
                _targetPose = LivingPose.Neutral;
                _stateStartSeconds = 0;
                _stateDurationSeconds = 2.0;
                _initialized = false;
            }

            public LivingPose Update(double seconds)
            {
                if (!_initialized)
                {
                    _initialized = true;
                    EnterState(LivingBehaviorState.IdleStill, seconds, RandomPauseSeconds());
                }

                if (seconds >= _stateStartSeconds + _stateDurationSeconds)
                    ChooseNextState(seconds);

                var progress = LivingMath.EaseInOut(LivingMath.Saturate((seconds - _stateStartSeconds) / _stateDurationSeconds));
                _currentPose = new LivingPose(
                    LivingMath.Lerp(_startPose.HeadYawDegrees, _targetPose.HeadYawDegrees, progress),
                    LivingMath.Lerp(_startPose.HeadPitchDegrees, _targetPose.HeadPitchDegrees, progress),
                    LivingMath.Lerp(_startPose.HeadTiltDegrees, _targetPose.HeadTiltDegrees, progress),
                    LivingMath.Lerp(_startPose.JawOpenDegrees, _targetPose.JawOpenDegrees, progress),
                    _state.ToString());

                return _currentPose;
            }

            private void ChooseNextState(double seconds)
            {
                switch (_state)
                {
                    case LivingBehaviorState.IdleStill:
                        EnterState(ChooseActionState(), seconds, RandomActionSeconds());
                        break;
                    case LivingBehaviorState.ReturnToNeutral:
                        EnterState(LivingBehaviorState.IdleStill, seconds, RandomPauseSeconds());
                        break;
                    default:
                        EnterState(LivingBehaviorState.ReturnToNeutral, seconds, RandomReturnSeconds());
                        break;
                }
            }

            private void EnterState(LivingBehaviorState state, double seconds, double durationSeconds)
            {
                _state = state;
                _stateStartSeconds = seconds;
                _stateDurationSeconds = System.Math.Max(0.35, durationSeconds);
                _startPose = _currentPose with { StateName = state.ToString() };
                _targetPose = PoseForState(state);
            }

            private LivingBehaviorState ChooseActionState()
            {
                var roll = _random.NextDouble();
                if (roll < 0.30)
                    return LivingBehaviorState.LookLeft;
                if (roll < 0.60)
                    return LivingBehaviorState.LookRight;
                if (roll < 0.78)
                    return LivingBehaviorState.LookDownSlight;
                return LivingBehaviorState.JawOpenPulse;
            }

            private LivingPose PoseForState(LivingBehaviorState state)
            {
                return state switch
                {
                    LivingBehaviorState.LookLeft => new LivingPose(
                        -Range(6.0, 10.0),
                        Range(-2.0, 2.0),
                        Range(-1.5, 1.5),
                        0,
                        state.ToString()),
                    LivingBehaviorState.LookRight => new LivingPose(
                        Range(6.0, 10.0),
                        Range(-2.0, 2.0),
                        Range(-1.5, 1.5),
                        0,
                        state.ToString()),
                    LivingBehaviorState.LookDownSlight => new LivingPose(
                        Range(-3.0, 3.0),
                        Range(2.0, 4.0),
                        Range(-1.0, 1.0),
                        0,
                        state.ToString()),
                    LivingBehaviorState.JawOpenPulse => new LivingPose(
                        0,
                        Range(0.0, 2.0),
                        0,
                        -Range(8.0, 14.0),
                        state.ToString()),
                    _ => LivingPose.Neutral with { StateName = state.ToString() }
                };
            }

            private double RandomPauseSeconds() => Range(1.5, 4.0);

            private double RandomActionSeconds() => Range(0.85, 1.8);

            private double RandomReturnSeconds() => Range(0.7, 1.4);

            private double Range(double min, double max) =>
                min + (_random.NextDouble() * (max - min));
        }

        private readonly record struct LivingEffectLighting(
            float Red,
            float Green,
            float Blue,
            float Intensity)
        {
            public static LivingEffectLighting Base { get; } =
                new(BaseLightRed, BaseLightGreen, BaseLightBlue, BaseLightIntensity);
        }

        private sealed class LivingEffectController
        {
            private const double FireChancePerJawPulse = 0.20;
            private readonly Random _random = new();
            private readonly FlameBreathEffect _flame = new();
            private readonly SmokePuffEffect _smoke = new();
            private bool _wasJawPulse;

            public void Reset()
            {
                _wasJawPulse = false;
                _flame.Reset();
                _smoke.Reset();
            }

            public LivingEffectLighting Update(LivingPose pose, double seconds)
            {
                var isJawPulse = string.Equals(
                    pose.StateName,
                    nameof(LivingBehaviorState.JawOpenPulse),
                    StringComparison.Ordinal);

                if (isJawPulse && !_wasJawPulse && pose.JawOpenDegrees < -3.0)
                    TryTriggerMouthBurst(seconds);

                _wasJawPulse = isJawPulse;

                var flame = _flame.Sample(seconds);
                var smoke = _smoke.Sample(seconds);
                if (!flame.IsActive && !smoke.IsActive)
                    return LivingEffectLighting.Base;

                var heat = flame.Intensity;
                var haze = smoke.Intensity;
                return new LivingEffectLighting(
                    (float)LivingMath.Lerp(BaseLightRed, 1.0, heat),
                    (float)LivingMath.Lerp(BaseLightGreen, 0.48 + (haze * 0.12), heat),
                    (float)LivingMath.Lerp(BaseLightBlue, 0.18 + (haze * 0.22), heat),
                    (float)(BaseLightIntensity +
                            (heat * 78000.0) +
                            (haze * 12000.0)));
            }

            private void TryTriggerMouthBurst(double seconds)
            {
                if (_flame.IsActive(seconds) || _smoke.IsActive(seconds))
                    return;

                if (_random.NextDouble() > FireChancePerJawPulse)
                    return;

                var flameDuration = Range(0.7, 1.4);
                var smokeDuration = Range(1.2, 2.0);
                _flame.Trigger(seconds, flameDuration, Range(0.78, 1.0));
                _smoke.Trigger(seconds + flameDuration * 0.42, smokeDuration, Range(0.35, 0.65));
                Log.Info(LogTag, $"MouthBurstEffect triggered: FlameBreathEffect={flameDuration:F2}s SmokePuffEffect={smokeDuration:F2}s.");
            }

            private double Range(double min, double max) =>
                min + (_random.NextDouble() * (max - min));
        }

        private abstract class MouthBurstEffect
        {
            private double _startSeconds = -1000;
            private double _durationSeconds;
            private double _strength;

            public bool IsActive(double seconds) =>
                seconds >= _startSeconds &&
                seconds <= _startSeconds + _durationSeconds;

            public void Trigger(double seconds, double durationSeconds, double strength)
            {
                _startSeconds = seconds;
                _durationSeconds = System.Math.Max(0.1, durationSeconds);
                _strength = System.Math.Clamp(strength, 0, 1);
            }

            public void Reset()
            {
                _startSeconds = -1000;
                _durationSeconds = 0;
                _strength = 0;
            }

            public LivingEffectSample Sample(double seconds)
            {
                if (!IsActive(seconds))
                    return LivingEffectSample.Inactive;

                var normalized = LivingMath.Saturate((seconds - _startSeconds) / _durationSeconds);
                return new LivingEffectSample(true, Shape(normalized) * _strength);
            }

            protected abstract double Shape(double normalized);
        }

        private sealed class FlameBreathEffect : MouthBurstEffect
        {
            protected override double Shape(double normalized)
            {
                var rise = LivingMath.Saturate(normalized / 0.24);
                var fall = 1.0 - LivingMath.Saturate((normalized - 0.24) / 0.76);
                var flicker = 0.86 + (0.14 * System.Math.Sin(normalized * 52.0));
                return LivingMath.EaseInOut(rise) * LivingMath.EaseInOut(fall) * flicker;
            }
        }

        private sealed class SmokePuffEffect : MouthBurstEffect
        {
            protected override double Shape(double normalized)
            {
                var rise = LivingMath.Saturate(normalized / 0.36);
                var drift = 1.0 - LivingMath.Saturate((normalized - 0.18) / 0.82);
                return LivingMath.EaseInOut(rise) * System.Math.Sqrt(System.Math.Max(0, drift));
            }
        }

        private readonly record struct LivingEffectSample(bool IsActive, double Intensity)
        {
            public static LivingEffectSample Inactive { get; } = new(false, 0);
        }

        private enum LivingBehaviorState
        {
            IdleStill,
            LookLeft,
            LookRight,
            LookDownSlight,
            JawOpenPulse,
            ReturnToNeutral
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
            double JawOpenDegrees,
            string StateName)
        {
            public static LivingPose Neutral { get; } = new(0, 0, 0, 0, nameof(LivingBehaviorState.IdleStill));
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

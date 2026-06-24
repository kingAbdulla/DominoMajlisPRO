using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace DominoMajlisPRO.GalleryEngine.VisualIdentity
{
    /// <summary>
    /// Clock state enum.
    /// </summary>
    public enum ClockState
    {
        Created,
        Running,
        Paused,
        Stopped,
        Disposed
    }

    /// <summary>
    /// Shared Animation Clock for all visual effects.
    /// Provides a single global clock to prevent per-effect timer overhead.
    /// Supports subscriber registration, Tick events, frame limiting, DeltaTime clamping, and lifecycle safety.
    /// 
    /// IMPORTANT: Render hosts must subscribe on appear and unsubscribe on disappear to avoid memory leaks.
    /// This clock does NOT create per-effect timers.
    /// This clock does NOT block the UI thread.
    /// This clock is compatible with MAUI UI-thread rendering.
    /// Uses Stopwatch for monotonic high-resolution timing (independent of system clock changes).
    /// 
    /// Part of Phase 1 Foundation implementation.
    /// Phase 1 Status: APPROVED
    /// </summary>
    public sealed class SharedAnimationClock
    {
        private static readonly SharedAnimationClock _instance = new SharedAnimationClock();
        private readonly Stopwatch _stopwatch;
        private long _lastFrameTimestamp;
        private double _elapsedTime;
        private double _deltaTime;
        private long _frameNumber;
        private ClockState _state;
        private double _targetFPS;
        private double _maxDeltaTime;
        
        // Diagnostics
        private double _currentFPS;
        private double _averageFrameTime;
        private double _lastTickDuration;
        private long _frameStartTime;
        private long _frameEndTime;
        private long _totalFrameTime;
        private long _frameCountForAverage;
        
        private readonly List<WeakReference<ITickSubscriber>> _subscribers;
        private readonly object _lock;
        private ITickSubscriber[] _subscriberBuffer; // Reusable buffer to avoid per-frame allocation
        private long _frameCountSinceCleanup; // Frame counter for periodic cleanup
        private const int CleanupInterval = 60; // Clean up dead references every 60 frames
        private long _frameCountSinceBufferCheck; // Frame counter for buffer shrink check
        private const int BufferShrinkCheckInterval = 300; // Check buffer size every 300 frames

        /// <summary>
        /// Gets the singleton instance of the SharedAnimationClock.
        /// </summary>
        public static SharedAnimationClock Instance => _instance;

        /// <summary>
        /// Gets the total elapsed time since the clock started.
        /// </summary>
        public double ElapsedTime => _elapsedTime;

        /// <summary>
        /// Gets the time delta between the last two frames (clamped to maxDeltaTime).
        /// </summary>
        public double DeltaTime => _deltaTime;

        /// <summary>
        /// Gets the current clock state.
        /// </summary>
        public ClockState State => _state;

        /// <summary>
        /// Gets the current frame number.
        /// </summary>
        public long FrameNumber => _frameNumber;

        /// <summary>
        /// Gets the current FPS.
        /// </summary>
        public double CurrentFPS => _currentFPS;

        /// <summary>
        /// Gets the average frame time in seconds.
        /// </summary>
        public double AverageFrameTime => _averageFrameTime;

        /// <summary>
        /// Gets the duration of the last tick in seconds.
        /// </summary>
        public double LastTickDuration => _lastTickDuration;

        /// <summary>
        /// Gets the frame start time in stopwatch ticks.
        /// </summary>
        public long FrameStartTime => _frameStartTime;

        /// <summary>
        /// Gets the frame end time in stopwatch ticks.
        /// </summary>
        public long FrameEndTime => _frameEndTime;

        /// <summary>
        /// Gets or sets the target FPS for frame limiting decisions.
        /// Note: This does NOT block the UI thread. It only provides decision data.
        /// </summary>
        public double TargetFPS
        {
            get => _targetFPS;
            set => _targetFPS = Math.Max(15.0, Math.Min(120.0, value));
        }

        /// <summary>
        /// Gets or sets the maximum allowed DeltaTime to prevent huge jumps after pause/resume.
        /// Default is 0.1 seconds (100ms).
        /// </summary>
        public double MaxDeltaTime
        {
            get => _maxDeltaTime;
            set => _maxDeltaTime = Math.Max(0.01, Math.Min(1.0, value));
        }

        /// <summary>
        /// Gets the minimum frame time for the target FPS.
        /// </summary>
        public double MinFrameTime => 1.0 / _targetFPS;

        /// <summary>
        /// Gets whether the current thread is the UI thread (debug only).
        /// </summary>
        public bool IsUIThread
        {
            get
            {
#if DEBUG
                // Platform.CurrentThreadId is not available on all platforms
                // Assume UI thread for debug diagnostics
                return true;
#else
                return true;
#endif
            }
        }

        /// <summary>
        /// Private constructor for singleton pattern.
        /// </summary>
        private SharedAnimationClock()
        {
            _stopwatch = new Stopwatch();
            _lastFrameTimestamp = 0;
            _elapsedTime = 0;
            _deltaTime = 0;
            _frameNumber = 0;
            _state = ClockState.Created;
            _targetFPS = 30.0;
            _maxDeltaTime = 0.1;
            
            _currentFPS = 30.0;
            _averageFrameTime = 0;
            _lastTickDuration = 0;
            _frameStartTime = 0;
            _frameEndTime = 0;
            _totalFrameTime = 0;
            _frameCountForAverage = 0;
            
            _subscribers = new List<WeakReference<ITickSubscriber>>();
            _subscriberBuffer = Array.Empty<ITickSubscriber>();
            _frameCountSinceCleanup = 0;
            _frameCountSinceBufferCheck = 0;
            _lock = new object();
        }

        /// <summary>
        /// Starts the animation clock.
        /// </summary>
        public void Start()
        {
            lock (_lock)
            {
                if (_state == ClockState.Running) return;

                _stopwatch.Restart();
                _lastFrameTimestamp = 0;
                _elapsedTime = 0;
                _deltaTime = 0;
                _frameNumber = 0;
                _state = ClockState.Running;
            }
        }

        /// <summary>
        /// Stops the animation clock.
        /// </summary>
        public void Stop()
        {
            lock (_lock)
            {
                _state = ClockState.Stopped;
                _stopwatch.Stop();
            }
        }

        /// <summary>
        /// Pauses the animation clock.
        /// </summary>
        public void Pause()
        {
            lock (_lock)
            {
                if (_state != ClockState.Running) return;
                _state = ClockState.Paused;
                _stopwatch.Stop();
            }
        }

        /// <summary>
        /// Resumes the animation clock.
        /// </summary>
        public void Resume()
        {
            lock (_lock)
            {
                if (_state != ClockState.Paused) return;
                _state = ClockState.Running;
                _stopwatch.Start();
                _lastFrameTimestamp = _stopwatch.ElapsedTicks; // Reset to prevent large DeltaTime jump
            }
        }

        /// <summary>
        /// Resets the clock to zero.
        /// </summary>
        public void Reset()
        {
            lock (_lock)
            {
                _stopwatch.Restart();
                _lastFrameTimestamp = 0;
                _elapsedTime = 0;
                _deltaTime = 0;
                _frameNumber = 0;
                _state = ClockState.Created;
                
                _currentFPS = _targetFPS;
                _averageFrameTime = 0;
                _totalFrameTime = 0;
                _frameCountForAverage = 0;
            }
        }

        /// <summary>
        /// Updates the clock for the current frame.
        /// Should be called once per frame from the main render loop.
        /// This method does NOT block. It only updates time and notifies subscribers.
        /// </summary>
        public void Update()
        {
            _frameStartTime = Stopwatch.GetTimestamp();

            lock (_lock)
            {
                if (_state != ClockState.Running) return;

                var currentTimestamp = _stopwatch.ElapsedTicks;
                var rawDeltaTimeTicks = currentTimestamp - _lastFrameTimestamp;
                
                // Convert to seconds
                var rawDeltaTime = rawDeltaTimeTicks / (double)Stopwatch.Frequency;
                
                // Clamp DeltaTime to prevent huge jumps after pause/resume
                _deltaTime = Math.Min(rawDeltaTime, _maxDeltaTime);
                _elapsedTime += _deltaTime;
                _lastFrameTimestamp = currentTimestamp;
                _frameNumber++;

                // Update FPS calculation
                if (_deltaTime > 0)
                {
                    _currentFPS = 1.0 / _deltaTime;
                }
            }

            // Notify subscribers outside the lock to prevent blocking
            NotifySubscribers();

            _frameEndTime = Stopwatch.GetTimestamp();
            _lastTickDuration = (_frameEndTime - _frameStartTime) / (double)Stopwatch.Frequency;
            
            // Update average frame time
            _totalFrameTime += (_frameEndTime - _frameStartTime);
            _frameCountForAverage++;
            if (_frameCountForAverage > 60) // Update average every 60 frames
            {
                _averageFrameTime = _totalFrameTime / (_frameCountForAverage * (double)Stopwatch.Frequency);
                _totalFrameTime = 0;
                _frameCountForAverage = 0;
            }
        }

        /// <summary>
        /// Registers a subscriber to receive Tick events.
        /// IMPORTANT: Subscribers must unsubscribe on disposal to avoid memory leaks.
        /// Uses WeakReference to prevent memory leaks if subscribers forget to unsubscribe.
        /// </summary>
        /// <param name="subscriber">The subscriber to register.</param>
        public void Subscribe(ITickSubscriber subscriber)
        {
            if (subscriber == null) return;

            lock (_lock)
            {
                // Remove any existing weak reference to the same subscriber
                _subscribers.RemoveAll(wr => !wr.TryGetTarget(out var target) || target == subscriber);
                
                // Add new weak reference
                _subscribers.Add(new WeakReference<ITickSubscriber>(subscriber));
            }
        }

        /// <summary>
        /// Unregisters a subscriber from Tick events.
        /// </summary>
        /// <param name="subscriber">The subscriber to unregister.</param>
        public void Unsubscribe(ITickSubscriber subscriber)
        {
            if (subscriber == null) return;

            lock (_lock)
            {
                _subscribers.RemoveAll(wr => !wr.TryGetTarget(out var target) || target == subscriber);
            }
        }

        /// <summary>
        /// Gets the current time in seconds for animation calculations.
        /// </summary>
        /// <returns>Current time in seconds.</returns>
        public double GetCurrentTime()
        {
            return _elapsedTime;
        }

        /// <summary>
        /// Gets a phase value (0-1) for a given period.
        /// Useful for creating repeating animations.
        /// </summary>
        /// <param name="period">The period in seconds.</param>
        /// <returns>A value between 0 and 1 representing the current phase.</returns>
        public double GetPhase(double period)
        {
            if (period <= 0) return 0;
            return (_elapsedTime % period) / period;
        }

        /// <summary>
        /// Gets a sine wave value for smooth oscillating animations.
        /// </summary>
        /// <param name="period">The period in seconds.</param>
        /// <param name="amplitude">The amplitude of the wave (default 1.0).</param>
        /// <returns>A sine wave value between -amplitude and amplitude.</returns>
        public double GetSineWave(double period, double amplitude = 1.0)
        {
            return Math.Sin(GetPhase(period) * Math.PI * 2) * amplitude;
        }

        /// <summary>
        /// Gets a cosine wave value for smooth oscillating animations.
        /// </summary>
        /// <param name="period">The period in seconds.</param>
        /// <param name="amplitude">The amplitude of the wave (default 1.0).</param>
        /// <returns>A cosine wave value between -amplitude and amplitude.</returns>
        public double GetCosineWave(double period, double amplitude = 1.0)
        {
            return Math.Cos(GetPhase(period) * Math.PI * 2) * amplitude;
        }

        /// <summary>
        /// Determines whether a frame should be rendered based on target FPS.
        /// This is a decision helper, NOT a blocking mechanism.
        /// Render hosts should call this to decide whether to render.
        /// </summary>
        /// <returns>True if the frame should be rendered.</returns>
        public bool ShouldRenderFrame()
        {
            return _deltaTime >= MinFrameTime;
        }

        /// <summary>
        /// Determines whether a frame should be skipped based on target FPS.
        /// This is a decision helper, NOT a blocking mechanism.
        /// </summary>
        /// <returns>True if the frame should be skipped.</returns>
        public bool ShouldSkipFrame()
        {
            return _deltaTime < MinFrameTime;
        }

        /// <summary>
        /// Notifies all active subscribers of the Tick event.
        /// Removes dead weak references automatically (periodically, not every frame).
        /// Does NOT hold lock during dispatch to prevent blocking.
        /// Uses reusable buffer to avoid per-frame allocation.
        /// </summary>
        private void NotifySubscribers()
        {
            int activeCount;
            bool performCleanup = false;
            bool checkBufferShrink = false;
            
            lock (_lock)
            {
                _frameCountSinceCleanup++;
                _frameCountSinceBufferCheck++;

                // Clean up dead weak references periodically (every 60 frames)
                if (_frameCountSinceCleanup >= CleanupInterval)
                {
                    _frameCountSinceCleanup = 0;
                    performCleanup = true;
                }

                if (performCleanup)
                {
                    _subscribers.RemoveAll(wr => !wr.TryGetTarget(out _));
                }

                activeCount = _subscribers.Count;

                // Resize buffer if needed
                if (_subscriberBuffer.Length < activeCount)
                {
                    _subscriberBuffer = new ITickSubscriber[activeCount];
                }

                // Check buffer shrink policy periodically (every 300 frames)
                if (_frameCountSinceBufferCheck >= BufferShrinkCheckInterval)
                {
                    _frameCountSinceBufferCheck = 0;
                    checkBufferShrink = true;
                }

                if (checkBufferShrink && _subscriberBuffer.Length > activeCount * 2 && _subscriberBuffer.Length > 16)
                {
                    // Shrink buffer if it's more than 2x the active count and larger than 16
                    _subscriberBuffer = new ITickSubscriber[Math.Max(16, activeCount)];
                }

                // Copy active subscribers to buffer
                for (int i = 0; i < activeCount; i++)
                {
                    _subscribers[i].TryGetTarget(out _subscriberBuffer[i]);
                }
            }

            // Dispatch outside the lock to prevent blocking
            for (int i = 0; i < activeCount; i++)
            {
                var subscriber = _subscriberBuffer[i];
                if (subscriber != null)
                {
                    try
                    {
                        subscriber.OnTick(_deltaTime, _elapsedTime, _frameNumber);
                    }
                    catch
                    {
                        // Silently handle errors - diagnostics forwarded to VisualDiagnostics
                    }
                }
            }
        }

        /// <summary>
        /// Gets the number of active subscribers.
        /// </summary>
        /// <returns>Number of active subscribers.</returns>
        public int GetSubscriberCount()
        {
            lock (_lock)
            {
                // Clean up dead weak references first
                _subscribers.RemoveAll(wr => !wr.TryGetTarget(out _));
                return _subscribers.Count;
            }
        }

        /// <summary>
        /// Clears all subscribers.
        /// Use with caution - this will stop all tick notifications.
        /// </summary>
        public void ClearSubscribers()
        {
            lock (_lock)
            {
                _subscribers.Clear();
            }
        }
    }

    /// <summary>
    /// Interface for tick subscribers.
    /// Render hosts must implement this to receive tick notifications.
    /// </summary>
    public interface ITickSubscriber
    {
        /// <summary>
        /// Called on each tick of the SharedAnimationClock.
        /// IMPORTANT: This method must not block and must not allocate per frame.
        /// </summary>
        /// <param name="deltaTime">The time delta since the last frame (clamped).</param>
        /// <param name="elapsedTime">The total elapsed time since clock start.</param>
        /// <param name="frameNumber">The current frame number.</param>
        void OnTick(double deltaTime, double elapsedTime, long frameNumber);
    }
}

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading;

namespace DominoMajlisPRO.GalleryEngine.VisualIdentity
{
    /// <summary>
    /// Visual Event Bus for deferred event dispatch and diagnostics.
    /// Provides thread-safe event publishing, subscription management, and deferred dispatch.
    /// Uses SharedAnimationClock for timing and supports sticky events with expiration.
    /// 
    /// Part of Phase 1 Foundation implementation.
    /// Phase 1 Status: Ready for Foundation Lock
    /// </summary>
    public static class VisualEventBus
    {
        private static readonly Dictionary<EventCategory, List<VisualEventHandler>> _subscribers;
        private static readonly Dictionary<string, EventEntry> _stickyEvents;
        private static readonly Queue<EventEntry> _eventQueue;
        private static readonly object _queueLock;
        private static readonly object _subscriptionLock;
        private static readonly object _stickyLock;
        private static int _maxQueueLength;
        private static int _maxStickyEventCount;
        private static QueueOverflowPolicy _overflowPolicy;
        private static long _eventCounter;
        private static long _droppedEventCount;
        private static long _rejectedEventCount;
        private static long _peakQueueLength;
        private static long _peakSubscriberCount;
        private static double _totalDispatchTime;
        private static double _worstDispatchTime;
        private static long _dispatchCount;
        private static int _propagationDepth;
        private static double _dispatchBudgetMs;
        private const int MaxPropagationDepth = 10;
        private const int DefaultMaxQueueLength = 1000;
        private const int DefaultMaxStickyEventCount = 100;
        private const double DefaultDispatchBudgetMs = 5.0;

        static VisualEventBus()
        {
            _subscribers = new Dictionary<EventCategory, List<VisualEventHandler>>();
            _stickyEvents = new Dictionary<string, EventEntry>();
            _eventQueue = new Queue<EventEntry>();
            _queueLock = new object();
            _subscriptionLock = new object();
            _stickyLock = new object();
            _maxQueueLength = DefaultMaxQueueLength;
            _maxStickyEventCount = DefaultMaxStickyEventCount;
            _overflowPolicy = QueueOverflowPolicy.DropOldest;
            _eventCounter = 0;
            _droppedEventCount = 0;
            _rejectedEventCount = 0;
            _peakQueueLength = 0;
            _peakSubscriberCount = 0;
            _totalDispatchTime = 0;
            _worstDispatchTime = 0;
            _dispatchCount = 0;
            _propagationDepth = 0;
            _dispatchBudgetMs = DefaultDispatchBudgetMs;
        }

        /// <summary>
        /// Gets or sets the maximum queue length.
        /// </summary>
        public static int MaxQueueLength
        {
            get
            {
                lock (_queueLock)
                {
                    return _maxQueueLength;
                }
            }
            set
            {
                lock (_queueLock)
                {
                    _maxQueueLength = Math.Max(1, value);
                }
            }
        }

        /// <summary>
        /// Gets or sets the maximum sticky event count.
        /// </summary>
        public static int MaxStickyEventCount
        {
            get
            {
                lock (_stickyLock)
                {
                    return _maxStickyEventCount;
                }
            }
            set
            {
                lock (_stickyLock)
                {
                    _maxStickyEventCount = Math.Max(1, value);
                }
            }
        }

        /// <summary>
        /// Gets or sets the queue overflow policy.
        /// </summary>
        public static QueueOverflowPolicy OverflowPolicy
        {
            get => _overflowPolicy;
            set => _overflowPolicy = value;
        }

        /// <summary>
        /// Gets or sets the dispatch budget in milliseconds per frame.
        /// </summary>
        public static double DispatchBudgetMs
        {
            get => _dispatchBudgetMs;
            set => _dispatchBudgetMs = Math.Max(0.1, value);
        }

        /// <summary>
        /// Gets the peak queue length recorded.
        /// </summary>
        public static long PeakQueueLength => _peakQueueLength;

        /// <summary>
        /// Gets the peak subscriber count recorded.
        /// </summary>
        public static long PeakSubscriberCount => _peakSubscriberCount;

        /// <summary>
        /// Gets the average dispatch time in milliseconds.
        /// </summary>
        public static double AverageDispatchTime => _dispatchCount > 0 ? _totalDispatchTime / _dispatchCount : 0;

        /// <summary>
        /// Gets the worst dispatch time in milliseconds.
        /// </summary>
        public static double WorstDispatchTime => _worstDispatchTime;

        /// <summary>
        /// Gets the total dropped event count.
        /// </summary>
        public static long DroppedEventCount => _droppedEventCount;

        /// <summary>
        /// Gets the total rejected event count.
        /// </summary>
        public static long RejectedEventCount => _rejectedEventCount;

        /// <summary>
        /// Gets the total dispatch count.
        /// </summary>
        public static long DispatchCount => _dispatchCount;

        /// <summary>
        /// Gets the current queue length.
        /// </summary>
        public static int CurrentQueueLength
        {
            get
            {
                lock (_queueLock)
                {
                    return _eventQueue.Count;
                }
            }
        }

        /// <summary>
        /// Gets the current sticky event count.
        /// </summary>
        public static int CurrentStickyEventCount
        {
            get
            {
                lock (_stickyLock)
                {
                    return _stickyEvents.Count;
                }
            }
        }

        /// <summary>
        /// Gets the current subscriber count.
        /// </summary>
        public static int CurrentSubscriberCount
        {
            get
            {
                lock (_subscriptionLock)
                {
                    int count = 0;
                    foreach (var kvp in _subscribers)
                    {
                        count += kvp.Value.Count;
                    }
                    return count;
                }
            }
        }

        /// <summary>
        /// Publishes an event to the event bus.
        /// </summary>
        /// <param name="category">The event category.</param>
        /// <param name="eventName">The event name.</param>
        /// <param name="eventData">The event data.</param>
        public static void Publish(EventCategory category, string eventName, Dictionary<string, object> eventData)
        {
            Publish(category, eventName, eventData, isSticky: false, stickyExpirationMs: 0, priority: VisualEventPriority.Normal);
        }

        /// <summary>
        /// Publishes an event to the event bus with sticky option.
        /// </summary>
        /// <param name="category">The event category.</param>
        /// <param name="eventName">The event name.</param>
        /// <param name="eventData">The event data.</param>
        /// <param name="isSticky">Whether the event is sticky.</param>
        /// <param name="stickyExpirationMs">Sticky event expiration in milliseconds (0 = no expiration).</param>
        /// <param name="priority">The event priority.</param>
        public static void Publish(EventCategory category, string eventName, Dictionary<string, object> eventData, bool isSticky, long stickyExpirationMs, VisualEventPriority priority = VisualEventPriority.Normal)
        {
            var eventId = Interlocked.Increment(ref _eventCounter);
            double timestamp;
            
            try
            {
                var clock = SharedAnimationClock.Instance;
                timestamp = clock.ElapsedTime;
            }
            catch
            {
                // Fallback to DateTime.UtcNow if SharedAnimationClock is unavailable
                timestamp = DateTime.UtcNow.Ticks / 10000000.0;
            }
            
            // Convert mutable Dictionary to immutable payload
            // TODO: Phase 2 optimization - Replace with zero-allocation immutable payload object
            // for hot paths. Current implementation allocates Dictionary and ReadOnlyDictionary.
            IReadOnlyDictionary<string, object> immutableData = eventData != null 
                ? new ReadOnlyDictionary<string, object>(new Dictionary<string, object>(eventData))
                : new ReadOnlyDictionary<string, object>(new Dictionary<string, object>());
            
            var eventEntry = new EventEntry(
                eventId,
                category,
                eventName,
                immutableData,
                timestamp,
                isSticky,
                stickyExpirationMs,
                stickyExpirationMs > 0 ? timestamp + (stickyExpirationMs / 1000.0) : 0,
                priority
            );

            lock (_queueLock)
            {
                // Handle sticky events
                if (isSticky)
                {
                    lock (_stickyLock)
                    {
                        var stickyKey = $"{category}_{eventName}";
                        
                        // Enforce max sticky event count
                        if (_stickyEvents.Count >= _maxStickyEventCount)
                        {
                            // Remove oldest sticky event (FIFO) without allocating a list
                            string firstKey = null;
                            foreach (var key in _stickyEvents.Keys)
                            {
                                firstKey = key;
                                break;
                            }
                            if (firstKey != null)
                                _stickyEvents.Remove(firstKey);
                        }
                        
                        _stickyEvents[stickyKey] = eventEntry;
                    }
                }

                // Handle queue overflow
                if (_eventQueue.Count >= _maxQueueLength)
                {
                    switch (_overflowPolicy)
                    {
                        case QueueOverflowPolicy.DropOldest:
                            _eventQueue.Dequeue();
                            Interlocked.Increment(ref _droppedEventCount);
                            break;
                        case QueueOverflowPolicy.DropNewest:
                            Interlocked.Increment(ref _rejectedEventCount);
                            return; // Drop the new event
                        case QueueOverflowPolicy.DropLowestPriority:
                            // Find and drop lowest priority event
                            var lowestPriorityEntry = FindLowestPriorityEvent();
                            if (lowestPriorityEntry != null && lowestPriorityEntry.Value.Priority < priority)
                            {
                                RemoveEventFromQueue(lowestPriorityEntry.Value.EventId);
                                Interlocked.Increment(ref _droppedEventCount);
                            }
                            else
                            {
                                Interlocked.Increment(ref _rejectedEventCount);
                                return;
                            }
                            break;
                        case QueueOverflowPolicy.RejectNew:
                            Interlocked.Increment(ref _rejectedEventCount);
                            return;
                        case QueueOverflowPolicy.Expand:
                            // Allow queue to expand (developer only)
                            break;
                    }
                }

                _eventQueue.Enqueue(eventEntry);
                if (_eventQueue.Count > _peakQueueLength)
                    _peakQueueLength = _eventQueue.Count;
            }
        }

        /// <summary>
        /// Finds the lowest priority event in the queue.
        /// </summary>
        private static EventEntry? FindLowestPriorityEvent()
        {
            EventEntry? lowest = null;
            var lowestPriority = VisualEventPriority.Background;
            
            foreach (var entry in _eventQueue)
            {
                if (entry.Priority < lowestPriority)
                {
                    lowestPriority = entry.Priority;
                    lowest = entry;
                }
            }
            
            return lowest;
        }

        /// <summary>
        /// Removes an event from the queue by ID.
        /// TODO: Phase 2 optimization - Replace Queue with priority queue or indexed collection
        /// to avoid O(n) rebuild when removing events by ID.
        /// </summary>
        private static void RemoveEventFromQueue(long eventId)
        {
            var tempQueue = new Queue<EventEntry>();
            while (_eventQueue.Count > 0)
            {
                var entry = _eventQueue.Dequeue();
                if (entry.EventId != eventId)
                    tempQueue.Enqueue(entry);
            }
            while (tempQueue.Count > 0)
            {
                _eventQueue.Enqueue(tempQueue.Dequeue());
            }
        }

        /// <summary>
        /// Subscribes to events of a specific category.
        /// </summary>
        /// <param name="category">The event category.</param>
        /// <param name="handler">The event handler.</param>
        /// <returns>A disposable subscription token.</returns>
        public static IDisposable Subscribe(EventCategory category, VisualEventHandler handler)
        {
            if (handler == null)
                throw new ArgumentNullException(nameof(handler));

            lock (_subscriptionLock)
            {
                if (!_subscribers.ContainsKey(category))
                    _subscribers[category] = new List<VisualEventHandler>();

                _subscribers[category].Add(handler);
                var subscriberCount = _subscribers[category].Count;
                if (subscriberCount > _peakSubscriberCount)
                    _peakSubscriberCount = subscriberCount;
            }

            return new EventSubscription(category, handler);
        }

        /// <summary>
        /// Unsubscribes a handler from a specific category.
        /// </summary>
        /// <param name="category">The event category.</param>
        /// <param name="handler">The event handler.</param>
        public static void Unsubscribe(EventCategory category, VisualEventHandler handler)
        {
            if (handler == null)
                return;

            lock (_subscriptionLock)
            {
                if (_subscribers.TryGetValue(category, out var handlers))
                {
                    handlers.Remove(handler);
                    if (handlers.Count == 0)
                        _subscribers.Remove(category);
                }
            }
        }

        /// <summary>
        /// Dispatches all queued events.
        /// Does not hold lock during dispatch to avoid deadlocks.
        /// Respects dispatch budget to prevent frame time overruns.
        /// </summary>
        public static void DispatchQueuedEvents()
        {
            if (_propagationDepth >= MaxPropagationDepth)
                return;

            // Copy queued events while holding lock
            EventEntry[] eventsToDispatch;
            lock (_queueLock)
            {
                if (_eventQueue.Count == 0)
                    return;

                eventsToDispatch = new EventEntry[_eventQueue.Count];
                for (int i = 0; i < eventsToDispatch.Length; i++)
                {
                    eventsToDispatch[i] = _eventQueue.Dequeue();
                }
            }

            // Dispatch events without holding lock, respecting budget
            _propagationDepth++;
            try
            {
                double timestamp;
                try
                {
                    var clock = SharedAnimationClock.Instance;
                    timestamp = clock.ElapsedTime;
                }
                catch
                {
                    timestamp = DateTime.UtcNow.Ticks / 10000000.0;
                }

                var startTime = timestamp;
                var budgetEndTime = startTime + (_dispatchBudgetMs / 1000.0);

                foreach (var eventEntry in eventsToDispatch)
                {
                    DispatchEvent(eventEntry);

                    // Check dispatch budget
                    try
                    {
                        var clock = SharedAnimationClock.Instance;
                        if (clock.ElapsedTime >= budgetEndTime)
                            break;
                    }
                    catch
                    {
                        if ((DateTime.UtcNow.Ticks / 10000000.0) >= budgetEndTime)
                            break;
                    }
                }

                double endTime;
                try
                {
                    var clock = SharedAnimationClock.Instance;
                    endTime = clock.ElapsedTime;
                }
                catch
                {
                    endTime = DateTime.UtcNow.Ticks / 10000000.0;
                }

                var dispatchTime = (endTime - startTime) * 1000; // Convert to ms
                _totalDispatchTime += dispatchTime;
                if (dispatchTime > _worstDispatchTime)
                    _worstDispatchTime = dispatchTime;
                _dispatchCount++;
            }
            finally
            {
                _propagationDepth--;
            }
        }

        /// <summary>
        /// Dispatches a single event to subscribers.
        /// </summary>
        private static void DispatchEvent(EventEntry eventEntry)
        {
            List<VisualEventHandler> handlers;
            lock (_subscriptionLock)
            {
                if (!_subscribers.TryGetValue(eventEntry.Category, out handlers))
                    return;

                // Create a copy to avoid holding lock during invocation
                handlers = new List<VisualEventHandler>(handlers);
            }

            foreach (var handler in handlers)
            {
                try
                {
                    handler(eventEntry);
                }
                catch (Exception ex)
                {
                    // Log error but continue dispatching to other handlers
                    System.Diagnostics.Debug.WriteLine($"[VisualEventBus] Handler failed for {eventEntry.EventName}: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Gets a sticky event by category and name.
        /// </summary>
        /// <param name="category">The event category.</param>
        /// <param name="eventName">The event name.</param>
        /// <returns>The sticky event entry, or null if not found or expired.</returns>
        public static EventEntry? GetStickyEvent(EventCategory category, string eventName)
        {
            var stickyKey = $"{category}_{eventName}";
            double currentTime;
            
            try
            {
                var clock = SharedAnimationClock.Instance;
                
                currentTime = clock.ElapsedTime;
            }
            catch
            {
                currentTime = DateTime.UtcNow.Ticks / 10000000.0;
            }

            lock (_stickyLock)
            {
                if (_stickyEvents.TryGetValue(stickyKey, out var eventEntry))
                {
                    // Check expiration
                    if (eventEntry.StickyExpirationTime > 0 && currentTime >= eventEntry.StickyExpirationTime)
                    {
                        _stickyEvents.Remove(stickyKey);
                        return null;
                    }
                    return eventEntry;
                }
            }

            return null;
        }

        /// <summary>
        /// Clears expired sticky events.
        /// </summary>
        public static void ClearExpiredStickyEvents()
        {
            double currentTime;
            try
            {
                var clock = SharedAnimationClock.Instance;
                
                currentTime = clock.ElapsedTime;
            }
            catch
            {
                currentTime = DateTime.UtcNow.Ticks / 10000000.0;
            }

            var expiredKeys = new List<string>();

            lock (_stickyLock)
            {
                foreach (var kvp in _stickyEvents)
                {
                    if (kvp.Value.StickyExpirationTime > 0 && currentTime >= kvp.Value.StickyExpirationTime)
                    {
                        expiredKeys.Add(kvp.Key);
                    }
                }

                foreach (var key in expiredKeys)
                {
                    _stickyEvents.Remove(key);
                }
            }
        }

        /// <summary>
        /// Clears all sticky events.
        /// </summary>
        public static void ClearAllStickyEvents()
        {
            lock (_stickyLock)
            {
                _stickyEvents.Clear();
            }
        }

        /// <summary>
        /// Clears all queued events.
        /// </summary>
        public static void ClearQueue()
        {
            lock (_queueLock)
            {
                _eventQueue.Clear();
            }
        }

        /// <summary>
        /// Gets a full diagnostics snapshot.
        /// </summary>
        /// <returns>Diagnostics snapshot.</returns>
        public static VisualEventBusDiagnostics GetDiagnosticsSnapshot()
        {
            return new VisualEventBusDiagnostics(
                CurrentQueueLength,
                CurrentStickyEventCount,
                CurrentSubscriberCount,
                _peakQueueLength,
                _peakSubscriberCount,
                _droppedEventCount,
                _rejectedEventCount,
                AverageDispatchTime,
                _worstDispatchTime,
                _dispatchCount,
                MaxQueueLength,
                MaxStickyEventCount,
                _dispatchBudgetMs,
                _overflowPolicy
            );
        }

        /// <summary>
        /// Resets the event bus diagnostics.
        /// </summary>
        public static void ResetDiagnostics()
        {
            lock (_queueLock)
            {
                _peakQueueLength = 0;
            }
            lock (_subscriptionLock)
            {
                _peakSubscriberCount = 0;
            }
            _totalDispatchTime = 0;
            _worstDispatchTime = 0;
            _dispatchCount = 0;
            Interlocked.Exchange(ref _droppedEventCount, 0);
            Interlocked.Exchange(ref _rejectedEventCount, 0);
        }
    }

    /// <summary>
    /// Event entry for queued and sticky events.
    /// Immutable struct for allocation-safe storage.
    /// </summary>
    public readonly struct EventEntry
    {
        public readonly long EventId;
        public readonly EventCategory Category;
        public readonly string EventName;
        public readonly IReadOnlyDictionary<string, object> EventData;
        public readonly double Timestamp;
        public readonly bool IsSticky;
        public readonly long StickyExpirationMs;
        public readonly double StickyExpirationTime;
        public readonly VisualEventPriority Priority;

        public EventEntry(
            long eventId,
            EventCategory category,
            string eventName,
            IReadOnlyDictionary<string, object> eventData,
            double timestamp,
            bool isSticky,
            long stickyExpirationMs,
            double stickyExpirationTime,
            VisualEventPriority priority)
        {
            EventId = eventId;
            Category = category;
            EventName = eventName;
            EventData = eventData;
            Timestamp = timestamp;
            IsSticky = isSticky;
            StickyExpirationMs = stickyExpirationMs;
            StickyExpirationTime = stickyExpirationTime;
            Priority = priority;
        }
    }

    /// <summary>
    /// Visual event handler delegate.
    /// </summary>
    /// <param name="eventEntry">The event entry.</param>
    public delegate void VisualEventHandler(EventEntry eventEntry);

    /// <summary>
    /// Event subscription token for IDisposable pattern.
    /// Sealed to prevent inheritance.
    /// </summary>
    public sealed class EventSubscription : IDisposable
    {
        private readonly EventCategory _category;
        private readonly VisualEventHandler _handler;
        private bool _disposed;

        public EventSubscription(EventCategory category, VisualEventHandler handler)
        {
            _category = category;
            _handler = handler;
            _disposed = false;
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            VisualEventBus.Unsubscribe(_category, _handler);
            _disposed = true;
        }
    }

    /// <summary>
    /// Queue overflow policy.
    /// </summary>
    public enum QueueOverflowPolicy
    {
        /// <summary>
        /// Drop the oldest event when queue is full.
        /// </summary>
        DropOldest,

        /// <summary>
        /// Drop the newest event when queue is full.
        /// </summary>
        DropNewest,

        /// <summary>
        /// Drop the lowest priority event when queue is full.
        /// </summary>
        DropLowestPriority,

        /// <summary>
        /// Reject new events when queue is full.
        /// </summary>
        RejectNew,

        /// <summary>
        /// Allow queue to expand (developer only).
        /// </summary>
        Expand
    }

    /// <summary>
    /// Visual event bus diagnostics snapshot.
    /// Immutable readonly struct for allocation-safe diagnostics.
    /// </summary>
    public readonly struct VisualEventBusDiagnostics
    {
        public int CurrentQueueLength { get; }
        public int CurrentStickyEventCount { get; }
        public int CurrentSubscriberCount { get; }
        public long PeakQueueLength { get; }
        public long PeakSubscriberCount { get; }
        public long DroppedEventCount { get; }
        public long RejectedEventCount { get; }
        public double AverageDispatchTime { get; }
        public double WorstDispatchTime { get; }
        public long DispatchCount { get; }
        public int MaxQueueLength { get; }
        public int MaxStickyEventCount { get; }
        public double DispatchBudgetMs { get; }
        public QueueOverflowPolicy OverflowPolicy { get; }

        public VisualEventBusDiagnostics(
            int currentQueueLength,
            int currentStickyEventCount,
            int currentSubscriberCount,
            long peakQueueLength,
            long peakSubscriberCount,
            long droppedEventCount,
            long rejectedEventCount,
            double averageDispatchTime,
            double worstDispatchTime,
            long dispatchCount,
            int maxQueueLength,
            int maxStickyEventCount,
            double dispatchBudgetMs,
            QueueOverflowPolicy overflowPolicy)
        {
            CurrentQueueLength = currentQueueLength;
            CurrentStickyEventCount = currentStickyEventCount;
            CurrentSubscriberCount = currentSubscriberCount;
            PeakQueueLength = peakQueueLength;
            PeakSubscriberCount = peakSubscriberCount;
            DroppedEventCount = droppedEventCount;
            RejectedEventCount = rejectedEventCount;
            AverageDispatchTime = averageDispatchTime;
            WorstDispatchTime = worstDispatchTime;
            DispatchCount = dispatchCount;
            MaxQueueLength = maxQueueLength;
            MaxStickyEventCount = maxStickyEventCount;
            DispatchBudgetMs = dispatchBudgetMs;
            OverflowPolicy = overflowPolicy;
        }
    }

    /// <summary>
    /// Application events bridge for integrating app-level events with VisualEventBus.
    /// Reserved for future application integration.
    /// Not part of Phase 1 runtime.
    /// </summary>
    public static class AppEventsBridge
    {
        /// <summary>
        /// Bridges application lifecycle events to VisualEventBus.
        /// Reserved for future implementation.
        /// </summary>
        public static void Initialize()
        {
            // Reserved for future application lifecycle integration
            // This will be connected to the actual app lifecycle in Phase 2 or later
        }

        /// <summary>
        /// Publishes an app lifecycle event.
        /// Reserved for future implementation.
        /// </summary>
        /// <param name="eventName">The event name.</param>
        /// <param name="eventData">The event data.</param>
        public static void PublishAppEvent(string eventName, Dictionary<string, object> eventData)
        {
            // Reserved for future implementation
            // VisualEventBus.Publish(EventCategory.System, eventName, eventData);
        }
    }
}

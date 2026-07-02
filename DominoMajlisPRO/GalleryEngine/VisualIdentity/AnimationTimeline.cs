using System;
using System.Collections.Generic;

namespace DominoMajlisPRO.GalleryEngine.VisualIdentity
{
    /// <summary>
    /// Timeline event for animation sequencing.
    /// Render-agnostic data model.
    /// </summary>
    public struct TimelineEvent
    {
        public double TriggerTime;
        public TimelineEventType EventType;
        public string ParameterName;
        public double ParameterValue;
        public string StringValue;

        public TimelineEvent(double triggerTime, TimelineEventType eventType)
        {
            TriggerTime = triggerTime;
            EventType = eventType;
            ParameterName = string.Empty;
            ParameterValue = 0.0;
            StringValue = string.Empty;
        }

        public TimelineEvent(double triggerTime, TimelineEventType eventType, string parameterName, double parameterValue)
        {
            TriggerTime = triggerTime;
            EventType = eventType;
            ParameterName = parameterName ?? string.Empty;
            ParameterValue = parameterValue;
            StringValue = string.Empty;
        }

        public TimelineEvent(double triggerTime, TimelineEventType eventType, string stringValue)
        {
            TriggerTime = triggerTime;
            EventType = eventType;
            ParameterName = string.Empty;
            ParameterValue = 0.0;
            StringValue = stringValue ?? string.Empty;
        }
    }

    /// <summary>
    /// Animation timeline for Living Visual Identity Engine.
    /// Render-agnostic architecture.
    /// Supports deterministic timeline events.
    /// Events are sorted by trigger time for efficient playback.
    /// Update() uses NextEventIndex for O(1) performance on long timelines.
    /// HashSet for triggered tracking is acceptable for Phase 1 (diagnostic/debug utility).
    /// 
    /// Part of Phase 1 Foundation implementation.
    /// Phase 1 Status: Ready for Foundation Lock
    /// </summary>
    public class AnimationTimeline
    {
        private readonly List<TimelineEvent> _events;
        private readonly HashSet<int> _triggeredEventIndices;
        
        private double _currentTime;
        private double _duration;
        private TimelineState _state;
        private bool _looping;
        private int _triggeredCount;
        private int _nextEventIndex; // Index of next event to trigger for O(1) Update()
        
        /// <summary>
        /// Gets the current timeline time.
        /// </summary>
        public double CurrentTime => _currentTime;
        
        /// <summary>
        /// Gets the timeline duration.
        /// </summary>
        public double Duration => _duration;
        
        /// <summary>
        /// Gets the current timeline state.
        /// </summary>
        public TimelineState State => _state;
        
        /// <summary>
        /// Gets whether the timeline is looping.
        /// </summary>
        public bool IsLooping => _looping;
        
        /// <summary>
        /// Gets the number of events on the timeline.
        /// </summary>
        public int EventCount => _events.Count;
        
        /// <summary>
        /// Gets the number of events that have been triggered.
        /// </summary>
        public int TriggeredCount => _triggeredCount;
        
        /// <summary>
        /// Event raised when a timeline event is triggered.
        /// </summary>
        public event Action<TimelineEvent> OnEventTriggered;
        
        /// <summary>
        /// Event raised when the timeline state changes.
        /// </summary>
        public event Action<TimelineState> OnStateChanged;
        
        /// <summary>
        /// Event raised when the timeline completes.
        /// </summary>
        public event Action OnCompleted;
        
        /// <summary>
        /// Initializes a new animation timeline.
        /// </summary>
        public AnimationTimeline()
        {
            _events = new List<TimelineEvent>();
            _triggeredEventIndices = new HashSet<int>();
            _currentTime = 0.0;
            _duration = 0.0;
            _state = TimelineState.Stopped;
            _looping = false;
            _triggeredCount = 0;
            _nextEventIndex = 0;
        }
        
        /// <summary>
        /// Initializes a new animation timeline with a duration.
        /// </summary>
        /// <param name="duration">The timeline duration in seconds.</param>
        public AnimationTimeline(double duration) : this()
        {
            _duration = Math.Max(0, duration);
        }
        
        /// <summary>
        /// Adds an event to the timeline.
        /// Events are automatically sorted by trigger time.
        /// </summary>
        /// <param name="event">The timeline event to add.</param>
        public void AddEvent(TimelineEvent @event)
        {
            if (@event.TriggerTime < 0)
                return;
            
            _events.Add(@event);
            
            // Sort events by trigger time
            _events.Sort((a, b) => a.TriggerTime.CompareTo(b.TriggerTime));
            
            // Update duration if needed
            if (@event.TriggerTime > _duration)
                _duration = @event.TriggerTime;
            
            // Reset next event index if needed
            if (_nextEventIndex >= _events.Count)
                _nextEventIndex = 0;
        }
        
        /// <summary>
        /// Adds an event to the timeline.
        /// </summary>
        /// <param name="triggerTime">The trigger time in seconds.</param>
        /// <param name="eventType">The event type.</param>
        public void AddEvent(double triggerTime, TimelineEventType eventType)
        {
            AddEvent(new TimelineEvent(triggerTime, eventType));
        }
        
        /// <summary>
        /// Adds a parameter event to the timeline.
        /// </summary>
        /// <param name="triggerTime">The trigger time in seconds.</param>
        /// <param name="eventType">The event type.</param>
        /// <param name="parameterName">The parameter name.</param>
        /// <param name="parameterValue">The parameter value.</param>
        public void AddEvent(double triggerTime, TimelineEventType eventType, string parameterName, double parameterValue)
        {
            AddEvent(new TimelineEvent(triggerTime, eventType, parameterName, parameterValue));
        }
        
        /// <summary>
        /// Adds a string value event to the timeline.
        /// </summary>
        /// <param name="triggerTime">The trigger time in seconds.</param>
        /// <param name="eventType">The event type.</param>
        /// <param name="stringValue">The string value.</param>
        public void AddEvent(double triggerTime, TimelineEventType eventType, string stringValue)
        {
            AddEvent(new TimelineEvent(triggerTime, eventType, stringValue));
        }
        
        /// <summary>
        /// Clears all events from the timeline.
        /// </summary>
        public void ClearEvents()
        {
            _events.Clear();
            _triggeredEventIndices.Clear();
            _triggeredCount = 0;
            _nextEventIndex = 0;
        }
        
        /// <summary>
        /// Sets the timeline duration.
        /// </summary>
        /// <param name="duration">The duration in seconds.</param>
        public void SetDuration(double duration)
        {
            _duration = Math.Max(0, duration);
        }
        
        /// <summary>
        /// Sets whether the timeline loops.
        /// </summary>
        /// <param name="looping">True to enable looping.</param>
        public void SetLooping(bool looping)
        {
            _looping = looping;
        }
        
        /// <summary>
        /// Plays the timeline from the current position.
        /// </summary>
        public void Play()
        {
            if (_state == TimelineState.Playing)
                return;
            
            SetState(TimelineState.Playing);
        }
        
        /// <summary>
        /// Pauses the timeline.
        /// </summary>
        public void Pause()
        {
            if (_state != TimelineState.Playing)
                return;
            
            SetState(TimelineState.Paused);
        }
        
        /// <summary>
        /// Resumes the timeline from paused state.
        /// </summary>
        public void Resume()
        {
            if (_state != TimelineState.Paused)
                return;
            
            SetState(TimelineState.Playing);
        }
        
        /// <summary>
        /// Stops the timeline and resets to the beginning.
        /// </summary>
        public void Stop()
        {
            SetState(TimelineState.Stopped);
            Reset();
        }
        
        /// <summary>
        /// Resets the timeline to the beginning.
        /// </summary>
        public void Reset()
        {
            _currentTime = 0.0;
            _triggeredEventIndices.Clear();
            _triggeredCount = 0;
            _nextEventIndex = 0;
            
            if (Duration > 0 && _state == TimelineState.Completed)
            {
                SetState(TimelineState.Stopped);
            }
        }
        
        /// <summary>
        /// Seeks to a specific time on the timeline.
        /// </summary>
        /// <param name="time">The time to seek to in seconds.</param>
        public void Seek(double time)
        {
            time = Math.Max(0, time);
            
            if (_duration > 0)
                time = Math.Min(time, _duration);
            
            _currentTime = time;
            
            // Reset triggered events for events after the seek time
            for (int i = 0; i < _events.Count; i++)
            {
                if (_events[i].TriggerTime > _currentTime)
                {
                    _triggeredEventIndices.Remove(i);
                }
            }
            
            // Recalculate triggered count
            _triggeredCount = _triggeredEventIndices.Count;
            
            // Reset next event index to first untriggered event
            _nextEventIndex = 0;
            while (_nextEventIndex < _events.Count && _triggeredEventIndices.Contains(_nextEventIndex))
            {
                _nextEventIndex++;
            }
        }
        
        /// <summary>
        /// Updates the timeline.
        /// Zero allocations.
        /// </summary>
        /// <param name="deltaTime">Delta time in seconds.</param>
        public void Update(double deltaTime)
        {
            if (_state != TimelineState.Playing)
                return;
            
            // Validate deltaTime
            if (double.IsNaN(deltaTime) || deltaTime < 0)
                deltaTime = 0;
            
            // Update current time
            _currentTime += deltaTime;
            
            // Check for completion
            if (_duration > 0 && _currentTime >= _duration)
            {
                if (_looping)
                {
                    // Loop back to beginning
                    _currentTime = 0.0;
                    _triggeredEventIndices.Clear();
                    _triggeredCount = 0;
                    _nextEventIndex = 0;
                }
                else
                {
                    // Complete the timeline
                    _currentTime = _duration;
                    SetState(TimelineState.Completed);
                    OnCompleted?.Invoke();
                    return;
                }
            }
            
            // Trigger events using NextEventIndex for O(1) performance
            TriggerEvents();
        }
        
        /// <summary>
        /// Triggers events at the current time.
        /// Uses NextEventIndex for O(1) performance on long timelines.
        /// Zero allocations.
        /// </summary>
        private void TriggerEvents()
        {
            // Trigger events from next event index onwards
            while (_nextEventIndex < _events.Count)
            {
                var timelineEvent = _events[_nextEventIndex];
                
                // Check if event should trigger
                if (timelineEvent.TriggerTime <= _currentTime)
                {
                    _triggeredEventIndices.Add(_nextEventIndex);
                    _triggeredCount++;
                    
                    OnEventTriggered?.Invoke(timelineEvent);
                    _nextEventIndex++;
                }
                else
                {
                    // Events are sorted, so no more events to trigger
                    break;
                }
            }
        }
        
        /// <summary>
        /// Sets the timeline state.
        /// </summary>
        /// <param name="newState">The new state.</param>
        private void SetState(TimelineState newState)
        {
            if (_state == newState)
                return;
            
            _state = newState;
            OnStateChanged?.Invoke(newState);
        }
        
        /// <summary>
        /// Validates the timeline configuration.
        /// </summary>
        /// <returns>True if valid, false otherwise.</returns>
        public bool Validate()
        {
            // Validate duration
            if (_duration < 0)
                return false;
            
            // Validate events
            for (int i = 0; i < _events.Count; i++)
            {
                var timelineEvent = _events[i];
                
                // Validate trigger time
                if (timelineEvent.TriggerTime < 0)
                    return false;
                
                // Validate event type
                if (!Enum.IsDefined(typeof(TimelineEventType), timelineEvent.EventType))
                    return false;
            }
            
            // Validate current time
            if (_currentTime < 0)
                return false;
            
            if (_duration > 0 && _currentTime > _duration)
                return false;
            
            return true;
        }
        
        /// <summary>
        /// Gets all events on the timeline.
        /// </summary>
        /// <returns>Read-only list of timeline events.</returns>
        public IReadOnlyList<TimelineEvent> GetEvents()
        {
            return _events.AsReadOnly();
        }
        
        /// <summary>
        /// Gets triggered event indices.
        /// </summary>
        /// <returns>Read-only set of triggered event indices.</returns>
        public IReadOnlySet<int> GetTriggeredEventIndices()
        {
            return _triggeredEventIndices;
        }
    }
}

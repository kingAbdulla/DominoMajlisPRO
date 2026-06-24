using System;

namespace DominoMajlisPRO.GalleryEngine.VisualIdentity
{
    /// <summary>
    /// Effect state machine for Living Visual Identity Engine.
    /// Render-agnostic architecture.
    /// Manages effect lifecycle with deterministic state transitions.
    /// Zero allocations in Update().
    /// Driven by external deltaTime (not integrated with SharedAnimationClock).
    /// Transition reason strings are diagnostic - use static/cached values in runtime hot paths.
    /// 
    /// Part of Phase 1 Foundation implementation.
    /// Phase 1 Status: Ready for Foundation Lock
    /// </summary>
    public class EffectStateMachine
    {
        private EffectState _currentState;
        private EffectState _previousState;
        private string _lastTransitionReason;
        private double _stateElapsedTime;
        
        // EffectState enum has 4 values: Idle=0, Active=1, Special=2, Cooldown=3
        private const int MaxEffectStateValue = 3;
        
        /// <summary>
        /// Gets the current effect state.
        /// </summary>
        public EffectState CurrentState => _currentState;
        
        /// <summary>
        /// Gets the previous effect state.
        /// </summary>
        public EffectState PreviousState => _previousState;
        
        /// <summary>
        /// Gets the reason for the last transition.
        /// </summary>
        public string LastTransitionReason => _lastTransitionReason;
        
        /// <summary>
        /// Gets the elapsed time in the current state.
        /// </summary>
        public double StateElapsedTime => _stateElapsedTime;
        
        /// <summary>
        /// Event raised when the state changes.
        /// </summary>
        public event Action<EffectState, EffectState, string> OnStateChanged;
        
        /// <summary>
        /// Initializes a new effect state machine.
        /// </summary>
        public EffectStateMachine()
        {
            _currentState = EffectState.Idle;
            _previousState = EffectState.Idle;
            _lastTransitionReason = "Initial state";
            _stateElapsedTime = 0.0;
        }
        
        /// <summary>
        /// Starts the effect (Idle -> Active).
        /// </summary>
        /// <param name="reason">The transition reason (optional diagnostic).</param>
        /// <returns>True if transition was successful.</returns>
        public bool Start(string reason = null)
        {
            if (_currentState != EffectState.Idle)
                return false;
            
            TransitionTo(EffectState.Active, reason ?? "Start");
            return true;
        }
        
        /// <summary>
        /// Triggers the special state (Active -> Special).
        /// </summary>
        /// <param name="reason">The transition reason (optional diagnostic).</param>
        /// <returns>True if transition was successful.</returns>
        public bool TriggerSpecial(string reason = null)
        {
            if (_currentState != EffectState.Active)
                return false;
            
            TransitionTo(EffectState.Special, reason ?? "TriggerSpecial");
            return true;
        }
        
        /// <summary>
        /// Enters cooldown state (Active/Special -> Cooldown).
        /// </summary>
        /// <param name="reason">The transition reason (optional diagnostic).</param>
        /// <returns>True if transition was successful.</returns>
        public bool EnterCooldown(string reason = null)
        {
            if (_currentState != EffectState.Active && _currentState != EffectState.Special)
                return false;
            
            TransitionTo(EffectState.Cooldown, reason ?? "EnterCooldown");
            return true;
        }
        
        /// <summary>
        /// Resets the state machine to Idle.
        /// Idempotent operation - always succeeds.
        /// </summary>
        /// <param name="reason">The transition reason (optional diagnostic).</param>
        /// <returns>True if transition was successful.</returns>
        public bool Reset(string reason = null)
        {
            TransitionTo(EffectState.Idle, reason ?? "Reset");
            return true;
        }
        
        /// <summary>
        /// Stops the effect (any state -> Idle).
        /// Idempotent operation - always succeeds.
        /// </summary>
        /// <param name="reason">The transition reason (optional diagnostic).</param>
        /// <returns>True if transition was successful.</returns>
        public bool Stop(string reason = null)
        {
            TransitionTo(EffectState.Idle, reason ?? "Stop");
            return true;
        }
        
        /// <summary>
        /// Updates the state machine.
        /// Zero allocations.
        /// Driven by external deltaTime (not integrated with SharedAnimationClock).
        /// </summary>
        /// <param name="deltaTime">Delta time in seconds.</param>
        public void Update(double deltaTime)
        {
            // Validate deltaTime
            if (double.IsNaN(deltaTime) || deltaTime < 0)
                deltaTime = 0;
            
            // Update state elapsed time
            _stateElapsedTime += deltaTime;
        }
        
        /// <summary>
        /// Transitions to a new state.
        /// Zero allocations.
        /// </summary>
        /// <param name="newState">The new state.</param>
        /// <param name="reason">The transition reason.</param>
        private void TransitionTo(EffectState newState, string reason)
        {
            if (_currentState == newState)
                return;
            
            _previousState = _currentState;
            _currentState = newState;
            _lastTransitionReason = reason ?? "Transition";
            _stateElapsedTime = 0.0;
            
            OnStateChanged?.Invoke(_previousState, _currentState, _lastTransitionReason);
        }
        
        /// <summary>
        /// Gets diagnostics information about the state machine.
        /// Allocation-safe: returns a struct, no heap allocation.
        /// </summary>
        /// <returns>Effect state machine diagnostics.</returns>
        public EffectStateMachineDiagnostics GetDiagnostics()
        {
            return new EffectStateMachineDiagnostics(
                _currentState,
                _previousState,
                _lastTransitionReason,
                _stateElapsedTime
            );
        }
        
        /// <summary>
        /// Validates the state machine configuration.
        /// Lightweight validation without Enum.IsDefined allocations.
        /// </summary>
        /// <returns>True if valid, false otherwise.</returns>
        public bool Validate()
        {
            // Validate state is within enum range
            int stateValue = (int)_currentState;
            int prevStateValue = (int)_previousState;
            
            if (stateValue < 0 || stateValue > MaxEffectStateValue)
                return false;
            
            if (prevStateValue < 0 || prevStateValue > MaxEffectStateValue)
                return false;
            
            // Validate elapsed time
            if (_stateElapsedTime < 0)
                return false;
            
            if (double.IsNaN(_stateElapsedTime))
                return false;
            
            return true;
        }
    }
    
    /// <summary>
    /// Effect state machine diagnostics information.
    /// Readonly struct for allocation-safe diagnostics reporting.
    /// </summary>
    public readonly struct EffectStateMachineDiagnostics
    {
        public readonly EffectState CurrentState;
        public readonly EffectState PreviousState;
        public readonly string LastTransitionReason;
        public readonly double StateElapsedTime;
        
        public EffectStateMachineDiagnostics(EffectState currentState, EffectState previousState, string lastTransitionReason, double stateElapsedTime)
        {
            CurrentState = currentState;
            PreviousState = previousState;
            LastTransitionReason = lastTransitionReason;
            StateElapsedTime = stateElapsedTime;
        }
    }
}

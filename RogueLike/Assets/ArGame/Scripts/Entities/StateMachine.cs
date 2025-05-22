using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Abstract base class for all states (Playable and Enemy).
/// Acts as the foundation for the State Pattern implementation.
/// </summary>
public abstract class State
{
    // The state machine that manages this state
    protected StateMachine stateMachine;

    // The GameObject that this state belongs to
    protected GameObject owner;

    // Flag to mark if this state is uninterruptible
    public bool IsUninterruptible { get; protected set; } = false;

    /// <summary>
    /// Constructor for creating a new state
    /// </summary>
    /// <param name="stateMachine">Reference to the StateMachine controlling this state</param>
    /// <param name="owner">The GameObject that owns this state</param>
    public State(StateMachine stateMachine, GameObject owner)
    {
        this.stateMachine = stateMachine;
        this.owner = owner;
    }

    // Virtual methods that can be overridden by derived states

    /// <summary>
    /// Called when entering this state. Use for initialization.
    /// </summary>
    public virtual void Enter() { }

    /// <summary>
    /// Called when exiting this state. Use for cleanup.
    /// </summary>
    public virtual void Exit() { }

    /// <summary>
    /// Called every frame for logic that needs to run continuously.
    /// </summary>
    public virtual void Update() { }

    /// <summary>
    /// Called at fixed intervals. Use for physics-based logic.
    /// </summary>
    public virtual void FixedUpdate() { }

    /// <summary>
    /// Checks if the state can be interrupted by another state.
    /// Override in derived classes to implement custom interruption logic.
    /// </summary>
    /// <param name="newState">The state that is attempting to interrupt this state</param>
    /// <returns>True if this state can be interrupted, false otherwise</returns>
    public virtual bool CanBeInterrupted(State newState)
    {
        return !IsUninterruptible;
    }
}

/// <summary>
/// State machine for managing Playable and Enemy states.
/// Implements the State Pattern to manage different behaviors and transitions.
/// Attach this to any GameObject that needs state management.
/// </summary>
public class StateMachine : MonoBehaviour
{
    // The currently active state
    protected State currentState;

    // Name of the current state for debugging
    public string CurrentStateName => currentState?.GetType().Name ?? "No State";

    // Flag to track if we're currently in a state transition
    private bool isTransitioning = false;

    /// <summary>
    /// Initialize the state machine with a starting state
    /// </summary>
    /// <param name="startingState">The initial state for this state machine</param>
    public void Initialize(State startingState)
    {
        currentState = startingState;
        currentState.Enter(); // Call Enter() on the initial state
        Debug.Log($"[StateMachine] Initialized with state: {CurrentStateName}");
    }

    /// <summary>
    /// Change from the current state to a new state
    /// </summary>
    /// <param name="newState">The new state to transition to</param>
    /// <returns>True if state change was successful, false if interrupted or in transition</returns>
    public bool ChangeState(State newState)
    {
        // Prevent recursive state changes
        if (isTransitioning)
        {
            Debug.LogWarning($"[StateMachine] Attempted state change to {newState.GetType().Name} while already transitioning");
            return false;
        }

        // Check if current state allows interruption
        if (currentState != null && !currentState.CanBeInterrupted(newState))
        {
            Debug.Log($"[StateMachine] State change blocked: {CurrentStateName} cannot be interrupted by {newState.GetType().Name}");
            return false;
        }

        isTransitioning = true;

        // Exit current state
        if (currentState != null)
        {
            Debug.Log($"[StateMachine] Exiting state: {CurrentStateName}");
            currentState.Exit();
        }

        // Change to new state
        currentState = newState;
        Debug.Log($"[StateMachine] Entering state: {currentState.GetType().Name}");

        // Enter new state
        currentState.Enter();

        isTransitioning = false;
        return true;
    }

    /// <summary>
    /// Force a state change regardless of interruption rules
    /// </summary>
    /// <param name="newState">The new state to transition to</param>
    public void ForceChangeState(State newState)
    {
        if (isTransitioning)
        {
            Debug.LogWarning($"[StateMachine] Force-changing state while already transitioning");
        }

        isTransitioning = true;

        // Exit current state
        if (currentState != null)
        {
            Debug.Log($"[StateMachine] Force exiting state: {CurrentStateName}");
            currentState.Exit();
        }

        // Change to new state
        currentState = newState;
        Debug.Log($"[StateMachine] Force entering state: {currentState.GetType().Name}");

        // Enter new state
        currentState.Enter();

        isTransitioning = false;
    }

    /// <summary>
    /// Called every frame, delegates to the current state's Update method
    /// </summary>
    private void Update()
    {
        if (currentState != null && !isTransitioning)
            currentState.Update();
    }

    /// <summary>
    /// Called at fixed intervals, delegates to the current state's FixedUpdate method
    /// </summary>
    private void FixedUpdate()
    {
        if (currentState != null && !isTransitioning)
            currentState.FixedUpdate();
    }
}

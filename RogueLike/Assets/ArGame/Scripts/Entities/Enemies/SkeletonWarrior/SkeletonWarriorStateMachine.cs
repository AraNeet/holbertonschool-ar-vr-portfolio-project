using UnityEngine;
using ArGame.Entities.Enemies.SkeletonWarrior;

/// <summary>
/// State machine for the SkeletonWarrior enemy.
/// Manages state transitions and behaviors for the skeleton enemy.
/// </summary>
public class SkeletonWarriorStateMachine : StateMachine
{
    [Header("Debug Options")]
    [SerializeField] private bool showStateTransitions = true;

    private SkeletonWarriorController controller;

    // Current state tracking

    private void Awake()
    {
        controller = GetComponent<SkeletonWarriorController>();
        if (controller == null)
        {
            Debug.LogError("SkeletonWarriorStateMachine requires a SkeletonWarriorController component!");
            enabled = false;
        }
    }

    // Use this instead of directly calling base.ChangeState
    public void ChangeToState(State newState)
    {
        // Track state transitions for debugging
        if (showStateTransitions && currentState != null)
        {
            string previousState = currentState.GetType().Name;
            string nextState = newState.GetType().Name;
            Debug.Log($"[{gameObject.name}] State changing: {previousState} -> {nextState}");
        }

        // Call the base class method to handle the actual state change
        base.ChangeState(newState);
    }
}
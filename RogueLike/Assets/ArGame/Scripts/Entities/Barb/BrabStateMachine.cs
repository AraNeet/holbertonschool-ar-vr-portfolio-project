using UnityEngine;

/// <summary>
/// State machine for the Barbarian playable character.
/// Extend this class to add Barb-specific state logic.
/// </summary>
public class BrabStateMachine : StateMachine
{
    // Allow access to current state for state checks in BrabPlayerController
    public new State currentState { get { return base.currentState; } }

    // Add Barb-specific state machine logic or properties here if needed
}
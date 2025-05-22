using UnityEngine;

/// <summary>
/// State machine for the Mage playable character.
/// Extend this class to add Mage-specific state logic.
/// </summary>
public class MageStateMachine : StateMachine
{
    // Allow access to current state for state checks in MagePlayerController
    public new State currentState { get { return base.currentState; } }

    // Add Mage-specific state machine logic or properties here if needed
}
# Player Controller System

This document explains how to set up and use the player controller system in the RogueLike AR/VR game.

## Overview

The player controller system is a complete solution for controlling the player character with:
- Support for both keyboard/mouse and touch controls
- Animation system integration
- Health and damage system
- Clean, component-based architecture

## Components & Classes

The player controller system consists of the following key classes:

### Core Components:
- `PlayerController`: Main controller responsible for movement and interactions
- `InputHandler`: Handles input from both keyboard/mouse and touch controls
- `PlayerAnimator`: Manages character animations
- `PlayerHealth`: Manages health, damage and death

### Mobile-Specific Components:
- `TouchJoystick`: Implements a virtual joystick for movement/camera
- `TouchButton`: Implements touch buttons for actions
- `MobileControlsManager`: Manages mobile UI controls and integration

## Setup Instructions

### 1. Basic Player Setup

1. Create an empty GameObject named "Player"
2. Add the following components to it:
   - Character Controller
   - PlayerController
   - PlayerHealth
   - InputHandler

3. Structure your player hierarchy as follows:
```
Player (GameObject)
├── Model (GameObject containing your character model)
│   └── [Add PlayerAnimator component here]
├── CameraTarget (Empty GameObject for camera following)
```

### 2. Animation Setup

1. Create an Animator Controller for your player character
2. Set up the following parameters in your Animator:
   - `Speed` (float): Controls movement animation speed
   - `Jump` (trigger): Triggers jump animation
   - `Grounded` (bool): Indicates if player is grounded
   - `Interact` (trigger): Triggers interaction animation
   - `Hit` (trigger): Triggers damage animation
   - `Death` (trigger): Triggers death animation

3. Create transitions between your animation states using these parameters
4. Assign the Animator Controller to your character model's Animator component

### 3. Mobile Controls Setup

1. Create a Canvas object with UI elements for mobile controls:
   - Create a joystick UI (background + handle) for movement
   - Create another joystick UI for camera control (optional)
   - Add jump and interact buttons

2. Add the `TouchJoystick` component to each joystick
3. Add the `TouchButton` component to each button
4. Create a new GameObject and add the `MobileControlsManager` component
5. Assign your UI elements to the MobileControlsManager in the Inspector

## Usage Examples

### Controlling the Player

The player can be controlled using different input methods:

**Keyboard/Mouse:**
- WASD / Arrow Keys: Movement
- Space: Jump
- E: Interact
- Right Mouse Button + Mouse Movement: Look around

**Mobile Controls:**
- Left virtual joystick: Movement
- Right virtual joystick or swipe: Look around
- Jump Button: Jump
- Interact Button: Interact with objects

### Integrating with Your Game

Here are some examples of how to use the player controller in your game:

#### Teleporting the Player to a New Location

```csharp
PlayerController playerController = FindObjectOfType<PlayerController>();
playerController.TeleportTo(newPosition);
```

#### Applying Damage to the Player

```csharp
PlayerHealth playerHealth = FindObjectOfType<PlayerHealth>();
playerHealth.TakeDamage(10f);
```

#### Listening for Player Death

```csharp
PlayerHealth playerHealth = FindObjectOfType<PlayerHealth>();
playerHealth.onDeath.AddListener(() => {
    // Handle player death
    GameManager.Instance.GameOver();
});
```

#### Adding New Interactable Objects

Implement the `IInteractable` interface in your object's class:

```csharp
public class TreasureChest : MonoBehaviour, IInteractable
{
    public void Interact(PlayerController player)
    {
        // Handle interaction
        Open();
        GiveReward(player);
    }
}
```

## Customizing the Player Controller

The player controller system is designed to be modular and extensible. Here are some ways you can customize it:

### Adding New Abilities

1. Add new input handling in the `InputHandler` class
2. Implement the ability logic in the `PlayerController`
3. Add new animation triggers in the `PlayerAnimator`

### Changing Movement Mechanics

Modify the `ProcessMovement()` method in the `PlayerController` class to adjust:
- Movement speed
- Jump height
- Air control
- Gravity

### Adding Power-ups

Create methods in the `PlayerController` to apply temporary effects:
```csharp
// Example already included in PlayerController
public void ApplySpeedBoost(float multiplier, float duration)
{
    StartCoroutine(SpeedBoostCoroutine(multiplier, duration));
}
```

## Troubleshooting

**Issue: Player not moving**
- Check that the InputHandler is correctly assigned
- Verify that your Animator doesn't have root motion enabled

**Issue: Animations not playing**
- Check Animator parameters match exactly with the names in PlayerAnimator
- Verify animation transitions are set up correctly

**Issue: Mobile controls not working**
- Make sure Canvas is set to Screen Space - Overlay
- Check that TouchJoystick components are properly configured
- Verify touch inputs are enabled in Project Settings 
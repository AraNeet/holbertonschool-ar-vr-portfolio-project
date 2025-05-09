# Creating a Simple Player Animator Controller

This guide explains how to set up a basic Animator Controller for use with the PlayerAnimator component.

## Step 1: Create Animator Controller Asset

1. Right-click in the Project window
2. Select Create > Animator Controller
3. Name it "PlayerAnimator"

## Step 2: Set Up Basic Parameters

Open the Animator Controller and add the following parameters:

| Parameter Name | Type    | Default | Description                      |
|----------------|---------|---------|----------------------------------|
| Speed          | Float   | 0       | Controls movement animation      |
| Jump           | Trigger | -       | Triggers jump animation          |
| Grounded       | Bool    | true    | Is the character on the ground?  |
| Interact       | Trigger | -       | Triggers interaction animation   |
| Hit            | Trigger | -       | Triggers damage reaction         |
| Death          | Trigger | -       | Triggers death animation         |

## Step 3: Create Basic States

Add the following animation states:

1. **Idle**
   - Base state when the character isn't moving

2. **Walking**
   - Played when Speed > 0.1 and Speed < 0.7

3. **Running**
   - Played when Speed >= 0.7

4. **Jump_Start**
   - Triggered by Jump parameter

5. **Jump_Loop**
   - Looping air state when not Grounded

6. **Jump_Land**
   - Transition when becoming Grounded again

7. **Interact**
   - Triggered by Interact parameter

8. **Hit**
   - Triggered by Hit parameter

9. **Death**
   - Triggered by Death parameter

## Step 4: Set Up State Transitions

### Movement Transitions:
- Idle → Walking: Speed > 0.1
- Walking → Running: Speed > 0.7
- Walking → Idle: Speed < 0.1
- Running → Walking: Speed < 0.7

### Jump Transitions:
- Any State → Jump_Start: Jump triggered
- Jump_Start → Jump_Loop: Exit Time 0.8
- Jump_Loop → Jump_Land: Grounded becomes true
- Jump_Land → Idle/Walking/Running: Exit Time 0.5 (based on Speed value)

### Interaction Transitions:
- Any State → Interact: Interact triggered
- Interact → Previous State: Exit Time 1.0

### Hit Transitions:
- Any State → Hit: Hit triggered
- Hit → Previous State: Exit Time 0.8

### Death Transition:
- Any State → Death: Death triggered

## Step 5: Set Transition Settings

For smooth transitions, configure each transition with:
- Appropriate exit times (when animation should transition out)
- Transition duration (0.1-0.25 seconds for most)
- Transition offset (usually 0)

## Step 6: Code Example for Animation Integration

Here's an example of how you would modify an animation parameter in code:

```csharp
using UnityEngine;
using ArGame.Player;

public class GameplayManager : MonoBehaviour
{
    [SerializeField] private PlayerAnimator playerAnimator;
    
    private void Start()
    {
        // Find player animator if not set
        if (playerAnimator == null)
            playerAnimator = FindObjectOfType<PlayerAnimator>();
    }
    
    public void PlayerJumped()
    {
        if (playerAnimator != null)
        {
            playerAnimator.TriggerJumpAnimation();
        }
    }
    
    public void PlayerLanded()
    {
        if (playerAnimator != null)
        {
            playerAnimator.SetGroundedState(true);
        }
    }
    
    public void UpdateMovementSpeed(float speed)
    {
        if (playerAnimator != null)
        {
            playerAnimator.UpdateMovementAnimation(speed);
        }
    }
}
```

## Step 7: Blend Trees (Optional)

For smoother movement animations, consider using a Blend Tree:

1. Right-click in the Animator and select Create State > From New Blend Tree
2. Name it "Movement"
3. Configure parameters:
   - Blend Type: 1D
   - Parameter: Speed
4. Add motion slots:
   - Motion 0: Idle animation, Threshold: 0
   - Motion 1: Walk animation, Threshold: 0.5
   - Motion 2: Run animation, Threshold: 1.0

This will smoothly blend between idle, walking, and running based on the Speed parameter.

## Common Issues

- **Animations not playing**: Ensure parameter names match exactly with what the PlayerAnimator is setting
- **Jerky transitions**: Add transition time to smooth between animations
- **Root motion conflicts**: Either disable root motion or ensure it's properly managed by the PlayerController

## Example Animator Structure

```
├── Base Layer
│   ├── Idle (default state)
│   ├── Movement (blend tree)
│   │   ├── Idle
│   │   ├── Walk
│   │   └── Run
│   ├── Jump_Start
│   ├── Jump_Loop
│   ├── Jump_Land
│   ├── Interact
│   ├── Hit
│   └── Death
```

This simple setup will work well with the provided PlayerAnimator component and provides a good foundation for adding more complex animations as needed. 
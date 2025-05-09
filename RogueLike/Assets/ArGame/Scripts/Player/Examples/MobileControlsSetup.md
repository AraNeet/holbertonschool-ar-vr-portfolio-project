# Mobile Controls Setup Guide

This guide explains how to set up mobile controls for the player controller system.

## Prerequisites

- Unity 2020.3 or newer
- TextMeshPro package installed
- The player controller scripts from the ArGame.Player namespace
- Basic understanding of Unity UI system

## Step 1: Create the Mobile Controls Canvas

1. Create a new Canvas in your scene:
   - GameObject > UI > Canvas
   - Name it "MobileControlsCanvas"

2. Configure the Canvas:
   - Canvas Scaler (Script) > UI Scale Mode: "Scale With Screen Size"
   - Reference Resolution: 1920 x 1080
   - Screen Match Mode: "Match Width or Height"
   - Match: 0.5 (to match both)
   - Render Mode: "Screen Space - Overlay"

3. Add an Event System if one doesn't exist:
   - GameObject > UI > Event System

## Step 2: Create the Movement Joystick

1. Create a Panel for the joystick background:
   - Create a UI Panel (GameObject > UI > Panel)
   - Name it "MovementJoystickBackground"
   - Set its RectTransform:
     - Width: 200, Height: 200
     - Anchors: Bottom Left
     - Pivot: 0.5, 0.5
     - Position: X: 200, Y: 200, Z: 0
   - Set Background color to semi-transparent (Alpha around 0.5)
   - Make it circular by adding a circular Image component

2. Create the joystick handle:
   - Create a UI Image inside the Background panel
   - Name it "MovementJoystickHandle"
   - Set its RectTransform:
     - Width: 80, Height: 80
     - Anchors: Middle Center
     - Position: X: 0, Y: 0, Z: 0
   - Use a circular sprite for the handle
   - Set color to white with alpha around 0.8

3. Add the TouchJoystick component to the background:
   - Select MovementJoystickBackground
   - Add Component > ArGame > Player > Mobile > TouchJoystick
   - Configure the parameters:
     - Movement Range: 50
     - Dead Zone: 0.1
     - Hide On Release: false (keep it visible)
     - Background Rect: assign MovementJoystickBackground RectTransform
     - Handle Rect: assign MovementJoystickHandle RectTransform
     - Handle Image: assign MovementJoystickHandle Image
     - Background Image: assign MovementJoystickBackground Image

## Step 3: Create the Look Joystick (optional)

1. Repeat the same steps as for the Movement Joystick, but:
   - Name it "LookJoystickBackground" and "LookJoystickHandle"
   - Place it on the bottom right: X: -200, Y: 200, Z: 0
   - Anchors: Bottom Right

2. Add the TouchJoystick component and configure it the same way as the movement joystick.

## Step 4: Create Action Buttons

### Jump Button

1. Create a Button:
   - GameObject > UI > Button
   - Name it "JumpButton"
   - Set its RectTransform:
     - Width: 120, Height: 120
     - Anchors: Bottom Right
     - Position: X: -250, Y: 350, Z: 0
   - Change the button sprite to something appropriate for jumping

2. Add a TextMeshPro text for the label:
   - Add Component > TextMeshPro - Text
   - Set text to "JUMP"
   - Configure font size and alignment as desired

3. Add the TouchButton component:
   - Add Component > ArGame > Player > Mobile > TouchButton
   - Configure parameters:
     - Highlight Scale: 1.1
     - Animation Duration: 0.1
     - Pressed Color: Slightly darker than normal

4. Add button click event:
   - In the Inspector, under the TouchButton component, find "On Button Pressed"
   - Click "+" to add a new event
   - Drag the button itself to the object field
   - Select function: TouchButton.NotifyJump()

### Interact Button

1. Create another Button:
   - Name it "InteractButton"
   - Set its RectTransform:
     - Width: 120, Height: 120
     - Anchors: Bottom Right
     - Position: X: -400, Y: 250, Z: 0
   - Change the button sprite to something appropriate for interaction

2. Add TextMeshPro text label with "INTERACT"

3. Add the TouchButton component with similar settings to the Jump button

4. Add button click event to call TouchButton.NotifyInteract()

## Step 5: Create the MobileControlsManager

1. Create an empty GameObject:
   - Name it "MobileControlsManager"

2. Add the MobileControlsManager component:
   - Add Component > ArGame > Player > MobileControlsManager

3. Assign references:
   - Movement Joystick: drag your MovementJoystickBackground (with TouchJoystick component)
   - Look Joystick: drag your LookJoystickBackground (if created)
   - Jump Button: drag your JumpButton
   - Interact Button: drag your InteractButton
   - Mobile Controls Canvas: drag your MobileControlsCanvas

4. Configure settings:
   - Auto Enable On Mobile: true (to automatically enable on mobile platforms)

## Step 6: Connect to Input Handler

1. Find your InputHandler component on your Player object

2. Make sure the Mobile Controls UI field in the InputHandler references your MobileControlsCanvas

## Step 7: Test Your Controls

### Testing in Editor

1. Enter Play mode
2. You can simulate touch by:
   - Using the mouse to interact with the joysticks
   - Clicking the action buttons

### Testing on a Mobile Device

1. Build and run the project on a mobile device
2. The controls should automatically be enabled

## Customizing Mobile Controls

### Visual Customization

- Replace the default UI sprites with custom images
- Adjust colors, transparency, and size to match your game's visual style
- Add visual feedback effects (e.g., button presses, joystick movement)

### Layout Customization

- Adjust button and joystick positions based on ergonomics
- Consider providing options for left-handed users
- Implement a layout editor for players to customize control positions

### Functional Customization

For more complex control schemes, you can:

1. Add more buttons for additional actions:
   ```csharp
   // In your TouchButton class
   public void NotifySpecialAbility()
   {
       if (FindObjectOfType<InputHandler>() is InputHandler inputHandler)
       {
           // Add this method to InputHandler
           inputHandler.OnSpecialAbilityButtonPressed();
       }
   }
   ```

2. Add gesture recognition for special moves:
   ```csharp
   // Example of detecting swipe gesture
   private Vector2 touchStartPos;
   
   public void OnPointerDown(PointerEventData eventData)
   {
       touchStartPos = eventData.position;
   }
   
   public void OnPointerUp(PointerEventData eventData)
   {
       Vector2 swipeDelta = eventData.position - touchStartPos;
       
       if (swipeDelta.magnitude > 100f) // Minimum swipe distance
       {
           // Determine swipe direction and trigger action
           if (Mathf.Abs(swipeDelta.x) > Mathf.Abs(swipeDelta.y))
           {
               // Horizontal swipe
               if (swipeDelta.x > 0)
                   NotifySwipeRight();
               else
                   NotifySwipeLeft();
           }
           else
           {
               // Vertical swipe
               if (swipeDelta.y > 0)
                   NotifySwipeUp();
               else
                   NotifySwipeDown();
           }
       }
   }
   ```

## Troubleshooting

- **Joystick not responding**: Check that the RectTransforms for background and handle are properly assigned
- **Buttons not triggering actions**: Verify that the button events are properly connected to the notification methods
- **Controls showing on desktop**: Check the Auto Enable On Mobile setting and platform detection logic
- **Joystick handle moving outside boundary**: Verify that movement range is set correctly

## Advanced Usage

### Adaptive Controls

You can make your controls adapt to different screen sizes with this approach:

```csharp
private void AdaptToScreenSize()
{
    // Get screen dimensions
    float screenWidth = Screen.width;
    float screenHeight = Screen.height;
    
    // Scale joystick size based on screen dimensions
    float joystickSize = Mathf.Min(screenWidth, screenHeight) * 0.25f;
    
    // Apply scaling to joystick RectTransform
    movementJoystickBackground.sizeDelta = new Vector2(joystickSize, joystickSize);
    
    // Position joystick relative to screen edges (percentage-based)
    float margin = joystickSize * 0.5f;
    movementJoystickBackground.anchoredPosition = new Vector2(margin, margin);
}
```

### Show/Hide Controls Based on Context

You can show or hide controls based on the current game state:

```csharp
public void SetControlsForGameplay()
{
    jumpButton.gameObject.SetActive(true);
    interactButton.gameObject.SetActive(true);
}

public void SetControlsForMenu()
{
    jumpButton.gameObject.SetActive(false);
    interactButton.gameObject.SetActive(false);
}
``` 
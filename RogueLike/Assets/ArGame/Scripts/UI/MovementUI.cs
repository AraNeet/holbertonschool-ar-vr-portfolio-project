using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Handles the layout and appearance of the movement controls UI
/// </summary>
public class MovementUI : MonoBehaviour
{
    [Header("Direction Buttons")]
    [SerializeField] private Transform directionalButtonsParent;
    [SerializeField] private Button upButton;
    [SerializeField] private Button downButton;
    [SerializeField] private Button leftButton;
    [SerializeField] private Button rightButton;
    [SerializeField] private Button upLeftButton;
    [SerializeField] private Button upRightButton;
    [SerializeField] private Button downLeftButton;
    [SerializeField] private Button downRightButton;

    [Header("Action Buttons")]
    [SerializeField] private Transform actionButtonsParent;
    [SerializeField] private Button sprintButton;
    [SerializeField] private Button attackButton;
    [SerializeField] private Button spinAttackButton;

    [Header("Appearance")]
    [SerializeField] private Color normalButtonColor = new Color(1f, 1f, 1f, 0.6f);
    [SerializeField] private Color pressedButtonColor = new Color(0.8f, 0.8f, 0.8f, 0.8f);
    [SerializeField] private Color actionButtonColor = new Color(1f, 0.6f, 0.6f, 0.7f);
    [SerializeField] private Color pressedActionButtonColor = new Color(1f, 0.4f, 0.4f, 0.8f);

    [Header("References")]
    [SerializeField] private GameObject player;

    // Player controller references
    private BrabPlayerController brabController;
    private MagePlayerController mageController;
    private bool hasFoundControllers = false;

    private void Start()
    {
        // Find player if not assigned
        if (player == null)
        {
            player = GameObject.FindGameObjectWithTag("Player");
        }

        if (player != null)
        {
            // Get player controllers
            brabController = player.GetComponent<BrabPlayerController>();
            mageController = player.GetComponent<MagePlayerController>();
            hasFoundControllers = true;
        }

        // Set up button appearance
        SetupButtonAppearance(upButton, normalButtonColor, pressedButtonColor);
        SetupButtonAppearance(downButton, normalButtonColor, pressedButtonColor);
        SetupButtonAppearance(leftButton, normalButtonColor, pressedButtonColor);
        SetupButtonAppearance(rightButton, normalButtonColor, pressedButtonColor);
        SetupButtonAppearance(upLeftButton, normalButtonColor, pressedButtonColor);
        SetupButtonAppearance(upRightButton, normalButtonColor, pressedButtonColor);
        SetupButtonAppearance(downLeftButton, normalButtonColor, pressedButtonColor);
        SetupButtonAppearance(downRightButton, normalButtonColor, pressedButtonColor);

        // Set up action button appearance
        SetupButtonAppearance(sprintButton, actionButtonColor, pressedActionButtonColor);
        SetupButtonAppearance(attackButton, actionButtonColor, pressedActionButtonColor);
        SetupButtonAppearance(spinAttackButton, actionButtonColor, pressedActionButtonColor);

        // When this UI is shown, disable keyboard/mouse input
        DisableKeyboardMouseInput();
    }

    private void OnEnable()
    {
        // When this UI is enabled, disable keyboard/mouse input
        DisableKeyboardMouseInput();
    }

    /// <summary>
    /// Disables keyboard/mouse input on player controllers
    /// </summary>
    private void DisableKeyboardMouseInput()
    {
        if (!hasFoundControllers)
        {
            // Try to find controllers again if they weren't found in Start
            if (player != null)
            {
                brabController = player.GetComponent<BrabPlayerController>();
                mageController = player.GetComponent<MagePlayerController>();
                hasFoundControllers = true;
            }
            else
            {
                player = GameObject.FindGameObjectWithTag("Player");
                if (player != null)
                {
                    brabController = player.GetComponent<BrabPlayerController>();
                    mageController = player.GetComponent<MagePlayerController>();
                    hasFoundControllers = true;
                }
            }
        }

        // Disable keyboard input on player controllers
        if (brabController != null)
        {
            brabController.disableKeyboardMouseInput = true;
        }

        if (mageController != null)
        {
            mageController.disableKeyboardMouseInput = true;
        }
    }

    /// <summary>
    /// Sets up the visual appearance of a button
    /// </summary>
    private void SetupButtonAppearance(Button button, Color normalColor, Color pressedColor)
    {
        if (button == null) return;

        // Get the button's image
        Image buttonImage = button.GetComponent<Image>();
        if (buttonImage != null)
        {
            // Set normal color
            ColorBlock colors = button.colors;
            colors.normalColor = normalColor;
            colors.pressedColor = pressedColor;
            colors.highlightedColor = normalColor;
            colors.selectedColor = pressedColor;
            button.colors = colors;

            // Apply normal color initially
            buttonImage.color = normalColor;
        }
    }

    /// <summary>
    /// Gets the button with the specified name
    /// </summary>
    public Button GetButton(string buttonName)
    {
        switch (buttonName.ToLower())
        {
            case "up": return upButton;
            case "down": return downButton;
            case "left": return leftButton;
            case "right": return rightButton;
            case "upleft": return upLeftButton;
            case "upright": return upRightButton;
            case "downleft": return downLeftButton;
            case "downright": return downRightButton;
            case "sprint": return sprintButton;
            case "attack": return attackButton;
            case "spinattack": return spinAttackButton;
            default: return null;
        }
    }

    /// <summary>
    /// Shows or hides the movement UI
    /// </summary>
    public void SetVisible(bool visible)
    {
        gameObject.SetActive(visible);
    }
}
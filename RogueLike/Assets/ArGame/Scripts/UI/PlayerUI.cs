using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;

/// <summary>
/// Manages the player's UI elements and coordinates between them
/// </summary>
public class PlayerUI : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField] private HealthBarUI healthBar;
    [SerializeField] private MovementUI movementUI;

    [Header("References")]
    [SerializeField] private GameObject player;
    [SerializeField] private Health playerHealth;
    [SerializeField] private MovementController playerMovement;

    // Movement direction vectors
    private readonly Vector3 UP = new Vector3(0, 0, 1);
    private readonly Vector3 DOWN = new Vector3(0, 0, -1);
    private readonly Vector3 LEFT = new Vector3(-1, 0, 0);
    private readonly Vector3 RIGHT = new Vector3(1, 0, 0);
    private readonly Vector3 UP_LEFT = new Vector3(-0.7071f, 0, 0.7071f);
    private readonly Vector3 UP_RIGHT = new Vector3(0.7071f, 0, 0.7071f);
    private readonly Vector3 DOWN_LEFT = new Vector3(-0.7071f, 0, -0.7071f);
    private readonly Vector3 DOWN_RIGHT = new Vector3(0.7071f, 0, -0.7071f);

    // Track which direction buttons are being pressed
    private bool isPressingDirection = false;
    private Vector3 currentDirection = Vector3.zero;

    // Button references
    private Button upButton;
    private Button downButton;
    private Button leftButton;
    private Button rightButton;
    private Button upLeftButton;
    private Button upRightButton;
    private Button downLeftButton;
    private Button downRightButton;
    private Button sprintButton;
    private Button attackButton;
    private Button spinAttackButton;

    // Player controller references
    private BrabPlayerController brabController;
    private BrabCombatController brabCombatController;
    private MagePlayerController mageController;
    private MageCombatController mageCombatController;

    // Spin attack charge tracking
    private bool isChargingSpinAttack = false;
    private float spinAttackChargeStartTime = 0f;
    private float spinAttackChargeThreshold = 1.0f;

    // Start is called before the first frame update
    private void Start()
    {
        // Find player if not assigned
        if (player == null)
        {
            player = GameObject.FindGameObjectWithTag("Player");
        }

        // Get player components if not assigned
        if (player != null)
        {
            if (playerHealth == null)
            {
                playerHealth = player.GetComponent<Health>();
            }

            if (playerMovement == null)
            {
                playerMovement = player.GetComponent<MovementController>();
            }

            // Get player controllers
            brabController = player.GetComponent<BrabPlayerController>();
            brabCombatController = player.GetComponent<BrabCombatController>();
            mageController = player.GetComponent<MagePlayerController>();
            mageCombatController = player.GetComponent<MageCombatController>();
        }

        // Initialize health bar if available
        if (healthBar == null)
        {
            healthBar = GetComponentInChildren<HealthBarUI>();
        }

        // Initialize movement UI if available
        if (movementUI == null)
        {
            movementUI = GetComponentInChildren<MovementUI>();
        }

        // Log error if player components not found
        if (playerHealth == null)
        {
            Debug.LogError("PlayerUI: Player Health component not found!");
        }

        if (playerMovement == null)
        {
            Debug.LogError("PlayerUI: Player MovementController component not found!");
        }

        // Get button references from the MovementUI component
        if (movementUI != null)
        {
            upButton = movementUI.GetButton("up");
            downButton = movementUI.GetButton("down");
            leftButton = movementUI.GetButton("left");
            rightButton = movementUI.GetButton("right");
            upLeftButton = movementUI.GetButton("upleft");
            upRightButton = movementUI.GetButton("upright");
            downLeftButton = movementUI.GetButton("downleft");
            downRightButton = movementUI.GetButton("downright");
            sprintButton = movementUI.GetButton("sprint");
            attackButton = movementUI.GetButton("attack");
            spinAttackButton = movementUI.GetButton("spinattack");

            // Set up the button event listeners
            SetupMovementButtons();
            SetupActionButtons();
        }
        else
        {
            Debug.LogWarning("PlayerUI: MovementUI component not found!");
        }
    }

    /// <summary>
    /// Sets up the movement button event listeners
    /// </summary>
    private void SetupMovementButtons()
    {
        if (playerMovement == null) return;

        // Set up press and release events for direction buttons
        SetupDirectionButton(upButton, UP);
        SetupDirectionButton(downButton, DOWN);
        SetupDirectionButton(leftButton, LEFT);
        SetupDirectionButton(rightButton, RIGHT);
        SetupDirectionButton(upLeftButton, UP_LEFT);
        SetupDirectionButton(upRightButton, UP_RIGHT);
        SetupDirectionButton(downLeftButton, DOWN_LEFT);
        SetupDirectionButton(downRightButton, DOWN_RIGHT);
    }

    /// <summary>
    /// Sets up a directional button with press and release events
    /// </summary>
    private void SetupDirectionButton(Button button, Vector3 direction)
    {
        if (button == null) return;

        // Add button press event trigger
        EventTrigger trigger = button.gameObject.GetComponent<EventTrigger>();
        if (trigger == null)
        {
            trigger = button.gameObject.AddComponent<EventTrigger>();
        }

        // Add pointer down event entry
        EventTrigger.Entry pointerDownEntry = new EventTrigger.Entry();
        pointerDownEntry.eventID = EventTriggerType.PointerDown;
        pointerDownEntry.callback.AddListener((data) =>
        {
            isPressingDirection = true;
            currentDirection = direction;
            playerMovement.SetMoveDirection(direction);
        });
        trigger.triggers.Add(pointerDownEntry);

        // Add pointer up event entry
        EventTrigger.Entry pointerUpEntry = new EventTrigger.Entry();
        pointerUpEntry.eventID = EventTriggerType.PointerUp;
        pointerUpEntry.callback.AddListener((data) =>
        {
            isPressingDirection = false;
            currentDirection = Vector3.zero;
            playerMovement.SetMoveDirection(Vector3.zero);
        });
        trigger.triggers.Add(pointerUpEntry);

        // Add pointer exit event to handle when touch/pointer exits the button
        EventTrigger.Entry pointerExitEntry = new EventTrigger.Entry();
        pointerExitEntry.eventID = EventTriggerType.PointerExit;
        pointerExitEntry.callback.AddListener((data) =>
        {
            if (isPressingDirection && currentDirection == direction)
            {
                isPressingDirection = false;
                currentDirection = Vector3.zero;
                playerMovement.SetMoveDirection(Vector3.zero);
            }
        });
        trigger.triggers.Add(pointerExitEntry);
    }

    /// <summary>
    /// Sets up the action button event listeners
    /// </summary>
    private void SetupActionButtons()
    {
        if (playerMovement == null) return;

        // Sprint button events
        if (sprintButton != null)
        {
            EventTrigger sprintTrigger = sprintButton.gameObject.GetComponent<EventTrigger>();
            if (sprintTrigger == null)
            {
                sprintTrigger = sprintButton.gameObject.AddComponent<EventTrigger>();
            }

            // Sprint button press event
            EventTrigger.Entry sprintDownEntry = new EventTrigger.Entry();
            sprintDownEntry.eventID = EventTriggerType.PointerDown;
            sprintDownEntry.callback.AddListener((data) =>
            {
                playerMovement.SetSprinting(true);
            });
            sprintTrigger.triggers.Add(sprintDownEntry);

            // Sprint button release event
            EventTrigger.Entry sprintUpEntry = new EventTrigger.Entry();
            sprintUpEntry.eventID = EventTriggerType.PointerUp;
            sprintUpEntry.callback.AddListener((data) =>
            {
                playerMovement.SetSprinting(false);
            });
            sprintTrigger.triggers.Add(sprintUpEntry);

            // Sprint button pointer exit event
            EventTrigger.Entry sprintExitEntry = new EventTrigger.Entry();
            sprintExitEntry.eventID = EventTriggerType.PointerExit;
            sprintExitEntry.callback.AddListener((data) =>
            {
                playerMovement.SetSprinting(false);
            });
            sprintTrigger.triggers.Add(sprintExitEntry);
        }

        // Attack button events
        if (attackButton != null)
        {
            attackButton.onClick.AddListener(() =>
            {
                PerformAttack();
            });
        }

        // Spin Attack button events with press and hold logic
        if (spinAttackButton != null)
        {
            EventTrigger spinTrigger = spinAttackButton.gameObject.GetComponent<EventTrigger>();
            if (spinTrigger == null)
            {
                spinTrigger = spinAttackButton.gameObject.AddComponent<EventTrigger>();
            }

            // Spin attack button press event - start charging
            EventTrigger.Entry spinDownEntry = new EventTrigger.Entry();
            spinDownEntry.eventID = EventTriggerType.PointerDown;
            spinDownEntry.callback.AddListener((data) =>
            {
                // Start the charging process
                isChargingSpinAttack = true;
                spinAttackChargeStartTime = Time.time;

                // Start a coroutine to check for charge completion
                StartCoroutine(CheckSpinAttackCharge());
            });
            spinTrigger.triggers.Add(spinDownEntry);

            // Spin attack button release event - perform regular spin attack if not charged enough
            EventTrigger.Entry spinUpEntry = new EventTrigger.Entry();
            spinUpEntry.eventID = EventTriggerType.PointerUp;
            spinUpEntry.callback.AddListener((data) =>
            {
                // If we were charging and released before charge threshold
                if (isChargingSpinAttack)
                {
                    float chargeTime = Time.time - spinAttackChargeStartTime;
                    if (chargeTime < spinAttackChargeThreshold)
                    {
                        // Not charged enough for charge spin attack, perform regular spin attack
                        PerformSpinAttack(false);
                    }

                    // Reset charging state
                    isChargingSpinAttack = false;
                }
            });
            spinTrigger.triggers.Add(spinUpEntry);

            // Spin attack button pointer exit event - cancel charging if pointer exits
            EventTrigger.Entry spinExitEntry = new EventTrigger.Entry();
            spinExitEntry.eventID = EventTriggerType.PointerExit;
            spinExitEntry.callback.AddListener((data) =>
            {
                // Cancel charging if pointer exits
                isChargingSpinAttack = false;
            });
            spinTrigger.triggers.Add(spinExitEntry);
        }
    }

    /// <summary>
    /// Monitors the spin attack charging process
    /// </summary>
    private IEnumerator CheckSpinAttackCharge()
    {
        while (isChargingSpinAttack)
        {
            // Check if we've charged long enough for a charge spin attack
            float chargeTime = Time.time - spinAttackChargeStartTime;
            if (chargeTime >= spinAttackChargeThreshold)
            {
                // Charged enough, perform charge spin attack

                PerformSpinAttack(true);

                // Reset charging state
                isChargingSpinAttack = false;
                yield break;
            }

            // Wait a bit before checking again
            yield return new WaitForSeconds(0.05f);
        }
    }

    /// <summary>
    /// Performs the appropriate attack based on player type
    /// </summary>
    private void PerformAttack()
    {
        // Barb player attack
        if (brabController != null && brabCombatController != null)
        {
            // Ensure keyboard input is disabled
            brabController.disableKeyboardMouseInput = true;
            brabCombatController.PerformNormalAttack();
            return;
        }

        // Mage player attack
        if (mageController != null && mageCombatController != null)
        {
            // Ensure keyboard input is disabled
            mageController.disableKeyboardMouseInput = true;
            // Use the primary attack for mage with auto-targeting enabled
            mageCombatController.PerformMagicAttack(true);
            return;
        }
    }

    /// <summary>
    /// Makes the health bar flash to indicate critical health
    /// </summary>
    /// <param name="duration">How long the flash effect should last</param>
    public void FlashHealthBar(float duration)
    {
        // Implement flashing effect when needed
        // This could be used when player takes critical damage
    }

    /// <summary>
    /// Shows or hides all UI elements
    /// </summary>
    /// <param name="visible">Whether UI should be visible</param>
    public void SetUIVisibility(bool visible)
    {
        gameObject.SetActive(visible);
    }

    /// <summary>
    /// Shows or hides the movement controls
    /// </summary>
    /// <param name="visible">Whether movement controls should be visible</param>
    public void SetMovementControlsVisibility(bool visible)
    {
        if (movementUI != null)
        {
            movementUI.gameObject.SetActive(visible);
        }
    }

    /// <summary>
    /// Performs a spin attack based on charge state
    /// </summary>
    /// <param name="isCharged">Whether to perform a charged spin attack</param>
    private void PerformSpinAttack(bool isCharged)
    {
        // Only works with Barbarian for now
        if (brabController != null && brabCombatController != null)
        {
            // Ensure keyboard input is disabled
            brabController.disableKeyboardMouseInput = true;

            if (isCharged)
            {
                // Perform charge spin attack
                brabCombatController.PerformSpinAttack();
                brabCombatController.PerformChargeSpinAttack();
            }
            else
            {
                // Perform regular spin attack
                brabCombatController.PerformSpinAttack();
            }
        }
    }
}
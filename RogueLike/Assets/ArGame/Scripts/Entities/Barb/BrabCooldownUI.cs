using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Simple UI to show the charge spin attack cooldown.
/// </summary>
public class BrabCooldownUI : MonoBehaviour
{
    public Image chargeSpinCooldownImage;

    private BrabCombatController combatController;
    private float cooldownStartTime;
    private bool isCoolingDown = false;

    private void Start()
    {
        combatController = GetComponent<BrabCombatController>();

        if (chargeSpinCooldownImage != null)
        {
            chargeSpinCooldownImage.fillAmount = 0f;
        }
    }

    private void Update()
    {
        // If the cooldown UI is assigned
        if (chargeSpinCooldownImage != null)
        {
            // Check if we can charge spin attack
            if (combatController.canChargeSpinAttack)
            {
                // If it was cooling down before, it's now ready
                if (isCoolingDown)
                {
                    chargeSpinCooldownImage.fillAmount = 0f;
                    isCoolingDown = false;
                }
            }
            else
            {
                // Start cooldown tracking if not already tracking
                if (!isCoolingDown)
                {
                    cooldownStartTime = Time.time;
                    isCoolingDown = true;
                }

                // Update fill amount based on cooldown progress (inverted so it empties)
                float elapsedTime = Time.time - cooldownStartTime;
                float fillAmount = 1f - (elapsedTime / combatController.chargeSpinAttackCooldown);
                fillAmount = Mathf.Clamp01(fillAmount);

                chargeSpinCooldownImage.fillAmount = fillAmount;
            }
        }
    }
}
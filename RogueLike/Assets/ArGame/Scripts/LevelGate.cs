using UnityEngine;

public class LevelGate : MonoBehaviour
{
    [Tooltip("The tag that identifies the player")]
    [SerializeField] private string playerTag = "Player";

    [Tooltip("Whether this gate requires all enemies to be defeated")]
    [SerializeField] private bool requireAllEnemiesDefeated = true;

    [Tooltip("Visual effect to play when the gate is activated")]
    [SerializeField] private ParticleSystem activationEffect;

    private bool isActivated = false;

    private void OnTriggerEnter(Collider other)
    {
        if (isActivated || !other.CompareTag(playerTag))
            return;

        if (requireAllEnemiesDefeated)
        {
            // Check if all enemies are defeated
            GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
            if (enemies.Length > 0)
            {
                // Display hint to player that all enemies must be defeated
                Debug.Log("Defeat all enemies to unlock the gate!");
                return;
            }
        }

        // Activate the gate
        ActivateGate();
    }

    private void ActivateGate()
    {
        isActivated = true;

        // Play activation effect if available
        if (activationEffect != null)
        {
            activationEffect.Play();
        }

        // Find the LevelsController and complete the current level
        LevelsControllers levelController = FindObjectOfType<LevelsControllers>();
        if (levelController != null)
        {
            levelController.CompleteCurrentLevel();
        }

        // Notify the GameManager to advance to the next level
        if (GameManager.Instance != null)
        {
            GameManager.Instance.AdvanceToNextLevel();
        }
    }

    // Call this method from enemy death events if tracking enemies manually
    public void CheckEnemiesDefeated()
    {
        if (requireAllEnemiesDefeated)
        {
            GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
            if (enemies.Length == 0)
            {
                // Auto-activate gate when all enemies are defeated
                ActivateGate();
            }
        }
    }
}
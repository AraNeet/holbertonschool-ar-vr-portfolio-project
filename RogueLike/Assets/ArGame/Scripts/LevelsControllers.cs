using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class LevelsControllers : MonoBehaviour
{
    [SerializeField] private string currentLevelTag = "Level-1"; // Default level

    private GameObject[] level1Objects;
    private GameObject[] level2Objects;
    private GameObject[] level3Objects;

    public UnityEvent<int> onLevelComplete = new UnityEvent<int>();
    private int currentLevelNumber = 1;

    // Start is called before the first frame update
    void Start()
    {
        // Find all objects with level tags
        level1Objects = GameObject.FindGameObjectsWithTag("Level-1");
        level2Objects = GameObject.FindGameObjectsWithTag("Level-2");
        level3Objects = GameObject.FindGameObjectsWithTag("Level-3");

        // Initial update of level visibility
        UpdateLevelVisibility();
    }

    // Call this method whenever player changes level
    public void SetCurrentLevel(string levelTag)
    {
        if (levelTag != currentLevelTag)
        {
            currentLevelTag = levelTag;

            // Parse level number from tag
            if (int.TryParse(levelTag.Substring(levelTag.IndexOf('-') + 1), out int levelNumber))
            {
                currentLevelNumber = levelNumber;
            }

            UpdateLevelVisibility();
        }
    }

    private void UpdateLevelVisibility()
    {
        // Enable/disable renderers based on current level
        SetLevelVisibility(level1Objects, currentLevelTag == "Level-1");
        SetLevelVisibility(level2Objects, currentLevelTag == "Level-2");
        SetLevelVisibility(level3Objects, currentLevelTag == "Level-3");
    }

    private void SetLevelVisibility(GameObject[] levelObjects, bool isVisible)
    {
        if (levelObjects == null) return;

        foreach (GameObject obj in levelObjects)
        {
            // Disable/enable renderers
            Renderer[] renderers = obj.GetComponentsInChildren<Renderer>();
            foreach (Renderer renderer in renderers)
            {
                renderer.enabled = isVisible;
            }

            // Enable/disable colliders if needed
            Collider[] colliders = obj.GetComponentsInChildren<Collider>();
            foreach (Collider collider in colliders)
            {
                collider.enabled = isVisible;
            }
        }
    }

    public void CompleteCurrentLevel()
    {
        onLevelComplete.Invoke(currentLevelNumber);

        // The GameManager will handle the actual level transition
    }

    public int GetCurrentLevelNumber()
    {
        return currentLevelNumber;
    }

    // Call this when all enemies on the level are defeated
    public void OnAllEnemiesDefeated()
    {
        CompleteCurrentLevel();
    }
}

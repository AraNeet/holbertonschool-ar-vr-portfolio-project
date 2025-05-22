using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("AR Components")]
    [SerializeField] private ARRaycastManager raycastManager;
    [SerializeField] private ARAnchorManager anchorManager;
    [SerializeField] private ARPlaneManager planeManager;

    [Header("Game Settings")]
    [SerializeField] private float respawnDelay = 3f;
    [SerializeField] private float fadeTransitionTime = 1.0f;
    [SerializeField] private int maxLevel = 3;
    [SerializeField] private float gameSize = 5.0f; // Size of the game area
    [SerializeField] private float gameHeight = 0.0f; // Height offset of the game from the detected plane
    [SerializeField] private Vector3 playerScale = Vector3.one; // Scale for player models
    [SerializeField] private Vector3 enemyScale = Vector3.one; // Scale for enemy models
    [SerializeField] private bool debugSpawning = false; // Enable debug logs for spawning

    [Header("Prefabs")]
    [SerializeField] private GameObject dungeonPrefab;
    [SerializeField] private GameObject playerMagePrefab;
    [SerializeField] private GameObject playerBarbPrefab;
    [SerializeField] private GameObject[] enemyPrefabs; // Array of enemy prefabs
    [SerializeField] private int minEnemiesPerLevel = 3; // Minimum number of enemies to spawn
    [SerializeField] private int maxEnemiesPerLevel = 6; // Maximum number of enemies to spawn

    [Header("UI References")]
    [SerializeField] private GameObject gameOverUI;
    [SerializeField] private GameObject characterSelectionUI;
    [SerializeField] private Slider gameSizeSlider;
    [SerializeField] private Slider gameHeightSlider;
    [SerializeField] private Image fadeImage;

    // Component references
    private ARPlacementManager arPlacementManager;
    private LevelsControllers levelController;
    private ARAnchor gameAnchor;
    private GameObject playerInstance;
    private GameObject dungeonInstance;
    private int currentLevel = 1;
    private bool isGamePlaced = false;
    private bool isMage = true; // Default to mage character

    // Spawn points
    private Transform playerSpawnPoint;
    private List<Transform> enemySpawnPoints = new List<Transform>();

    private void Awake()
    {
        // Singleton pattern
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        // Initialize UI elements
        if (gameOverUI != null)
        {
            gameOverUI.SetActive(false);
        }

        if (characterSelectionUI != null)
        {
            characterSelectionUI.SetActive(true);
        }

        if (fadeImage != null)
        {
            fadeImage.gameObject.SetActive(true);
            Color c = fadeImage.color;
            c.a = 0;
            fadeImage.color = c;
        }
    }

    private void Start()
    {
        InitializeComponents();

        // Initialize game size slider if available
        if (gameSizeSlider != null)
        {
            gameSizeSlider.value = gameSize;
            gameSizeSlider.onValueChanged.AddListener(UpdateGameSize);
        }

        // Initialize game height slider if available
        if (gameHeightSlider != null)
        {
            gameHeightSlider.value = gameHeight;
            gameHeightSlider.onValueChanged.AddListener(UpdateGameHeight);
        }
    }

    private void InitializeComponents()
    {
        arPlacementManager = new ARPlacementManager(raycastManager, anchorManager, planeManager);
    }

    private void Update()
    {
        // If game is already placed, skip placement logic
        if (isGamePlaced)
            return;

        // Handle placement tap
        if (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began)
        {
            Debug.Log($"Touch detected at {Input.GetTouch(0).position}");
            TryPlaceGame(Input.GetTouch(0).position);
        }
#if UNITY_EDITOR
        // Mouse input for editor testing
        if (Input.GetMouseButtonDown(0))
        {
            TryPlaceGame(Input.mousePosition);
        }
#endif
    }

    public void TryPlaceGame(Vector2 screenPosition)
    {
        // Only proceed if character selection is done
        if (characterSelectionUI.activeSelf)
        {
            Debug.Log("Character selection UI is still active, can't place game yet");
            return;
        }

        Debug.Log($"Trying to place game at screen position {screenPosition}");

        // Check if AR components are initialized
        if (raycastManager == null || anchorManager == null || planeManager == null)
        {
            Debug.LogError("AR components not initialized correctly!");
            return;
        }

        // Log plane tracking status
        if (debugSpawning)
        {
            Debug.Log($"AR Plane tracking status: {planeManager.enabled}, Planes detected: {planeManager.trackables.count}");
            foreach (var plane in planeManager.trackables)
            {
                Debug.Log($"Plane detected: {plane.trackableId}, alignment: {plane.alignment}, position: {plane.transform.position}");
            }
        }

        Pose pose;
        bool raycastHit = arPlacementManager.TryGetPlacementPose(screenPosition, out pose);
        Debug.Log($"Raycast hit: {raycastHit}, Pose position: {pose.position}");

        if (raycastHit)
        {
            // Apply height offset to the placement position
            Vector3 adjustedPosition = pose.position + new Vector3(0, gameHeight, 0);
            Pose adjustedPose = new Pose(adjustedPosition, pose.rotation);

            PlaceGame(adjustedPose);
        }
        else
        {
            Debug.Log("Could not find a valid placement position. Make sure you're pointing at a detected plane.");
        }
    }

    private void PlaceGame(Pose pose)
    {
        // Create anchor
        gameAnchor = arPlacementManager.CreateAnchor(pose);

        if (gameAnchor != null)
        {
            // Instantiate the dungeon and parent it to the anchor
            dungeonInstance = Instantiate(dungeonPrefab, pose.position, pose.rotation, gameAnchor.transform);
            dungeonInstance.transform.localScale = Vector3.one * gameSize;

            if (debugSpawning)
            {
                Debug.Log($"Dungeon instantiated: {dungeonInstance != null}, at position: {dungeonInstance?.transform.position}");
            }

            // Get level controller
            levelController = dungeonInstance.GetComponent<LevelsControllers>();
            if (levelController == null)
            {
                levelController = dungeonInstance.AddComponent<LevelsControllers>();
            }

            // Find spawn points
            FindSpawnPoints();

            // Spawn player at random spawn point
            SpawnPlayer();

            // Spawn enemies at random spawn points
            SpawnEnemies();

            // Hide planes and UI
            arPlacementManager.HidePlanes();

            isGamePlaced = true;
        }
    }

    private void FindSpawnPoints()
    {
        playerSpawnPoint = null;
        enemySpawnPoints.Clear();

        // Find spawn point transforms in the dungeon
        Transform spawnPointsParent = dungeonInstance.transform.Find("SpawnPoints");
        if (debugSpawning)
        {
            Debug.Log($"SpawnPoints parent found: {spawnPointsParent != null}");
        }

        // Check if we have existing spawn points in the prefab
        bool hasManualSpawnPoints = false;

        if (spawnPointsParent != null)
        {
            // Look for player spawn point
            Transform existingPlayerSpawn = spawnPointsParent.Find("PlayerSpawn");
            if (existingPlayerSpawn != null)
            {
                playerSpawnPoint = existingPlayerSpawn;
                hasManualSpawnPoints = true;
                if (debugSpawning)
                {
                    Debug.Log($"Found manual player spawn point at {playerSpawnPoint.localPosition}");
                }
            }

            // Look for enemy spawn points
            for (int i = 0; i < 10; i++) // Check for up to 10 enemy spawn points
            {
                Transform enemySpawn = spawnPointsParent.Find($"EnemySpawn_{i}");
                if (enemySpawn != null)
                {
                    enemySpawnPoints.Add(enemySpawn);
                    hasManualSpawnPoints = true;
                    if (debugSpawning)
                    {
                        Debug.Log($"Found manual enemy spawn point {i} at {enemySpawn.localPosition}");
                    }
                }
            }
        }

        // If manual spawn points were found, validate and use them
        if (hasManualSpawnPoints)
        {
            // If we have manual enemy spawns but no player spawn, create a player spawn
            if (playerSpawnPoint == null && enemySpawnPoints.Count > 0)
            {
                if (spawnPointsParent == null)
                {
                    spawnPointsParent = new GameObject("SpawnPoints").transform;
                    spawnPointsParent.SetParent(dungeonInstance.transform);
                    spawnPointsParent.localPosition = Vector3.zero;
                }

                GameObject manualPlayerSpawnObj = new GameObject("PlayerSpawn");
                manualPlayerSpawnObj.transform.parent = spawnPointsParent;

                // Place the player in the center
                manualPlayerSpawnObj.transform.localPosition = Vector3.zero;
                playerSpawnPoint = manualPlayerSpawnObj.transform;

                if (debugSpawning)
                {
                    Debug.Log($"Created player spawn at center because manual enemy spawns were found");
                }
            }

            // If we found player spawn but no enemy spawns, create enemy spawns
            if (playerSpawnPoint != null && enemySpawnPoints.Count == 0)
            {
                // Create enemy spawns using the automatic system
                CreateAutomaticEnemySpawns(spawnPointsParent);

                if (debugSpawning)
                {
                    Debug.Log($"Created automatic enemy spawns because only player spawn was found");
                }
            }

            // Check if all spawn points are far enough apart, if not, recreate enemy spawns
            bool spawnsTooClose = false;
            if (playerSpawnPoint != null && enemySpawnPoints.Count > 0)
            {
                float minSafeDistance = gameSize * 0.2f;

                foreach (Transform enemySpawn in enemySpawnPoints)
                {
                    if (Vector3.Distance(playerSpawnPoint.localPosition, enemySpawn.localPosition) < minSafeDistance)
                    {
                        spawnsTooClose = true;
                        if (debugSpawning)
                        {
                            Debug.Log($"Enemy spawn at {enemySpawn.localPosition} is too close to player");
                        }
                        break;
                    }
                }

                if (spawnsTooClose)
                {
                    Debug.LogWarning("Manual spawn points are too close! Recreating enemy spawns with safe distances.");
                    enemySpawnPoints.Clear();
                    CreateAutomaticEnemySpawns(spawnPointsParent);
                }
            }

            return; // Exit early since we handled manual spawn points
        }

        // If we get here, we need to create automatic spawn points
        if (spawnPointsParent == null)
        {
            spawnPointsParent = new GameObject("SpawnPoints").transform;
            spawnPointsParent.SetParent(dungeonInstance.transform);
            spawnPointsParent.localPosition = Vector3.zero;
        }

        // Create player spawn point first
        GameObject autoPlayerSpawnObj = new GameObject("PlayerSpawn");
        autoPlayerSpawnObj.transform.parent = spawnPointsParent;

        // Player spawns near center with some randomness
        float playerOffsetX = Random.Range(-gameSize * 0.2f, gameSize * 0.2f);
        float playerOffsetZ = Random.Range(-gameSize * 0.2f, gameSize * 0.2f);
        Vector3 playerSpawnPos = new Vector3(playerOffsetX, 0, playerOffsetZ);
        autoPlayerSpawnObj.transform.localPosition = playerSpawnPos;
        playerSpawnPoint = autoPlayerSpawnObj.transform;

        // Create enemy spawn points
        CreateAutomaticEnemySpawns(spawnPointsParent);
    }

    private void CreateAutomaticEnemySpawns(Transform parent)
    {
        // Define minimum distance between spawn points (as a fraction of game size)
        float minDistanceBetweenSpawns = gameSize * 0.25f;
        float safeSpawnRadius = gameSize * 0.4f; // Radius for safe spawn area
        List<Vector3> usedPositions = new List<Vector3>();

        // Add player position to used positions to prevent overlap
        if (playerSpawnPoint != null)
        {
            usedPositions.Add(playerSpawnPoint.localPosition);
        }

        // Determine number of enemies to spawn based on level
        int enemyCount = Mathf.Min(maxEnemiesPerLevel, minEnemiesPerLevel + currentLevel - 1);

        int attempts = 0;
        int maxAttempts = enemyCount * 10; // Limit attempts to prevent infinite loop

        for (int i = 0; i < enemyCount; i++)
        {
            GameObject spawnPoint = new GameObject($"EnemySpawn_{i}");
            spawnPoint.transform.parent = parent;

            Vector3 spawnPos = Vector3.zero;
            bool validPosition = false;

            // Try to find a valid position
            while (!validPosition && attempts < maxAttempts)
            {
                attempts++;

                // Generate position on the edge of the game area to keep enemies away from center
                float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
                float distance = Random.Range(safeSpawnRadius * 0.7f, safeSpawnRadius);

                spawnPos = new Vector3(
                    Mathf.Cos(angle) * distance,
                    0,
                    Mathf.Sin(angle) * distance
                );

                // Check if this position is far enough from other spawn points
                validPosition = true;
                foreach (Vector3 usedPos in usedPositions)
                {
                    if (Vector3.Distance(spawnPos, usedPos) < minDistanceBetweenSpawns)
                    {
                        validPosition = false;
                        break;
                    }
                }

                // Additional check to make sure it's within the game bounds
                if (validPosition)
                {
                    float halfSize = gameSize * 0.45f;
                    if (Mathf.Abs(spawnPos.x) > halfSize || Mathf.Abs(spawnPos.z) > halfSize)
                    {
                        validPosition = false;
                    }
                }
            }

            // If we couldn't find a valid position, use a fallback method
            if (!validPosition)
            {
                // Fallback: Place in one of the corners
                int corner = i % 4;
                float offset = gameSize * 0.35f;

                switch (corner)
                {
                    case 0: spawnPos = new Vector3(offset, 0, offset); break;     // Top-right
                    case 1: spawnPos = new Vector3(-offset, 0, offset); break;    // Top-left
                    case 2: spawnPos = new Vector3(-offset, 0, -offset); break;   // Bottom-left
                    case 3: spawnPos = new Vector3(offset, 0, -offset); break;    // Bottom-right
                }
            }

            // Set the position and add to used positions
            spawnPoint.transform.localPosition = spawnPos;
            usedPositions.Add(spawnPos);
            enemySpawnPoints.Add(spawnPoint.transform);
        }

        if (debugSpawning)
        {
            Debug.Log($"Created {enemySpawnPoints.Count} automatic enemy spawn points");
            for (int i = 0; i < enemySpawnPoints.Count; i++)
            {
                Debug.Log($"Enemy {i} spawn position: {enemySpawnPoints[i].localPosition}");
            }
        }
    }

    private void SpawnPlayer()
    {
        if (debugSpawning)
        {
            Debug.Log($"SpawnPlayer called, playerSpawnPoint: {playerSpawnPoint != null}");
        }

        if (playerSpawnPoint == null) return;

        // Instantiate selected player prefab
        GameObject prefabToSpawn = isMage ? playerMagePrefab : playerBarbPrefab;

        if (debugSpawning)
        {
            Debug.Log($"Player prefab to spawn: {prefabToSpawn != null}");
        }

        if (prefabToSpawn == null) return;

        playerInstance = Instantiate(prefabToSpawn, playerSpawnPoint.position, playerSpawnPoint.rotation);

        // Apply scale to player
        if (playerInstance != null)
        {
            playerInstance.transform.localScale = playerScale;
        }
    }

    private void SpawnEnemies()
    {
        if (debugSpawning)
        {
            Debug.Log($"SpawnEnemies called, enemyPrefabs count: {enemyPrefabs?.Length ?? 0}, spawnPoints: {enemySpawnPoints.Count}");
        }

        if (enemyPrefabs == null || enemyPrefabs.Length == 0)
        {
            Debug.LogError("No enemy prefabs assigned to GameManager!");
            return;
        }

        // Use EnemyManager if available
        EnemyManager enemyManager = FindObjectOfType<EnemyManager>();
        if (enemyManager != null)
        {
            // Pass the enemy prefabs to the EnemyManager along with scale
            enemyManager.SpawnEnemies(enemySpawnPoints.ToArray(), enemyPrefabs, currentLevel, enemyScale);
        }
        else
        {
            // Fallback to direct instantiation
            foreach (Transform spawnPoint in enemySpawnPoints)
            {
                // Select a random enemy prefab based on level difficulty
                int prefabIndex = GetEnemyPrefabIndexForLevel();
                GameObject selectedPrefab = enemyPrefabs[prefabIndex];

                GameObject enemy = Instantiate(selectedPrefab, spawnPoint.position, spawnPoint.rotation);

                // Apply scale to enemy
                if (enemy != null)
                {
                    enemy.transform.localScale = enemyScale;
                }
            }
        }
    }

    // Helper method to select appropriate enemy prefab based on level
    private int GetEnemyPrefabIndexForLevel()
    {
        if (enemyPrefabs.Length == 1) return 0; // If only one prefab, use it

        // Easier enemies more common in early levels, harder enemies more common in later levels
        float levelProgress = (float)currentLevel / maxLevel;

        // Weight selection toward harder enemies as levels progress
        float randomValue = Random.value;

        // Scale by level progress to bias toward harder enemies in later levels
        int preferredIndex = Mathf.FloorToInt(randomValue * enemyPrefabs.Length);

        // Sometimes use a completely random enemy
        if (Random.value < 0.3f)
        {
            return Random.Range(0, enemyPrefabs.Length);
        }

        // Sometimes use an enemy appropriate to the current level
        int levelBasedIndex = Mathf.FloorToInt(levelProgress * (enemyPrefabs.Length - 1));

        // Mix the two approaches for variety
        return Random.value < 0.7f ? levelBasedIndex : preferredIndex;
    }

    public void SetCharacter(bool isMage)
    {
        this.isMage = isMage;
        characterSelectionUI.SetActive(false);
    }

    private void UpdateGameSize(float size)
    {
        gameSize = size;
        if (dungeonInstance != null)
        {
            dungeonInstance.transform.localScale = Vector3.one * gameSize;
        }
    }

    private void UpdateGameHeight(float height)
    {
        gameHeight = height;

        // If the game is already placed, adjust its height
        if (isGamePlaced && dungeonInstance != null)
        {
            Vector3 currentPos = dungeonInstance.transform.position;
            Vector3 newPos = new Vector3(currentPos.x, gameAnchor.transform.position.y + height, currentPos.z);
            dungeonInstance.transform.position = newPos;
        }
    }

    // Method to adjust height after placement (can be called from UI)
    public void AdjustGameHeight(float heightDelta)
    {
        gameHeight += heightDelta;

        if (gameHeightSlider != null)
        {
            gameHeightSlider.value = gameHeight;
        }
        else
        {
            UpdateGameHeight(gameHeight);
        }
    }

    public void AdvanceToNextLevel()
    {
        currentLevel++;
        if (currentLevel > maxLevel)
        {
            // Game completed - could trigger a win state here
            currentLevel = 1;
        }

        StartCoroutine(TransitionToLevel($"Level-{currentLevel}"));
    }

    private IEnumerator TransitionToLevel(string levelTag)
    {
        // Fade out
        yield return StartCoroutine(FadeScreen(0, 1));

        // Change level
        if (levelController != null)
        {
            levelController.SetCurrentLevel(levelTag);
        }

        // Respawn player and enemies
        FindSpawnPoints();
        SpawnPlayer();
        SpawnEnemies();

        // Fade in
        yield return StartCoroutine(FadeScreen(1, 0));
    }

    private IEnumerator FadeScreen(float startAlpha, float endAlpha)
    {
        if (fadeImage == null) yield break;

        float elapsedTime = 0;
        Color color = fadeImage.color;

        while (elapsedTime < fadeTransitionTime)
        {
            elapsedTime += Time.deltaTime;
            float t = Mathf.Clamp01(elapsedTime / fadeTransitionTime);
            color.a = Mathf.Lerp(startAlpha, endAlpha, t);
            fadeImage.color = color;
            yield return null;
        }

        color.a = endAlpha;
        fadeImage.color = color;
    }

    public void PlayerDied()
    {
        if (gameOverUI != null)
        {
            gameOverUI.SetActive(true);
        }

        // Auto restart after delay
        StartCoroutine(RespawnAfterDelay());
    }

    private IEnumerator RespawnAfterDelay()
    {
        yield return new WaitForSeconds(respawnDelay);
        RestartGame();
    }

    public void RestartGame()
    {
        // Hide game over UI
        if (gameOverUI != null)
        {
            gameOverUI.SetActive(false);
        }

        // Reset level and respawn player
        currentLevel = 1;
        StartCoroutine(TransitionToLevel($"Level-{currentLevel}"));
    }


    // Add a manual placement method for emergency use
    public void PlaceGameAtCameraPosition()
    {
        if (characterSelectionUI.activeSelf)
        {
            Debug.Log("Character selection UI is still active, can't place game yet");
            characterSelectionUI.SetActive(false);
        }

        Debug.Log("Emergency placement: Placing game at camera position");

        // Get camera position and rotation
        Camera arCamera = Camera.main;
        if (arCamera == null)
        {
            Debug.LogError("No main camera found for emergency placement");
            return;
        }

        // Place 1 meter in front of camera
        Vector3 position = arCamera.transform.position + arCamera.transform.forward * 1.5f;
        position.y = 0; // Place at ground level

        Pose pose = new Pose(position, Quaternion.Euler(0, arCamera.transform.eulerAngles.y, 0));
        PlaceGame(pose);
    }
}

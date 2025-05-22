using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class EnemyManager : MonoBehaviour
{
    public static EnemyManager Instance { get; private set; }

    [SerializeField] private GameObject[] defaultEnemyPrefabs;

    private List<GameObject> activeEnemies = new List<GameObject>();
    private Vector3 currentEnemyScale = Vector3.one;

    public UnityEvent onAllEnemiesDefeated = new UnityEvent();

    private void Awake()
    {
        // Singleton pattern
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void RegisterEnemy(GameObject enemy)
    {
        if (!activeEnemies.Contains(enemy))
        {
            activeEnemies.Add(enemy);
        }
    }

    public void UnregisterEnemy(GameObject enemy)
    {
        if (activeEnemies.Contains(enemy))
        {
            activeEnemies.Remove(enemy);

            // Check if this was the last enemy
            if (activeEnemies.Count == 0)
            {
                onAllEnemiesDefeated.Invoke();

                // Notify level controller
                LevelsControllers levelController = FindObjectOfType<LevelsControllers>();
                if (levelController != null)
                {
                    levelController.OnAllEnemiesDefeated();
                }

                // Notify level gates
                LevelGate[] gates = FindObjectsOfType<LevelGate>();
                foreach (LevelGate gate in gates)
                {
                    gate.CheckEnemiesDefeated();
                }
            }
        }
    }

    public int GetEnemyCount()
    {
        return activeEnemies.Count;
    }

    public void SpawnEnemies(Transform[] spawnPoints)
    {
        SpawnEnemies(spawnPoints, defaultEnemyPrefabs, 1, Vector3.one);
    }

    public void SpawnEnemies(Transform[] spawnPoints, GameObject[] enemyPrefabs, int currentLevel)
    {
        SpawnEnemies(spawnPoints, enemyPrefabs, currentLevel, Vector3.one);
    }

    public void SpawnEnemies(Transform[] spawnPoints, GameObject[] enemyPrefabs, int currentLevel, Vector3 enemyScale)
    {
        // Validate input
        if (enemyPrefabs == null || enemyPrefabs.Length == 0)
        {
            if (defaultEnemyPrefabs == null || defaultEnemyPrefabs.Length == 0)
            {
                Debug.LogError("No enemy prefabs available to spawn!");
                return;
            }
            enemyPrefabs = defaultEnemyPrefabs;
        }

        // Store the scale for future spawns
        currentEnemyScale = enemyScale;

        // Clear existing enemies
        ClearAllEnemies();

        // Spawn new enemies at each spawn point
        foreach (Transform spawnPoint in spawnPoints)
        {
            int prefabIndex = GetEnemyPrefabIndexForLevel(enemyPrefabs.Length, currentLevel);
            SpawnEnemy(spawnPoint.position, spawnPoint.rotation, enemyPrefabs[prefabIndex]);
        }
    }

    public GameObject SpawnEnemy(Vector3 position, Quaternion rotation)
    {
        if (defaultEnemyPrefabs == null || defaultEnemyPrefabs.Length == 0)
        {
            Debug.LogError("No default enemy prefabs assigned to EnemyManager!");
            return null;
        }

        int randomIndex = Random.Range(0, defaultEnemyPrefabs.Length);
        return SpawnEnemy(position, rotation, defaultEnemyPrefabs[randomIndex]);
    }

    public GameObject SpawnEnemy(Vector3 position, Quaternion rotation, GameObject enemyPrefab)
    {
        if (enemyPrefab == null)
        {
            Debug.LogError("Null enemy prefab passed to SpawnEnemy!");
            return null;
        }

        GameObject enemy = Instantiate(enemyPrefab, position, rotation);

        // Apply scale to enemy
        enemy.transform.localScale = currentEnemyScale;

        RegisterEnemy(enemy);
        return enemy;
    }

    private int GetEnemyPrefabIndexForLevel(int prefabCount, int currentLevel)
    {
        if (prefabCount == 1) return 0; // If only one prefab, use it

        // Use a simple level-based algorithm to select enemies
        // Higher level = higher chance of stronger enemies

        // Calculate difficulty value between 0-1 based on level
        float difficulty = Mathf.Clamp01((currentLevel - 1) / 5f);

        // Easy enemies have higher chance in early levels
        if (Random.value > difficulty)
        {
            return Random.Range(0, prefabCount / 2 + 1);
        }
        // Harder enemies more common in later levels
        else
        {
            return Random.Range(prefabCount / 3, prefabCount);
        }
    }

    public void ClearAllEnemies()
    {
        foreach (GameObject enemy in activeEnemies)
        {
            if (enemy != null)
            {
                Destroy(enemy);
            }
        }

        activeEnemies.Clear();
    }
}
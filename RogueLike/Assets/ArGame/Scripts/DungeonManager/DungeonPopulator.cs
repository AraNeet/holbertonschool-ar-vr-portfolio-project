using System.Collections.Generic;
using UnityEngine;

public class DungeonPopulator
{
    private GameObject enemyPrefab;
    private GameObject treasurePrefab;
    private float enemySpawnChance;
    private float treasureSpawnChance;

    public DungeonPopulator(GameObject enemyPrefab, GameObject treasurePrefab, float enemySpawnChance, float treasureSpawnChance)
    {
        this.enemyPrefab = enemyPrefab;
        this.treasurePrefab = treasurePrefab;
        this.enemySpawnChance = enemySpawnChance;
        this.treasureSpawnChance = treasureSpawnChance;
    }

    public void PopulateDungeon(List<Room> rooms)
    {
        // Add enemies and treasure to rooms
        foreach (var room in rooms)
        {
            // Chance to spawn an enemy
            if (Random.value < enemySpawnChance && enemyPrefab != null)
            {
                SpawnEnemy(room);
            }

            // Chance to spawn treasure
            if (Random.value < treasureSpawnChance && treasurePrefab != null)
            {
                SpawnTreasure(room);
            }
        }
    }

    private void SpawnEnemy(Room room)
    {
        GameObject enemy = Object.Instantiate(enemyPrefab, room.roomObject.transform);
        enemy.transform.localPosition = Vector3.zero; // Center of room
        enemy.tag = "Enemy";

        // Randomize scale slightly
        float scale = Random.Range(0.8f, 1.2f);
        enemy.transform.localScale = Vector3.one * scale;

        // Random rotation
        enemy.transform.localRotation = Quaternion.Euler(0, Random.Range(0, 360), 0);
    }

    private void SpawnTreasure(Room room)
    {
        GameObject treasure = Object.Instantiate(treasurePrefab, room.roomObject.transform);
        treasure.transform.localPosition = Vector3.zero; // Center of room
        treasure.tag = "Treasure";

        // Random rotation
        treasure.transform.localRotation = Quaternion.Euler(0, Random.Range(0, 360), 0);
    }
} 
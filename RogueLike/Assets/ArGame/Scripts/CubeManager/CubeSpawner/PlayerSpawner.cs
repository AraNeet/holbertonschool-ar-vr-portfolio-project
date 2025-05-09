using UnityEngine;

public class PlayerSpawner
{
    private GameObject playerPrefab;
    private GameObject playerInstance;
    private Vector3 originalScale = Vector3.one;
    private float defaultScale = 1.0f;
    
    public PlayerSpawner(GameObject playerPrefab, float defaultScale = 1.0f)
    {
        this.playerPrefab = playerPrefab;
        this.defaultScale = defaultScale;
    }
    
    public GameObject SpawnPlayer(Vector3 spawnPosition, Transform parent = null, float scale = 0f)
    {
        // Use the default scale if none provided
        if (scale <= 0f)
        {
            scale = defaultScale;
        }
        
        // Check if player prefab is assigned
        if (playerPrefab == null)
        {
            Debug.LogError("Player prefab is not assigned!");
            return null;
        }
        
        // Destroy any existing player instance
        DestroyPlayer();
        
        // Handle the parent relationship properly
        if (parent != null)
        {
            // First instantiate without a position (using identity)
            playerInstance = Object.Instantiate(playerPrefab);
            
            // Set parent first
            playerInstance.transform.SetParent(parent);
            
            // Then set local position
            playerInstance.transform.localPosition = parent.InverseTransformPoint(spawnPosition);
            
            Debug.Log($"Player spawned with parent. World position: {playerInstance.transform.position}, Local position: {playerInstance.transform.localPosition}");
        }
        else
        {
            // No parent, just instantiate normally
            playerInstance = Object.Instantiate(playerPrefab, spawnPosition, Quaternion.identity);
        }
        
        // Store the original scale of the player
        originalScale = playerInstance.transform.localScale;
        
        // Apply scale if different from default
        if (scale != 1.0f)
        {
            ApplyPlayerScale(scale);
        }
        
        Debug.Log($"Player spawned at: {spawnPosition} with scale: {scale}");
        
        return playerInstance;
    }
    
    public void ApplyPlayerScale(float scaleFactor)
    {
        if (playerInstance != null)
        {
            playerInstance.transform.localScale = originalScale * scaleFactor;
            Debug.Log($"Player scaled to: {scaleFactor} of original size");
        }
    }
    
    public void ResetPlayerScale()
    {
        if (playerInstance != null)
        {
            playerInstance.transform.localScale = originalScale;
            Debug.Log("Player scale reset to original");
        }
    }
    
    public void DestroyPlayer()
    {
        if (playerInstance != null)
        {
            Object.Destroy(playerInstance);
            playerInstance = null;
        }
    }
    
    public GameObject GetPlayerInstance()
    {
        return playerInstance;
    }
} 
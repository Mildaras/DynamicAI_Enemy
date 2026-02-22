using UnityEngine;

/// <summary>
/// Debug utility to spawn enemies for testing.
/// Press L to spawn a Tank in front of the player.
/// </summary>
public class DebugSpawner : MonoBehaviour
{
    [Header("Debug Spawn Settings")]
    [Tooltip("Tank prefab to spawn")]
    public GameObject tankPrefab;
    
    [Tooltip("Distance in front of player to spawn")]
    public float spawnDistance = 5f;
    
    [Tooltip("Key to press to spawn a tank")]
    public KeyCode spawnKey = KeyCode.L;

    void Update()
    {
        if (Input.GetKeyDown(spawnKey))
        {
            SpawnTank();
        }
    }

    private void SpawnTank()
    {
        if (tankPrefab == null)
        {
            Debug.LogWarning("DebugSpawner: No tankPrefab assigned!");
            return;
        }

        // Calculate spawn position in front of player
        Vector3 spawnPos = transform.position + transform.forward * spawnDistance;
        spawnPos.y = transform.position.y; // Keep at player's Y level

        // Instantiate the tank
        GameObject tank = Instantiate(tankPrefab, spawnPos, Quaternion.identity);
        
        Debug.Log($"<color=cyan>[DEBUG] Spawned Tank at {spawnPos}</color>");
        
        // Optional: Make tank face player
        Vector3 dirToPlayer = (transform.position - tank.transform.position).normalized;
        if (dirToPlayer.sqrMagnitude > 0.01f)
        {
            tank.transform.rotation = Quaternion.LookRotation(dirToPlayer);
        }
    }
}

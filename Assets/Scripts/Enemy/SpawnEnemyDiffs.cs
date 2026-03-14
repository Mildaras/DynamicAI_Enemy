using System.Collections.Generic;
using UnityEngine;

public class SpawnEnemyDiffs : MonoBehaviour
{
    [Header("Enemy Prefabs")]
    public GameObject easy;
    public GameObject medium;
    public GameObject hard;


    private List<GameObject> enemies = new List<GameObject>();

    void Update()
    {
        
    }

    public void spawnEasy()
    {
        GameObject enemy = Instantiate(easy, transform.position, Quaternion.identity);
        enemy.SetActive(true);
        CaptureEnemyWeights(enemy);
    }

    public void spawnMedium()
    {
        GameObject enemy = Instantiate(medium, transform.position, Quaternion.identity);
        enemy.SetActive(true);
        CaptureEnemyWeights(enemy);
    }

    public void spawnHard()
    {
        GameObject enemy = Instantiate(hard, transform.position, Quaternion.identity);
        enemy.SetActive(true);
        CaptureEnemyWeights(enemy);
    }
    
    void CaptureEnemyWeights(GameObject enemy)
    {
        var controller = enemy.GetComponent<EnemyController>();
        if (controller != null && controller.role == EnemyRole.Main)
        {
            // Wait one frame for enemy to fully initialize
            StartCoroutine(CaptureAfterFrame());
        }
    }
    
    System.Collections.IEnumerator CaptureAfterFrame()
    {
        yield return null; // Wait one frame
        
        if (StatsOverlay.Instance != null)
        {
            StatsOverlay.Instance.CaptureCurrentWeights();
        }
    }

}
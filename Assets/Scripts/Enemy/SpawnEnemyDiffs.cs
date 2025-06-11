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
    }

    public void spawnMedium()
    {
        GameObject enemy = Instantiate(medium, transform.position, Quaternion.identity);
        enemy.SetActive(true);
    }

    public void spawnHard()
    {
        GameObject enemy = Instantiate(hard, transform.position, Quaternion.identity);
        enemy.SetActive(true);
    }

}
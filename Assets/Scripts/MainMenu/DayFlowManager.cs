using UnityEngine;
using TMPro;
using System.Collections;

public class DayFlowManager : MonoBehaviour
{
    public static DayFlowManager Instance { get; private set; }

    [Header("Map Prefabs")]
    public GameObject[] mapPrefabs;
    public Transform mapsParent;

    [Header("Character Prefabs")]
    public GameObject mainEnemyPrefab;

    [Header("Hub Area")]
    public GameObject graceArea;
    [Tooltip("Where in the hub the player should start/return")]
    public Transform hubSpawnPoint;

    [Header("Combat Spawns")]
    [Tooltip("These are only used as temporary markers inside each map prefab")]
    public string playerSpawnTag = "PlayerSpawn";
    public string enemySpawnTag  = "EnemySpawn";

    [Header("Game Over UI")]
    public CanvasGroup gameOverPanel;
    public TextMeshProUGUI daysSurvivedText;
    public float gameOverFadeDuration = 2f;

    private GameObject _currentMap, _enemy;
    private GameObject _player;
    private int _currentDay;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);

            // Find the one-and-only Player in the scene
            _player = GameObject.FindWithTag("Player");

            _currentDay = PlayerPrefs.GetInt("GameDay", 1);

            if (gameOverPanel)
            {
                Debug.Log("GameOverPanel found, setting up for fade-in.");
                gameOverPanel.alpha = 0f;
                gameOverPanel.blocksRaycasts = false;
            }
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    /// <summary>
    /// Called by your NPC or UI button to begin the day’s combat.
    /// </summary>
    public void StartDay()
    {
        // 1) Spawn the combat map and enemy, teleport the player
        SpawnCombat();

        // 2) Hide the hub area (which also hides the hub’s Player, UI, etc.)
        //graceArea?.SetActive(false);
    }

    private void SpawnCombat()
    {
        // a) Pick & instantiate a random map under mapsParent
        var prefab = mapPrefabs[Random.Range(0, mapPrefabs.Length)];
        _currentMap = Instantiate(prefab, mapsParent);

        var pMarker = _currentMap.transform.Find(playerSpawnTag);
        var eMarker = _currentMap.transform.Find(enemySpawnTag);

        if (pMarker == null || eMarker == null)
        {
            Debug.LogError("Spawn points not found inside the map prefab.");
            return;
        }


        // c) Teleport the existing player to the combat spawn
        if (_player != null && pMarker != null)
        {
            _player.transform.position = pMarker.position;
            _player.transform.rotation = pMarker.rotation;
        }

        // d) Re-parent the main camera (already on the player) remains intact—
        //    no action needed for the camera since the player GameObject stays the same.

        // e) Instantiate the combat enemy at the enemy spawn
        _enemy = Instantiate(mainEnemyPrefab, eMarker.position, eMarker.rotation);

        // f) Hook death events
        PlayerData.OnPlayerDeath += OnPlayerDeath;
        StartCoroutine(DelayedEnemySetup(_enemy));


        Debug.Log($"Day {_currentDay} started on {prefab.name}");
    }

    private void OnPlayerDeath()
    {
        PlayerData.OnPlayerDeath -= OnPlayerDeath;
        Debug.Log("[DayFlowManager] Player died → Game Over");

        // tear down combat
        CleanupCombat();

        // show Game Over UI in-scene
        StartCoroutine(ShowGameOver());
    }

    private void OnEnemyDeath()
    {
        var eComp = _enemy.GetComponent<Enemy>();
        if (eComp != null)
            eComp.OnDeath -= OnEnemyDeath;

        Debug.Log("[DayFlowManager] Enemy died → Back to hub");

        // clean up the combat scene
        CleanupCombat();

        // increment day count
        _currentDay++;
        PlayerPrefs.SetInt("GameDay", _currentDay);

        // auto-save progress after surviving a day
        GameSaveManager.SaveGame();

        // 1) teleport player back to the hub spawn
        if (_player != null && hubSpawnPoint != null)
        {
            _player.transform.position = hubSpawnPoint.position;
            _player.transform.rotation = hubSpawnPoint.rotation;
        }
    }

    private void CleanupCombat()
    {
        if (_enemy != null) Destroy(_enemy);
        if (_currentMap != null) Destroy(_currentMap);
    }

    private IEnumerator ShowGameOver()
    {
        int survived = Mathf.Max(0, _currentDay - 1);
        if (daysSurvivedText != null)
            daysSurvivedText.text = $"Days Survived: {survived}";

        gameOverPanel.blocksRaycasts = true;
        float t = 0f;
        while (t < gameOverFadeDuration)
        {
            t += Time.unscaledDeltaTime;
            gameOverPanel.alpha = Mathf.Clamp01(t / gameOverFadeDuration);
            yield return null;
        }
        gameOverPanel.alpha = 1f;
    }

    private IEnumerator DelayedEnemySetup(GameObject enemy)
    {
        yield return null; // wait 1 frame so map and player are positioned

        if (enemy.TryGetComponent(out Enemy eComp))
        {
            eComp.OnDeath += OnEnemyDeath;

        }
    }

}
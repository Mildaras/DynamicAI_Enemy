using System;
using System.IO;
using UnityEngine;

/// <summary>
/// Manages game sessions for AI learning.
/// Each session starts clean, logs data, and saves results with timestamp.
/// </summary>
public class SessionManager : MonoBehaviour
{
    public static SessionManager Instance { get; private set; }

    [Header("Session Settings")]
    [Tooltip("Automatically clear logs at start of each session")]
    public bool clearLogsOnStart = true;

    [Tooltip("Automatically save session summary when game ends")]
    public bool autoSaveOnExit = true;

    private string _sessionId;
    private string _sessionDirectory;
    private string _currentSessionFile;
    private DateTime _sessionStartTime;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void Start()
    {
        StartNewSession();
    }

    void OnApplicationQuit()
    {
        if (autoSaveOnExit)
        {
            EndSession();
        }
    }

    /// <summary>
    /// Start a new clean session.
    /// </summary>
    public void StartNewSession()
    {
        _sessionStartTime = DateTime.Now;
        _sessionId = _sessionStartTime.ToString("yyyy.MM.dd-HH.mm");
        
        // Create sessions directory
        string baseDir = Path.Combine(Application.persistentDataPath, "Sessions");
        if (!Directory.Exists(baseDir))
        {
            Directory.CreateDirectory(baseDir);
        }

        _sessionDirectory = Path.Combine(baseDir, $"Session_{_sessionId}");
        Directory.CreateDirectory(_sessionDirectory);

        _currentSessionFile = Path.Combine(_sessionDirectory, "SessionSummary.txt");

        AdaptiveLogger.Critical($"=== NEW SESSION STARTED ===");
        AdaptiveLogger.Critical($"Session ID: {_sessionId}");
        AdaptiveLogger.Critical($"Session folder: {_sessionDirectory}");

        // Clear action logs for clean start
        if (clearLogsOnStart && ActionLogger.Instance != null)
        {
            ActionLogger.Instance.ClearAllLogs();
            AdaptiveLogger.Important("Cleared action logs - starting fresh session");
        }

        // Write session start info
        WriteSessionInfo("SESSION_START", $"Started at {_sessionStartTime:yyyy-MM-dd HH:mm:ss}");
    }

    /// <summary>
    /// End current session and save all data.
    /// </summary>
    public void EndSession()
    {
        if (string.IsNullOrEmpty(_sessionId))
        {
            AdaptiveLogger.Warning("No active session to end");
            return;
        }

        DateTime endTime = DateTime.Now;
        TimeSpan duration = endTime - _sessionStartTime;

        AdaptiveLogger.Critical($"=== SESSION ENDED ===");
        AdaptiveLogger.Critical($"Duration: {duration.TotalMinutes:F1} minutes");
        AdaptiveLogger.Critical($"Results saved to: {_sessionDirectory}");

        // Copy action log to session folder
        string actionLogPath = Path.Combine(Application.persistentDataPath, "ActionLog.json");
        if (File.Exists(actionLogPath))
        {
            string sessionActionLog = Path.Combine(_sessionDirectory, "ActionLog.json");
            File.Copy(actionLogPath, sessionActionLog, true);
        }

        // Write session end info
        WriteSessionInfo("SESSION_END", 
            $"Ended at {endTime:yyyy-MM-dd HH:mm:ss}\n" +
            $"Duration: {duration.Hours}h {duration.Minutes}m {duration.Seconds}s");

        // Create session index entry
        UpdateSessionIndex();
    }

    /// <summary>
    /// Save combat summary to current session.
    /// </summary>
    public void SaveCombatSummary(LogParser.Summary summary)
    {
        if (string.IsNullOrEmpty(_sessionDirectory)) return;

        string summaryFile = Path.Combine(_sessionDirectory, "CombatStats.txt");
        var lines = new[]
        {
            $"Combat Summary - Session {_sessionId}",
            $"Timestamp: {DateTime.Now:yyyy-MM-dd HH:mm:ss}",
            "=" + new string('=', 50),
            "",
            "=== PLAYER PERFORMANCE ===",
            $"Melee: {summary.PlayerMeleeSwings} swings, {summary.PlayerMeleeHits} hits ({summary.PlayerMeleeAccuracy:P1})",
            $"Ranged: {summary.PlayerProjectiles} fired, {summary.PlayerProjectileHits} hits ({summary.PlayerProjectileAccuracy:P1})",
            $"Telekinesis Kills: {summary.PlayerTelekinesisKills}",
            "",
            "=== ABILITIES ===",
            $"Blink: {summary.PlayerBlinkUses} (successes: {summary.PlayerBlinkSuccesses})",
            $"Stun: {summary.PlayerStunUses}",
            $"Reflect: {summary.PlayerReflectUses}",
            $"Smite: {summary.PlayerSmiteUses}",
            "",
            "=== ENEMY PERFORMANCE ===",
            $"Melee Attacks: {summary.EnemyMeleeAttacks}",
            $"Spells: {summary.EnemySpellCasts} cast, {summary.EnemySpellHits} hits, {summary.EnemySpellMisses} misses",
            $"Explode Attempts: {summary.EnemyExplodeAttempts}, Hits: {summary.EnemyExplodeHits}",
            "",
            "=== ENEMY TACTICS ===",
            $"Cover Attempts: {summary.EnemyCoverAttempts} (successes: {summary.EnemyCoverSuccesses})",
            $"Heal Uses: {summary.EnemyHealUses}",
            $"Wall Uses: {summary.EnemyWallUses}",
            "",
            "=== SUMMONS ===",
            $"Exploder: {summary.EnemySpawnExploder}",
            $"Bouncer: {summary.EnemySpawnBouncer}",
            $"Tank: {summary.EnemySpawnTank}",
            "",
            "=== CONTEXTUAL STATS ===",
            $"Average Player Health: {summary.AveragePlayerHealth:P0}",
            $"Average Enemy Health: {summary.AverageEnemyHealth:P0}",
            $"Player Aggression Score: {summary.PlayerAggressionScore:F2}",
            ""
        };

        File.WriteAllLines(summaryFile, lines);
        AdaptiveLogger.Important($"Combat summary saved to session folder");
    }

    /// <summary>
    /// Save weight changes to current session.
    /// </summary>
    public void SaveWeightChanges(string changeReport)
    {
        if (string.IsNullOrEmpty(_sessionDirectory)) return;

        string weightsFile = Path.Combine(_sessionDirectory, "WeightChanges.txt");
        File.WriteAllText(weightsFile, 
            $"Weight Adaptation - Session {_sessionId}\n" +
            $"Timestamp: {DateTime.Now:yyyy-MM-dd HH:mm:ss}\n" +
            new string('=', 60) + "\n\n" +
            changeReport);

        AdaptiveLogger.Important($"Weight changes saved to session folder");
    }

    private void WriteSessionInfo(string header, string content)
    {
        var lines = new[]
        {
            $"[{header}]",
            content,
            new string('-', 40),
            ""
        };
        File.AppendAllLines(_currentSessionFile, lines);
    }

    private void UpdateSessionIndex()
    {
        string indexFile = Path.Combine(Application.persistentDataPath, "Sessions", "SessionIndex.txt");
        string entry = $"{_sessionStartTime:yyyy-MM-dd HH:mm:ss} | Duration: {(DateTime.Now - _sessionStartTime).TotalMinutes:F1}min | Folder: Session_{_sessionId}";
        File.AppendAllText(indexFile, entry + Environment.NewLine);
    }

    public string GetSessionDirectory() => _sessionDirectory;
    public string GetSessionId() => _sessionId;
}

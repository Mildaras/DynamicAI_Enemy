using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

public class ActionLogger : MonoBehaviour
{
    public static ActionLogger Instance;

    [Header("Logging Settings")]
    [Tooltip("How often (seconds) to save log to disk and trigger AI adaptation. Lower = more frequent adaptation, higher = less I/O overhead. Recommended: 30-60s")]
    public float saveInterval = 60f;
    
    [Tooltip("Maximum number of recent events to keep in memory for real-time learning")]
    public int ringBufferSize = 200;
    
    [Header("Log Management")]
    [Tooltip("Maximum age of logs in hours before auto-cleanup")]
    public float maxLogAgeHours = 24f;
    
    [Tooltip("Clear logs after successful learning iteration")]
    public bool clearLogsAfterLearning = true;

    // In-memory ring buffer for real-time access
    private ActionRecord[] ringBuffer;
    private int ringBufferHead = 0;
    private int ringBufferCount = 0;
    
    // Pending actions to be written to disk
    private List<ActionRecord> pendingDiskWrites = new List<ActionRecord>();
    private string filePath;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);

            // Initialize ring buffer
            ringBuffer = new ActionRecord[ringBufferSize];
            ringBufferHead = 0;
            ringBufferCount = 0;

            filePath = Path.Combine(Application.persistentDataPath, "ActionsLog.json");
            Debug.Log($"[ActionLogger] Logging to: {filePath} | Ring buffer size: {ringBufferSize}");
            
            // Clean up old logs on startup
            CleanupOldLogs();

            StartCoroutine(AutoSaveCoroutine());
        }
        else
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// Call this from anywhere to record an action (basic version).
    /// </summary>
    public void LogAction(
        string actor,
        string actionType,
        string target = "",
        bool isHit = false,
        float damage = 0f,
        float distance = 0f
    )
    {
        var timestamp = Time.time;
        var record = new ActionRecord(
            timestamp,
            actor,
            actionType,
            target,
            isHit,
            damage,
            distance
        );
        
        // Add to ring buffer for real-time access
        AddToRingBuffer(record);
        
        // Add to pending disk writes
        pendingDiskWrites.Add(record);
    }
    
    /// <summary>
    /// Call this to record an action with full context data.
    /// </summary>
    public void LogActionWithContext(
        string actor,
        string actionType,
        string target = "",
        bool isHit = false,
        float damage = 0f,
        float distance = 0f,
        float actorHealthPercent = -1f,
        float targetHealthPercent = -1f,
        string actorState = "Unknown",
        bool wasSuccessful = false
    )
    {
        var timestamp = Time.time;
        var record = new ActionRecord(
            timestamp,
            actor,
            actionType,
            target,
            isHit,
            damage,
            distance,
            actorHealthPercent,
            targetHealthPercent,
            actorState,
            wasSuccessful
        );
        
        // Add to ring buffer for real-time access
        AddToRingBuffer(record);
        
        // Add to pending disk writes
        pendingDiskWrites.Add(record);
    }
    
    /// <summary>
    /// Adds a record to the ring buffer (circular queue).
    /// </summary>
    private void AddToRingBuffer(ActionRecord record)
    {
        ringBuffer[ringBufferHead] = record;
        ringBufferHead = (ringBufferHead + 1) % ringBufferSize;
        
        if (ringBufferCount < ringBufferSize)
            ringBufferCount++;
    }
    
    /// <summary>
    /// Get the N most recent events from the ring buffer.
    /// </summary>
    public List<ActionRecord> GetRecentEvents(int count = 50)
    {
        count = Mathf.Min(count, ringBufferCount);
        var result = new List<ActionRecord>(count);
        
        // Start from most recent and go backwards
        int index = (ringBufferHead - 1 + ringBufferSize) % ringBufferSize;
        for (int i = 0; i < count; i++)
        {
            result.Add(ringBuffer[index]);
            index = (index - 1 + ringBufferSize) % ringBufferSize;
        }
        
        result.Reverse(); // Return in chronological order
        return result;
    }
    
    /// <summary>
    /// Get events within a specific time window (in seconds).
    /// </summary>
    public List<ActionRecord> GetEventsInTimeWindow(float windowSeconds = 10f)
    {
        float currentTime = Time.time;
        float cutoffTime = currentTime - windowSeconds;
        var result = new List<ActionRecord>();
        
        // Scan ring buffer for events within time window
        for (int i = 0; i < ringBufferCount; i++)
        {
            var record = ringBuffer[i];
            if (record != null && record.Timestamp >= cutoffTime)
            {
                result.Add(record);
            }
        }
        
        return result.OrderBy(r => r.Timestamp).ToList();
    }
    
    /// <summary>
    /// Get all events in the ring buffer.
    /// </summary>
    public List<ActionRecord> GetAllRecentEvents()
    {
        return GetRecentEvents(ringBufferCount);
    }
    
    /// <summary>
    /// Set the log file path (used by SessionManager to log to session folders).
    /// </summary>
    public void SetLogFilePath(string newPath)
    {
        // Flush current pending writes before changing path
        if (pendingDiskWrites.Count > 0)
        {
            SaveAndClear();
        }
        
        filePath = newPath;
        Debug.Log($"[ActionLogger] Changed log path to: {filePath}");
    }
    
    /// <summary>
    /// Get the current log file path.
    /// </summary>
    public string GetLogFilePath() => filePath;
    
    /// <summary>
    /// Clear all logged data (ring buffer and disk file).
    /// Call this after AI has successfully learned from the data.
    /// </summary>
    public void ClearAllLogs()
    {
        // Clear ring buffer
        ringBuffer = new ActionRecord[ringBufferSize];
        ringBufferHead = 0;
        ringBufferCount = 0;
        
        // Clear pending writes
        pendingDiskWrites.Clear();
        
        // Delete disk file
        if (File.Exists(filePath))
        {
            File.Delete(filePath);
            Debug.Log("[ActionLogger] Cleared all logs after learning.");
        }
    }
    
    /// <summary>
    /// Cleanup logs older than maxLogAgeHours.
    /// </summary>
    private void CleanupOldLogs()
    {
        if (!File.Exists(filePath))
            return;
            
        try
        {
            var fileInfo = new FileInfo(filePath);
            var age = DateTime.Now - fileInfo.LastWriteTime;
            
            if (age.TotalHours > maxLogAgeHours)
            {
                File.Delete(filePath);
                Debug.Log($"[ActionLogger] Deleted old log file (age: {age.TotalHours:F1} hours)");
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[ActionLogger] Failed to cleanup old logs: {e.Message}");
        }
    }

    /// <summary>
    /// Manually trigger a save (rarely needed now).
    /// </summary>
    public void RunTheSave() => SaveAndClear();

    private IEnumerator AutoSaveCoroutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(saveInterval);
            SaveAndClear();
        }
    }

    private void OnApplicationQuit()
    {
        // ensure final flush
        SaveAndClear();
    }

    /// <summary>
    /// Reads the existing JSON file (if any), merges in new records
    /// (deduped & sorted), then writes the whole thing back out.
    /// After saving, triggers automatic AI adaptation.
    /// </summary>
    private void SaveAndClear()
    {
        if (pendingDiskWrites.Count == 0)
        {
            Debug.Log("[ActionLogger] No actions to save.");
            return;
        }

        // Dedupe & sort the newly logged actions
        var newBatch = pendingDiskWrites
            .Distinct()
            .OrderBy(a => a.Timestamp)
            .ToList();

        // Load existing file or start fresh
        ActionRecordList wrapper;
        if (File.Exists(filePath))
        {
            try
            {
                var existingJson = File.ReadAllText(filePath);
                wrapper = JsonUtility.FromJson<ActionRecordList>(existingJson)
                          ?? new ActionRecordList();
            }
            catch (Exception e)
            {
                Debug.LogError($"[ActionLogger] Failed to parse existing JSON, starting fresh: {e.Message}");
                wrapper = new ActionRecordList();
            }
        }
        else
        {
            wrapper = new ActionRecordList();
        }

        // Merge (avoid duplicates across saves)
        var all = wrapper.actions
            .Concat(newBatch)
            .Distinct()
            .OrderBy(a => a.Timestamp)
            .ToList();
        wrapper.actions = all;

        // Ensure directory exists
        Directory.CreateDirectory(Path.GetDirectoryName(filePath));

        // Serialize & overwrite
        try
        {
            var prettyJson = JsonUtility.ToJson(wrapper, true);
            File.WriteAllText(filePath, prettyJson);
            Debug.Log($"[ActionLogger] Saved {newBatch.Count} new actions (total {all.Count}) to {filePath} | Ring buffer: {ringBufferCount}/{ringBufferSize}");
        }
        catch (IOException e)
        {
            Debug.LogError($"[ActionLogger] Save failed: {e.Message}");
        }

        pendingDiskWrites.Clear();
        
        // Trigger automatic adaptation after saving
        TriggerAdaptation();
    }
    
    /// <summary>
    /// Notify CombatAnalytics to run adaptation automatically.
    /// </summary>
    private void TriggerAdaptation()
    {
        var analytics = FindObjectOfType<CombatAnalytics>();
        if (analytics != null)
        {
            analytics.RunAdaptation();
        }
    }
}

/// <summary>
/// Serializable wrapper so JsonUtility can (de)serialize our list as one object.
/// </summary>
[Serializable]
public class ActionRecordList
{
    public List<ActionRecord> actions = new List<ActionRecord>();
}

/// <summary>
/// Represents a generic in-game action (player or enemy).
/// Properties have been converted to public fields for serialization.
/// </summary>
[Serializable]
public class ActionRecord : IEquatable<ActionRecord>
{
    // Basic action data
    public float Timestamp;
    public string Actor;
    public string ActionType;
    public string Target;
    public bool IsHit;
    public float Damage;
    public float Distance;
    
    // Context data for learning
    public float ActorHealthPercent = -1f;
    public float TargetHealthPercent = -1f;
    public string DistanceCategory;
    public string ActorState;
    public bool WasSuccessful;

    // Basic constructor (backwards compatible)
    public ActionRecord(
        float timestamp,
        string actor,
        string actionType,
        string target,
        bool isHit,
        float damage,
        float distance
    )
    {
        Timestamp  = timestamp;
        Actor      = actor;
        ActionType = actionType;
        Target     = target;
        IsHit      = isHit;
        Damage     = damage;
        Distance   = distance;
        
        // Default context values
        ActorHealthPercent = -1f;
        TargetHealthPercent = -1f;
        DistanceCategory = CategorizeDistance(distance);
        ActorState = "Unknown";
        WasSuccessful = isHit;
    }
    
    // Full constructor with context
    public ActionRecord(
        float timestamp,
        string actor,
        string actionType,
        string target,
        bool isHit,
        float damage,
        float distance,
        float actorHealthPercent,
        float targetHealthPercent,
        string actorState,
        bool wasSuccessful
    )
    {
        Timestamp  = timestamp;
        Actor      = actor;
        ActionType = actionType;
        Target     = target;
        IsHit      = isHit;
        Damage     = damage;
        Distance   = distance;
        ActorHealthPercent = actorHealthPercent;
        TargetHealthPercent = targetHealthPercent;
        DistanceCategory = CategorizeDistance(distance);
        ActorState = actorState;
        WasSuccessful = wasSuccessful;
    }
    
    private static string CategorizeDistance(float dist)
    {
        if (dist < 3f) return "Close";
        if (dist < 10f) return "Medium";
        return "Far";
    }

    public bool Equals(ActionRecord other)
    {
        if (other == null) return false;
        return Timestamp  == other.Timestamp
            && Actor      == other.Actor
            && ActionType == other.ActionType
            && Target     == other.Target
            && IsHit      == other.IsHit
            && Damage     == other.Damage
            && Distance   == other.Distance;
    }

    public override bool Equals(object obj) => Equals(obj as ActionRecord);

    public override int GetHashCode()
    {
        return (Timestamp, Actor, ActionType, Target, IsHit, Damage, Distance).GetHashCode();
    }
}

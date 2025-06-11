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
    [Tooltip("How often (seconds) to flush the log to disk")]
    public float saveInterval = 30f;

    private List<ActionRecord> actions = new List<ActionRecord>();
    private string filePath;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);

            filePath = Path.Combine(Application.persistentDataPath, "ActionsLog.json");
            Debug.Log($"[ActionLogger] Logging to: {filePath}");

            StartCoroutine(AutoSaveCoroutine());
        }
        else
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// Call this from anywhere to record an action.
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
        actions.Add(record);
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
    /// </summary>
    private void SaveAndClear()
    {
        if (actions.Count == 0)
        {
            Debug.Log("[ActionLogger] No actions to save.");
            return;
        }

        // Dedupe & sort the newly logged actions
        var newBatch = actions
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
            Debug.Log($"[ActionLogger] Saved {newBatch.Count} new actions (total {all.Count}) to {filePath}");
        }
        catch (IOException e)
        {
            Debug.LogError($"[ActionLogger] Save failed: {e.Message}");
        }

        actions.Clear();
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
    public float Timestamp;
    public string Actor;
    public string ActionType;
    public string Target;
    public bool IsHit;
    public float Damage;
    public float Distance;

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

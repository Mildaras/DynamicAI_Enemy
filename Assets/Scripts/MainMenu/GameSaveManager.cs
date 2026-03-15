using System;
using System.IO;
using UnityEngine;

[Serializable]
public class GameSaveData
{
    public int day;
    public float gold;
    public float health;
    public float shield;

    // abilities
    public bool hasExtraJump;
    public bool hasInvunerability;
    public bool hasBlink;
    public bool hasStunPulse;
    public bool hasReflect;
    public bool hasSmite;
}

/// <summary>
/// Static helper for saving / loading game progress to a JSON file.
/// No MonoBehaviour needed — call directly from anywhere.
/// </summary>
public static class GameSaveManager
{
    private static string SavePath =>
        Path.Combine(Application.persistentDataPath, "savegame.json");

    /// <summary>
    /// Snapshot current game state and write to disk.
    /// </summary>
    public static void SaveGame()
    {
        var data = new GameSaveData
        {
            day              = PlayerPrefs.GetInt("GameDay", 1),
            gold             = PlayerData.playerGold,
            health           = PlayerData.playerHealth,
            shield           = PlayerData.playerShield,
            hasExtraJump     = PlayerData.hasExtraJump,
            hasInvunerability = PlayerData.hasInvunerability,
            hasBlink         = PlayerData.hasBlink,
            hasStunPulse     = PlayerData.hasStunPulse,
            hasReflect       = PlayerData.hasReflect,
            hasSmite         = PlayerData.hasSmite
        };

        string json = JsonUtility.ToJson(data, true);
        File.WriteAllText(SavePath, json);
        Debug.Log($"[SaveManager] Game saved to {SavePath}");
    }

    /// <summary>
    /// Load saved data and push it back into PlayerData statics / PlayerPrefs.
    /// </summary>
    public static void LoadGame()
    {
        if (!HasSaveFile())
        {
            Debug.LogWarning("[SaveManager] No save file found.");
            return;
        }

        string json = File.ReadAllText(SavePath);
        var data = JsonUtility.FromJson<GameSaveData>(json);

        PlayerPrefs.SetInt("GameDay", data.day);

        PlayerData.RestoreState(data.health, data.shield, data.gold);

        PlayerData.hasExtraJump     = data.hasExtraJump;
        PlayerData.hasInvunerability = data.hasInvunerability;
        PlayerData.hasBlink         = data.hasBlink;
        PlayerData.hasStunPulse     = data.hasStunPulse;
        PlayerData.hasReflect       = data.hasReflect;
        PlayerData.hasSmite         = data.hasSmite;

        Debug.Log($"[SaveManager] Game loaded — Day {data.day}, Gold {data.gold}");
    }

    public static bool HasSaveFile() => File.Exists(SavePath);

    public static void DeleteSave()
    {
        if (File.Exists(SavePath))
        {
            File.Delete(SavePath);
            Debug.Log("[SaveManager] Save file deleted.");
        }
    }
}

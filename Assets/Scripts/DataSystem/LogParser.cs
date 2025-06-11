using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

public class LogParser
{
    // Path to the JSON log file
    private string _logPath => Path.Combine(Application.persistentDataPath, "ActionsLog.json");

    /// <summary>
    /// Detailed per-(ActionType, Actor) stats.
    /// </summary>
    public class DetailedStats
    {
        public string ActionType;   // e.g. "Exploder", "Spell", etc.
        public string ActorName;    // "Player" or "Enemy"
        public int    HitCount;      // number of logged hits or casts
        public float  MedianDamage;
        public float  MedianDistance;
    }

    /// <summary>
    /// Aggregate summary of player and enemy actions.
    /// </summary>
    public struct Summary
    {
        // Player stats
        public int PlayerSwings;
        public int PlayerSwingHits;
        public float PlayerSwingAccuracy;

        public int PlayerProjectiles;
        public int PlayerProjectileHits;
        public float PlayerProjectileAccuracy;

        public int PlayerAbilityUses;

        // Enemy stats
        public int EnemyShotSpells;
        public int EnemySpellHits;
        public int EnemySpellMisses;

        // Spawn counts for summons
        public int EnemySpawnExploder;
        public int EnemySpawnBouncer;
        public int EnemySpawnTank;
    }

    [Serializable]
    private class ActionRecordList
    {
        public List<JsonActionRecord> actions = new List<JsonActionRecord>();
    }

    [Serializable]
    private class JsonActionRecord
    {
        public float   Timestamp;
        public string  Actor;
        public string  ActionType;
        public string  Target;
        public bool    IsHit;
        public float   Damage;
        public float   Distance;
    }

    /// <summary>
    /// Parses the JSON log and computes both summary and detailed stats.
    /// </summary>
    public (Summary summary, List<DetailedStats> detailed) ParseAll()
    {
        var summary = new Summary();
        // For detailed grouping: key = (actionType, actor)
        var groups = new Dictionary<(string actionType, string actor), List<(float dmg, float dist)>>();

        if (!File.Exists(_logPath))
        {
            Debug.LogWarning($"LogParser: No log file found at {_logPath}");
            return (summary, new List<DetailedStats>());
        }

        ActionRecordList wrapper;
        try
        {
            var json = File.ReadAllText(_logPath);
            wrapper = JsonUtility.FromJson<ActionRecordList>(json) ?? new ActionRecordList();
        }
        catch (Exception e)
        {
            Debug.LogError($"LogParser: Failed to read or parse JSON at {_logPath}: {e.Message}");
            return (summary, new List<DetailedStats>());
        }

        foreach (var record in wrapper.actions)
        {
            string actor      = record.Actor;
            string actionType = record.ActionType;
            bool   isHit      = record.IsHit;
            float  dmgRaw     = record.Damage;
            float  distRaw    = record.Distance;
            float  distNorm   = (distRaw <= 0f) ? 0f : distRaw;

            // 1) Summary counts
            if (actor.Equals("Player", StringComparison.OrdinalIgnoreCase))
            {
                switch (actionType)
                {
                    case "Swing":
                        summary.PlayerSwings++;
                        if (isHit) summary.PlayerSwingHits++;
                        break;

                    case "Spell":
                    case "Wand":
                    case "Projectile":
                        summary.PlayerProjectiles++;
                        if (isHit) summary.PlayerProjectileHits++;
                        break;

                    case "UseAbility":
                        summary.PlayerAbilityUses++;
                        break;
                }
            }
            else if (actor.Equals("Enemy", StringComparison.OrdinalIgnoreCase))
            {
                switch (actionType)
                {
                    case "ShotSpell":
                        summary.EnemyShotSpells++;
                        break;

                    case "Spell":
                        if (isHit) summary.EnemySpellHits++;
                        break;

                    case "SpawnExploders":
                        summary.EnemySpawnExploder++;
                        break;

                    case "SpawnBouncers":
                        summary.EnemySpawnBouncer++;
                        break;

                    case "SpawnTank":
                        summary.EnemySpawnTank++;
                        break;
                }
            }

            // 2) Detailed grouping for damage events only
            bool recordForDetail = false;
            string detailKey = null;

            if (actor.Equals("Player", StringComparison.OrdinalIgnoreCase))
            {
                // Player dealing damage
                if (isHit && dmgRaw > 0f)
                {
                    string enemyCategory;
                    string tgt = record.Target ?? string.Empty;
                    if (tgt.IndexOf("Exploder", StringComparison.OrdinalIgnoreCase) >= 0)
                        enemyCategory = "Exploder";
                    else if (tgt.IndexOf("Bouncer", StringComparison.OrdinalIgnoreCase) >= 0)
                        enemyCategory = "Bouncer";
                    else if (tgt.IndexOf("Tank", StringComparison.OrdinalIgnoreCase) >= 0)
                        enemyCategory = "Tank";
                    else
                        enemyCategory = "Enemy";

                    detailKey = enemyCategory;
                    recordForDetail = true;
                }
            }
            else if (actor.Equals("Enemy", StringComparison.OrdinalIgnoreCase))
            {
                if ((actionType == "Spell" || actionType == "Exploder" ||
                     actionType == "Bouncer" || actionType == "Tank") && dmgRaw > 0f && isHit)
                {
                    detailKey = actionType;
                    recordForDetail = true;
                }
            }

            if (recordForDetail)
            {
                var key = (actionType: detailKey, actor: actor);
                if (!groups.TryGetValue(key, out var list))
                {
                    list = new List<(float, float)>();
                    groups[key] = list;
                }
                list.Add((dmgRaw, distNorm));
            }
        }

        // finalize accuracies and misses
        if (summary.PlayerSwings > 0)
            summary.PlayerSwingAccuracy = (float)summary.PlayerSwingHits / summary.PlayerSwings;
        if (summary.PlayerProjectiles > 0)
            summary.PlayerProjectileAccuracy = (float)summary.PlayerProjectileHits / summary.PlayerProjectiles;
        summary.EnemySpellMisses = summary.EnemyShotSpells - summary.EnemySpellHits;

        // build detailed stats
        var detailed = groups.Select(kvp =>
        {
            var (actionType, actor) = kvp.Key;
            var data = kvp.Value;
            var dmgs  = data.Select(x => x.dmg).OrderBy(x => x).ToList();
            var dists = data.Select(x => x.dist).OrderBy(x => x).ToList();

            float Median(IList<float> arr)
            {
                int n = arr.Count;
                if (n == 0) return 0f;
                return (n % 2 == 1)
                    ? arr[n / 2]
                    : (arr[n / 2 - 1] + arr[n / 2]) * 0.5f;
            }

            return new DetailedStats
            {
                ActionType     = actionType,
                ActorName      = actor,
                HitCount       = data.Count,
                MedianDamage   = Median(dmgs),
                MedianDistance = Median(dists)
            };
        }).ToList();

        return (summary, detailed);
    }
}

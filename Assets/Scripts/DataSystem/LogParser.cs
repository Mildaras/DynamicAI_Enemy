using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

public class LogParser
{
    /// <summary>
    /// Get the current action log path from ActionLogger (which may be session-specific).
    /// </summary>
    private string _logPath
    {
        get
        {
            // Use ActionLogger's current file path (may be in a session folder)
            if (ActionLogger.Instance != null)
            {
                return ActionLogger.Instance.GetLogFilePath();
            }
            // Fallback to default if ActionLogger not initialized
            return Path.Combine(Application.persistentDataPath, "ActionsLog.json");
        }
    }

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
        // Player combat actions
        public int PlayerMeleeSwings;
        public int PlayerMeleeHits;
        public float PlayerMeleeAccuracy;

        public int PlayerProjectiles;
        public int PlayerProjectileHits;
        public float PlayerProjectileAccuracy;
        
        public int PlayerTelekinesisKills;

        // Player abilities
        public int PlayerBlinkUses;
        public int PlayerBlinkSuccesses;
        public int PlayerStunUses;
        public int PlayerReflectUses;
        public int PlayerSmiteUses;

        // Enemy combat actions
        public int EnemyMeleeAttacks;
        public int EnemySpellCasts;
        public int EnemySpellHits;
        public int EnemySpellMisses;
        
        public int EnemyExplodeAttempts;
        public int EnemyExplodeHits;

        // Enemy tactical actions
        public int EnemyCoverAttempts;
        public int EnemyCoverSuccesses;
        public int EnemyHealUses;
        public int EnemyWallUses;

        // Enemy spawns
        public int EnemySpawnExploder;
        public int EnemySpawnBouncer;
        public int EnemySpawnTank;
        
        // Contextual metrics
        public float AveragePlayerHealth;     // Average player health during actions
        public float AverageEnemyHealth;      // Average enemy health during actions
        public float PlayerAggressionScore;   // 0-1, based on offensive vs defensive actions
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
        
        // Context fields (may be -1 or null for older logs)
        public float   ActorHealthPercent = -1f;
        public float   TargetHealthPercent = -1f;
        public string  DistanceCategory;
        public string  ActorState;
        public bool    WasSuccessful;
    }

    /// <summary>
    /// Parses from ring buffer (real-time, in-memory). Fastest option.
    /// </summary>
    public (Summary summary, List<DetailedStats> detailed) ParseFromMemory(List<ActionRecord> events)
    {
        var summary = new Summary();
        var groups = new Dictionary<(string actionType, string actor), List<(float dmg, float dist)>>();
        
        // Convert ActionRecord to parse logic (reuse existing logic)
        return ParseRecords(events.Select(r => new JsonActionRecord
        {
            Timestamp = r.Timestamp,
            Actor = r.Actor,
            ActionType = r.ActionType,
            Target = r.Target,
            IsHit = r.IsHit,
            Damage = r.Damage,
            Distance = r.Distance,
            ActorHealthPercent = r.ActorHealthPercent,
            TargetHealthPercent = r.TargetHealthPercent,
            DistanceCategory = r.DistanceCategory,
            ActorState = r.ActorState,
            WasSuccessful = r.WasSuccessful
        }).ToList());
    }
    
    /// <summary>
    /// Parses the JSON log file from disk (slower, for historical data).
    /// </summary>
    public (Summary summary, List<DetailedStats> detailed) ParseAll()
    {
        if (!File.Exists(_logPath))
        {
            Debug.LogWarning($"LogParser: No log file found at {_logPath}");
            return (new Summary(), new List<DetailedStats>());
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
            return (new Summary(), new List<DetailedStats>());
        }
        
        return ParseRecords(wrapper.actions);
    }
    
    /// <summary>
    /// Shared parsing logic for both file and memory sources.
    /// </summary>
    private (Summary summary, List<DetailedStats> detailed) ParseRecords(List<JsonActionRecord> records)
    {
        var summary = new Summary();
        var groups = new Dictionary<(string actionType, string actor), List<(float dmg, float dist)>>();

        // For contextual metrics
        float totalPlayerHealth = 0f;
        int playerHealthSamples = 0;
        float totalEnemyHealth = 0f;
        int enemyHealthSamples = 0;
        int offensiveActions = 0;
        int defensiveActions = 0;
        
        foreach (var record in records)
        {
            string actor      = record.Actor;
            string actionType = record.ActionType;
            bool   isHit      = record.IsHit;
            bool   success    = record.WasSuccessful;
            float  dmgRaw     = record.Damage;
            float  distRaw    = record.Distance;
            float  distNorm   = (distRaw <= 0f) ? 0f : distRaw;
            
            // Track health context
            if (record.ActorHealthPercent >= 0f)
            {
                if (actor.Equals("Player", StringComparison.OrdinalIgnoreCase))
                {
                    totalPlayerHealth += record.ActorHealthPercent;
                    playerHealthSamples++;
                }
                else if (actor.Equals("Enemy", StringComparison.OrdinalIgnoreCase))
                {
                    totalEnemyHealth += record.ActorHealthPercent;
                    enemyHealthSamples++;
                }
            }

            // 1) Player actions - pattern-based parsing
            if (actor.Equals("Player", StringComparison.OrdinalIgnoreCase))
            {
                // Player combat
                if (actionType == "Player_MeleeSwing")
                {
                    summary.PlayerMeleeSwings++;
                    if (isHit) summary.PlayerMeleeHits++;
                    offensiveActions++;
                }
                else if (actionType == "Player_Projectile" || actionType == "Player_ReflectedSpell")
                {
                    summary.PlayerProjectiles++;
                    if (isHit) summary.PlayerProjectileHits++;
                    offensiveActions++;
                }
                else if (actionType == "Player_Telekinesis")
                {
                    if (isHit) summary.PlayerTelekinesisKills++;
                    offensiveActions++;
                }
                else if (actionType == "Player_Smite")
                {
                    summary.PlayerSmiteUses++;
                    offensiveActions++;
                }
                // Player abilities
                else if (actionType == "UseAbility_Blink")
                {
                    summary.PlayerBlinkUses++;
                    if (success) summary.PlayerBlinkSuccesses++;
                    defensiveActions++;  // Blink is mobility/escape
                }
                else if (actionType == "UseAbility_Stun")
                {
                    summary.PlayerStunUses++;
                    offensiveActions++;  // Stun is offensive
                }
                else if (actionType == "UseAbility_Reflect")
                {
                    summary.PlayerReflectUses++;
                    defensiveActions++;  // Reflect is defensive
                }
            }
            // 2) Enemy actions - pattern-based parsing
            else if (actor.Equals("Enemy", StringComparison.OrdinalIgnoreCase))
            {
                // Enemy combat
                if (actionType == "Enemy_MeleeAttack" || actionType == "Enemy_BouncerAttack" || actionType == "Enemy_TankAttack")
                {
                    summary.EnemyMeleeAttacks++;
                }
                else if (actionType == "Enemy_CastSpell")
                {
                    summary.EnemySpellCasts++;
                }
                else if (actionType == "Enemy_SpellHit")
                {
                    if (isHit) summary.EnemySpellHits++;
                }
                else if (actionType == "Enemy_Explode")
                {
                    summary.EnemyExplodeAttempts++;
                    if (isHit) summary.EnemyExplodeHits++;
                }
                // Enemy tactical
                else if (actionType == "RunToCover")
                {
                    summary.EnemyCoverAttempts++;
                    if (success) summary.EnemyCoverSuccesses++;
                }
                else if (actionType == "Heal")
                {
                    summary.EnemyHealUses++;
                }
                else if (actionType == "SummonWall")
                {
                    summary.EnemyWallUses++;
                }
                // Enemy spawns
                else if (actionType == "Enemy_SpawnExploders")
                {
                    summary.EnemySpawnExploder++;
                }
                else if (actionType == "Enemy_SpawnBouncers")
                {
                    summary.EnemySpawnBouncer++;
                }
                else if (actionType == "Enemy_SpawnTank")
                {
                    summary.EnemySpawnTank++;
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

        // Finalize accuracies
        if (summary.PlayerMeleeSwings > 0)
            summary.PlayerMeleeAccuracy = (float)summary.PlayerMeleeHits / summary.PlayerMeleeSwings;
        if (summary.PlayerProjectiles > 0)
            summary.PlayerProjectileAccuracy = (float)summary.PlayerProjectileHits / summary.PlayerProjectiles;
        
        // Fix spell miss calculation: misses = casts - hits
        summary.EnemySpellMisses = summary.EnemySpellCasts - summary.EnemySpellHits;
        
        // Calculate contextual metrics
        if (playerHealthSamples > 0)
            summary.AveragePlayerHealth = totalPlayerHealth / playerHealthSamples;
        if (enemyHealthSamples > 0)
            summary.AverageEnemyHealth = totalEnemyHealth / enemyHealthSamples;
        
        // Calculate aggression score (0 = all defensive, 1 = all offensive)
        int totalTacticalActions = offensiveActions + defensiveActions;
        if (totalTacticalActions > 0)
            summary.PlayerAggressionScore = (float)offensiveActions / totalTacticalActions;

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

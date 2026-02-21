using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

public class CombatAnalytics : MonoBehaviour
{
    [Header("Adaptation Settings")]
    [Range(0f, 1f)]
    [Tooltip("How aggressively the MAIN enemy adjusts its weights based on player behavior")]
    public float adaptationRate = 0.5f;

    [Tooltip("Maximum expected median hit distance for normalization")]
    public float maxMedianDistance = 5f;

    [Header("Logging")]
    [Tooltip("Control how much info is logged to console. None=Silent, Critical=Only important, Important=Key events, Detailed=Adapters, Verbose=Everything")]
    public AdaptiveLogger.LogLevel logLevel = AdaptiveLogger.LogLevel.Important;

    private LogParser.Summary _summary;
    private List<LogParser.DetailedStats> _detailed;
    private string _summaryPath;

    // Weight adaptation strategies
    private readonly List<IWeightAdapter> _adapters = new List<IWeightAdapter>
    {
        new OffensiveWeightAdapter(),
        new DefensiveWeightAdapter(),
        new SummoningWeightAdapter()
    };

    void Start()
    {
        AdaptiveLogger.SetLevel(logLevel);
        
        _summaryPath = Path.Combine(Application.persistentDataPath, "CombatSummary.txt");
        AdaptiveLogger.Critical($"Summary file: {_summaryPath}");

        // Parse logs on startup
        var parser = new LogParser();
        (_summary, _detailed) = parser.ParseAll();

        AdaptiveLogger.Important("Logs parsed; press 'K' to run adaptation.");
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.K))
            RunAdaptation();
    }

    private void LogConsoleSummary(LogParser.Summary s)
    {
        AdaptiveLogger.Important(
            "=== Combat Summary ===\n" +
            $"  Player Melee: {s.PlayerMeleeSwings} swings, {s.PlayerMeleeHits} hits ({s.PlayerMeleeAccuracy:P1})\n" +
            $"  Player Ranged: {s.PlayerProjectiles} fired, {s.PlayerProjectileHits} hits ({s.PlayerProjectileAccuracy:P1})\n" +
            $"  Abilities: Blink={s.PlayerBlinkUses}, Stun={s.PlayerStunUses}, Reflect={s.PlayerReflectUses}, Smite={s.PlayerSmiteUses}\n" +
            $"  Enemy Spells: {s.EnemySpellCasts} cast, {s.EnemySpellHits} hits, {s.EnemySpellMisses} misses\n" +
            $"  Summons: Exploder={s.EnemySpawnExploder}, Bouncer={s.EnemySpawnBouncer}, Tank={s.EnemySpawnTank}\n" +
            $"  Avg Health: Player={s.AveragePlayerHealth:P0}, Enemy={s.AverageEnemyHealth:P0}, Aggression={s.PlayerAggressionScore:F2}"
        );
    }

    private void AppendSummaryToFile(LogParser.Summary s)
    {
        var lines = new[]
        {
            $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}]",
            $"PlayerMeleeSwings={s.PlayerMeleeSwings}",
            $"PlayerMeleeHits={s.PlayerMeleeHits}",
            $"PlayerMeleeAccuracy={s.PlayerMeleeAccuracy:F3}",
            $"PlayerProjectiles={s.PlayerProjectiles}",
            $"PlayerProjectileHits={s.PlayerProjectileHits}",
            $"PlayerProjectileAccuracy={s.PlayerProjectileAccuracy:F3}",
            $"PlayerBlinkUses={s.PlayerBlinkUses}",
            $"PlayerStunUses={s.PlayerStunUses}",
            $"PlayerReflectUses={s.PlayerReflectUses}",
            $"PlayerSmiteUses={s.PlayerSmiteUses}",
            $"EnemySpellCasts={s.EnemySpellCasts}",
            $"EnemySpellHits={s.EnemySpellHits}",
            $"EnemySpellMisses={s.EnemySpellMisses}",
            $"EnemySpawnExploder={s.EnemySpawnExploder}",
            $"EnemySpawnBouncer={s.EnemySpawnBouncer}",
            $"EnemySpawnTank={s.EnemySpawnTank}",
            $"AveragePlayerHealth={s.AveragePlayerHealth:F3}",
            $"AverageEnemyHealth={s.AverageEnemyHealth:F3}",
            $"PlayerAggressionScore={s.PlayerAggressionScore:F3}",
            new string('-', 40)
        };
        File.AppendAllLines(_summaryPath, lines);
    }

    public void RunAdaptation()
    {
        // 1) Flush pending log data to disk first
        if (SessionManager.Instance != null)
        {
            SessionManager.Instance.FlushActionLog();
            AdaptiveLogger.Detailed("Flushed pending actions to disk");
        }
        
        // 2) Re-parse logs to get current session data
        var parser = new LogParser();
        (_summary, _detailed) = parser.ParseAll();
        AdaptiveLogger.Detailed("Parsed current session logs");
        
        // 3) Find the Main enemy
        var mainEnemy = FindObjectsOfType<EnemyController>()
            .FirstOrDefault(e => e.role == EnemyRole.Main);
        if (mainEnemy == null)
        {
            AdaptiveLogger.Warning("No Main-role EnemyController found.");
            return;
        }

        string keyPrefix = mainEnemy.name + "_";
        AdaptiveLogger.Important($"\n========== ADAPTATION STARTED: {mainEnemy.name} ==========");

        // 4) Build player behavior profile
        var profile = PlayerProfile.Build(_summary, _detailed, maxMedianDistance);
        AdaptiveLogger.Important($"Player Profile:\n{profile}");

        // 5) Load current weights from PlayerPrefs or enemy defaults
        var weights = new EnemyWeightProfile();
        weights.LoadFromPrefs(keyPrefix, mainEnemy);
        
        // Store original weights for comparison
        var originalWeights = new EnemyWeightProfile();
        originalWeights.LoadFrom(mainEnemy);
        AdaptiveLogger.Detailed($"BEFORE adaptation:\n{FormatWeightsDetailed(originalWeights)}");

        // 6) Run all adaptation strategies
        foreach (var adapter in _adapters)
        {
            adapter.Adapt(profile, _summary, _detailed, weights, adaptationRate);
        }

        // 7) Validate weights (clamp, NaN checks)
        weights.Validate();

        // 8) Save adapted weights to PlayerPrefs
        weights.SaveToPrefs(keyPrefix);

        // 9) Apply to enemy controller
        weights.ApplyTo(mainEnemy);
        
        // 10) Show detailed before/after comparison
        AdaptiveLogger.Detailed($"AFTER adaptation:\n{FormatWeightsDetailed(weights)}");
        AdaptiveLogger.Critical(GenerateChangeReport(originalWeights, weights));

        // 11) Display final combat summary
        LogConsoleSummary(_summary);
        
        // 12) Save to current session folder
        if (SessionManager.Instance != null)
        {
            SessionManager.Instance.SaveCombatSummary(_summary);
            SessionManager.Instance.SaveWeightChanges(GenerateChangeReport(originalWeights, weights));
        }
        
        // 13) Also append to legacy summary file for backwards compatibility
        AppendSummaryToFile(_summary);
    }

    private string FormatWeightsDetailed(EnemyWeightProfile w)
    {
        return $"  Offensive:\n" +
               $"    Fast={w.FastSpell:F2}, Med={w.MediumSpell:F2}, Slow={w.SlowSpell:F2}, Atk={w.Attack:F2}\n" +
               $"  Defensive:\n" +
               $"    CoverHealth={w.CoverForHealth:F2}, CoverAttacks={w.CoverFromAttacks:F2}\n" +
               $"    WallRanged={w.WallRangedAttack:F2}, HealHigh={w.HealHigh:F2}, HealCover={w.HealCover:F2}, WallHeal={w.WallHeal:F2}\n" +
               $"  Summoning:\n" +
               $"    Exploder={w.SpawnExploder:F2}, Bouncer={w.SpawnBouncer:F2}, Tank={w.SpawnTank:F2}";
    }

    private string GenerateChangeReport(EnemyWeightProfile before, EnemyWeightProfile after)
    {
        var changes = new List<string>();
        
        void CheckChange(string name, float beforeVal, float afterVal)
        {
            float delta = afterVal - beforeVal;
            if (Mathf.Abs(delta) > 0.01f)
            {
                float percentChange = beforeVal != 0 ? (delta / beforeVal) * 100f : 0f;
                string arrow = delta > 0 ? "↑" : "↓";
                changes.Add($"  {name}: {beforeVal:F2} → {afterVal:F2} ({arrow} {Mathf.Abs(delta):F2}, {percentChange:+0.0;-0.0}%)");
            }
        }
        
        CheckChange("FastSpell", before.FastSpell, after.FastSpell);
        CheckChange("MediumSpell", before.MediumSpell, after.MediumSpell);
        CheckChange("SlowSpell", before.SlowSpell, after.SlowSpell);
        CheckChange("Attack", before.Attack, after.Attack);
        CheckChange("CoverForHealth", before.CoverForHealth, after.CoverForHealth);
        CheckChange("CoverFromAttacks", before.CoverFromAttacks, after.CoverFromAttacks);
        CheckChange("WallRangedAttack", before.WallRangedAttack, after.WallRangedAttack);
        CheckChange("HealHigh", before.HealHigh, after.HealHigh);
        CheckChange("HealCover", before.HealCover, after.HealCover);
        CheckChange("WallHeal", before.WallHeal, after.WallHeal);
        CheckChange("SpawnExploder", before.SpawnExploder, after.SpawnExploder);
        CheckChange("SpawnBouncer", before.SpawnBouncer, after.SpawnBouncer);
        CheckChange("SpawnTank", before.SpawnTank, after.SpawnTank);
        
        if (changes.Count == 0)
        {
            return "⚠️ NO WEIGHTS CHANGED! (Adaptation rate too low or insufficient data?)";
        }
        
        return $"=== WEIGHT CHANGES ({changes.Count} modified) ===\n" + string.Join("\n", changes);
    }
}

/// <summary>
/// Extension to compute median of floats.
/// </summary>
public static class LinqExtensions
{
    public static float Median(this IEnumerable<float> src)
    {
        var arr = src.OrderBy(x => x).ToArray();
        int n = arr.Length;
        if (n == 0) return 0f;
        return (n % 2 == 1) ? arr[n / 2] : (arr[n / 2 - 1] + arr[n / 2]) * 0.5f;
    }
}
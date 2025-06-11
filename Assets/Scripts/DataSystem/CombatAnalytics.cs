using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

public class CombatAnalytics : MonoBehaviour
{
    [Header("Adaptation Settings")]
    [Range(0f, 1f)]
    [Tooltip("How aggressively the MAIN enemy adjusts its weights based on the last session’s stats")]
    public float adaptationRate = 0.5f;

    [Tooltip("Maximum expected median hit distance for normalization")]
    public float maxMedianDistance = 5f;

    private LogParser.Summary _summary;
    private List<LogParser.DetailedStats> _detailed;
    private string _summaryPath;

    void Start()
    {
        _summaryPath = Path.Combine(Application.persistentDataPath, "CombatSummary.txt");

        // 1) parse JSON log
        var parser = new LogParser();
        (_summary, _detailed) = parser.ParseAll();

        // 2) log to console and file
        LogConsoleSummary(_summary);
        AppendSummaryToFile(_summary);

        Debug.Log("[CombatAnalytics] Parsed log; press 'K' to adapt weights.");
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.K))
            RunAdaptation();
    }

    private void LogConsoleSummary(LogParser.Summary s)
    {
        Debug.Log(
            "=== Combat Summary ===\n" +
            $"- Player Swings:           {s.PlayerSwings}\n" +
            $"- Swing Hits:             {s.PlayerSwingHits}\n" +
            $"- Swing Accuracy:         {s.PlayerSwingAccuracy:P1}\n" +
            $"- Projectiles Fired:      {s.PlayerProjectiles}\n" +
            $"- Projectile Hits:        {s.PlayerProjectileHits}\n" +
            $"- Projectile Accuracy:    {s.PlayerProjectileAccuracy:P1}\n" +
            $"- Abilities Used:         {s.PlayerAbilityUses}\n" +
            $"- Enemy Spells Cast:      {s.EnemyShotSpells}\n" +
            $"- Enemy Spell Hits:       {s.EnemySpellHits}\n" +
            $"- Enemy Spell Misses:     {s.EnemySpellMisses}"+
            $"- Summons Spawned:        Exploder={s.EnemySpawnExploder}, Bouncer={s.EnemySpawnBouncer}, Tank={s.EnemySpawnTank}"
        );
    }

    private void AppendSummaryToFile(LogParser.Summary s)
    {
        var lines = new[]
        {
            $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}]",
            $"PlayerSwings={s.PlayerSwings}",
            $"PlayerSwingHits={s.PlayerSwingHits}",
            $"PlayerSwingAccuracy={s.PlayerSwingAccuracy:F3}",
            $"PlayerProjectiles={s.PlayerProjectiles}",
            $"PlayerProjectileHits={s.PlayerProjectileHits}",
            $"PlayerProjectileAccuracy={s.PlayerProjectileAccuracy:F3}",
            $"PlayerAbilityUses={s.PlayerAbilityUses}",
            $"EnemyShotSpells={s.EnemyShotSpells}",
            $"EnemySpellHits={s.EnemySpellHits}",
            $"EnemySpellMisses={s.EnemySpellMisses}",
            $"EnemySpawnExploder={s.EnemySpawnExploder}",
            $"EnemySpawnBouncer={s.EnemySpawnBouncer}",
            $"EnemySpawnTank={s.EnemySpawnTank}",
            new string('-', 40)
        };
        File.AppendAllLines(_summaryPath, lines);
    }

    public void RunAdaptation() => RunAdaptationInternal();

    private void RunAdaptationInternal()
    {
        // 1) Find the MAIN enemy
        var mainEnemy = FindObjectsOfType<EnemyController>()
            .FirstOrDefault(e => e.role == EnemyRole.Main);
        if (mainEnemy == null)
        {
            Debug.LogWarning("No Main-role EnemyController found.");
            return;
        }

        string key = mainEnemy.name + "_";
        Debug.Log($"[CombatAnalytics] Adapting weights for {mainEnemy.name}...");

        // 2) Load & clamp weights
        float wFast      = LoadClamp(key + "fastSpell",   mainEnemy.fastSpell);
        float wMed       = LoadClamp(key + "mediumSpell", mainEnemy.mediumSpell);
        float wSlow      = LoadClamp(key + "slowSpell",   mainEnemy.slowSpell);
        float wAtk       = LoadClamp(key + "atck",        mainEnemy.atck);
        float wSpawnExpl = LoadClamp(key + "spawnExpl",   mainEnemy.spawnExpl);
        float wSpawnBoun = LoadClamp(key + "spawnBoun",   mainEnemy.spawnBoun);
        float wSpawnTank = LoadClamp(key + "spawnTnk",    mainEnemy.spawnTnk);

        Debug.Log($"[CombatAnalytics] Loaded weights for {mainEnemy.name}:\n" +
                  $"fast={wFast:F1}, med={wMed:F1}, slow={wSlow:F1}, atk={wAtk:F1}\n" +
                  $"spawnExpl={wSpawnExpl:F1}, spawnBoun={wSpawnBoun:F1}, spawnTank={wSpawnTank:F1}");

        // 3) Normalize high-level stats
        float accNorm     = Mathf.Clamp01(_summary.PlayerSwingAccuracy);
        float abilityNorm = Mathf.Clamp01(_summary.PlayerAbilityUses / 10f);
        float medianDist  = _detailed
            .Where(d => d.ActorName == "Player")
            .Select(d => d.MedianDistance)
            .DefaultIfEmpty(0f)
            .Median();
        float distNorm    = Mathf.Clamp01(medianDist / maxMedianDistance);

        Debug.Log($"[CombatAnalytics] Normalized stats:\n" +
                  $"Acc={accNorm:F2}, Abilities={abilityNorm:F2}, MedDist={medianDist:F2}");

        // 4) Compute spell vs melee targets
        float baseSpell = (wFast + wMed + wSlow) / 3f;
        float tSpell    = baseSpell * (1f + abilityNorm * 0.6f + distNorm * 0.4f);
        float tAtk      = wAtk * (1f + accNorm * 0.5f);

        Debug.Log($"[CombatAnalytics] baseSpell={baseSpell:F1}, tSpell={tSpell:F1}");

        // 5) Summoner-specific stats from detailed hits
        float GetHits(string actionType) =>
            _detailed
                .Where(d => d.ActionType.Equals(actionType, StringComparison.OrdinalIgnoreCase))
                .Where(d => d.ActorName == "Enemy")
                .Select(d => (float)d.HitCount)
                .DefaultIfEmpty(0f)
                .Sum();

        float GetMedDmg(string actionType) =>
            _detailed
                .Where(d => d.ActionType.Equals(actionType, StringComparison.OrdinalIgnoreCase))
                .Where(d => d.ActorName == "Enemy")
                .Select(d => d.MedianDamage)
                .DefaultIfEmpty(0f)
                .First();

        float hitsExpl = GetHits("Exploder");
        float hitsBoun = GetHits("Bouncer");
        float hitsTank = GetHits("Tank");

        float spawnExpl = _summary.EnemySpawnExploder * 3f;
        float spawnBoun = _summary.EnemySpawnBouncer;
        float spawnTank = _summary.EnemySpawnTank;
        Debug.Log($"[CombatAnalytics] spawnsExpl={spawnExpl}, spawnsBoun={spawnBoun}, spawnsTank={spawnTank}");

        float dmgExpl  = GetMedDmg("Exploder");
        float dmgBoun  = GetMedDmg("Bouncer");
        float dmgTank  = GetMedDmg("Tank");

        Debug.Log($"[CombatAnalytics] hitsExpl={hitsExpl}, hitsBoun={hitsBoun}, hitsTank={hitsTank}\n" +
                  $"dmgExpl={dmgExpl}, dmgBoun={dmgBoun}, dmgTank={dmgTank}");

        const float epsilon     = 0.1f;
        const float adjustScale = 0.8f;
        // Exploder
        float effExpl     = (dmgExpl * hitsExpl)/(wSpawnExpl * dmgExpl);
        float tSpawnExpl  = wSpawnExpl * (1f + (1f - effExpl) * adjustScale);
        mainEnemy.spawnExpl = Mathf.Lerp(wSpawnExpl, tSpawnExpl, adaptationRate);

        // Bouncer
        float effBoun     = (dmgBoun * hitsBoun)/(wSpawnBoun * dmgBoun);
        float tSpawnBoun  = wSpawnBoun * (1f + (1f - effBoun) * adjustScale);
        mainEnemy.spawnBoun = Mathf.Lerp(wSpawnBoun, tSpawnBoun, adaptationRate);

        // Tank
        float effTank     = (dmgTank * hitsTank)/(wSpawnTank * dmgTank);
        float tSpawnTank  = wSpawnTank * (1f + (1f - effTank) * adjustScale);
        mainEnemy.spawnTnk  = Mathf.Lerp(wSpawnTank, tSpawnTank, adaptationRate);

        Debug.Log($"[CombatAnalytics] effExpl={effExpl:F2}, effBoun={effBoun:F2}, effTank={effTank:F2}\n" +
                  $"tSpawnExpl={tSpawnExpl:F1}, tSpawnBoun={tSpawnBoun:F1}, tSpawnTank={tSpawnTank:F1}");

        float maxHits = (hitsBoun + hitsExpl + hitsTank) / 3f;
        float maxDmg = (dmgBoun + dmgExpl + dmgTank) / 3f;
        
        float nHitsExpl = Mathf.Clamp01(hitsExpl / maxHits);
        float nHitsBoun = Mathf.Clamp01(hitsBoun / maxHits);
        float nHitsTank = Mathf.Clamp01(hitsTank / maxHits);
        float nDmgExpl  = Mathf.Clamp01(dmgExpl  / maxDmg);
        float nDmgBoun  = Mathf.Clamp01(dmgBoun  / maxDmg);
        float nDmgTank  = Mathf.Clamp01(dmgTank  / maxDmg);

        Debug.Log($"[CombatAnalytics] nHitsExpl={nHitsExpl:F2}, nHitsBoun={nHitsBoun:F2}, nHitsTank={nHitsTank:F2}\n" +
                  $"nDmgExpl={nDmgExpl:F2}, nDmgBoun={nDmgBoun:F2}, nDmgTank={nDmgTank:F2}");

        // float effExpl = nDmgExpl / (nHitsExpl + 0.1f);
        // float effBoun = nDmgBoun / (nHitsBoun + 0.1f);
        // float effTank = nDmgTank / (nHitsTank + 0.1f);

        // float tSpawnExpl = wSpawnExpl * (1f + (1f - effExpl) * 0.8f);
        // float tSpawnBoun = wSpawnBoun * (1f + (1f - effBoun) * 0.8f);
        // float tSpawnTank = wSpawnTank * (1f + (1f - effTank) * 0.8f);

        // // 6) Lerp & assign all
        // mainEnemy.fastSpell   = Mathf.Lerp(wFast,   tSpell,      adaptationRate);
        // mainEnemy.mediumSpell = Mathf.Lerp(wMed,    tSpell,      adaptationRate);
        // mainEnemy.slowSpell   = Mathf.Lerp(wSlow,   tSpell,      adaptationRate);
        // mainEnemy.atck        = Mathf.Lerp(wAtk,    tAtk,        adaptationRate);
        // mainEnemy.spawnExpl   = Mathf.Lerp(wSpawnExpl, tSpawnExpl, adaptationRate);
        // mainEnemy.spawnBoun   = Mathf.Lerp(wSpawnBoun, tSpawnBoun, adaptationRate);
        // mainEnemy.spawnTnk    = Mathf.Lerp(wSpawnTank, tSpawnTank, adaptationRate);

        // // 7) Persist
        // SavePref(key + "fastSpell",   mainEnemy.fastSpell);
        // SavePref(key + "mediumSpell", mainEnemy.mediumSpell);
        // SavePref(key + "slowSpell",   mainEnemy.slowSpell);
        // SavePref(key + "atck",        mainEnemy.atck);
        // SavePref(key + "spawnExpl",   mainEnemy.spawnExpl);
        // SavePref(key + "spawnBoun",   mainEnemy.spawnBoun);
        // SavePref(key + "spawnTnk",    mainEnemy.spawnTnk);
        // PlayerPrefs.Save();

        // // 8) Log AFTER adaptation
        // Debug.Log(
        //     $"[After] fast={mainEnemy.fastSpell:F1}, med={mainEnemy.mediumSpell:F1}, slow={mainEnemy.slowSpell:F1}, atk={mainEnemy.atck:F1}\n" +
        //     $"        spawnExpl={mainEnemy.spawnExpl:F1}, spawnBoun={mainEnemy.spawnBoun:F1}, spawnTank={mainEnemy.spawnTnk:F1}\n" +
        //     $"(SwingAcc={_summary.PlayerSwingAccuracy:P1}, ProjAcc={_summary.PlayerProjectileAccuracy:P1}, " +
        //     $"Abilities={_summary.PlayerAbilityUses}, MedDist={medianDist:F2}, " +
        //     $"hitsExpl={hitsExpl}, hitsBoun={hitsBoun}, hitsTank={hitsTank})"
        // );

        // 9) Append summary for record
        AppendSummaryToFile(_summary);
    }

    private float LoadClamp(string key, float fallback)
        => Mathf.Clamp(PlayerPrefs.GetFloat(key, fallback), 0f, 1000f);

    private void SavePref(string key, float val)
        => PlayerPrefs.SetFloat(key, Mathf.Clamp(val, 0f, 1000f));
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
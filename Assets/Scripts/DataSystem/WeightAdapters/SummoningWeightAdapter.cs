using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Adapts summoning weights (spawnExpl, spawnBoun, spawnTnk) based on minion effectiveness.
/// Uses ROI calculation: damage dealt by minions vs. spawn cost.
/// </summary>
public class SummoningWeightAdapter : IWeightAdapter
{
    private const float ADJUST_SCALE = 0.8f;
    private const float MIN_DIVISOR = 0.1f;

    public void Adapt(
        PlayerProfile profile, 
        LogParser.Summary summary,
        List<LogParser.DetailedStats> detailed,
        EnemyWeightProfile weights,
        float adaptationRate)
    {
        // Extract minion effectiveness from detailed stats
        float hitsExpl = GetHits(detailed, "Enemy_Explode");
        float hitsBoun = GetHits(detailed, "Enemy_BouncerAttack");
        float hitsTank = GetHits(detailed, "Enemy_TankAttack");

        float dmgExpl = GetMedianDamage(detailed, "Enemy_Explode");
        float dmgBoun = GetMedianDamage(detailed, "Enemy_BouncerAttack");
        float dmgTank = GetMedianDamage(detailed, "Enemy_TankAttack");

        AdaptiveLogger.Detailed($"[Summoning] Minions - Expl:{hitsExpl:F0} hits, Boun:{hitsBoun:F0} hits, Tank:{hitsTank:F0} hits");

        // Calculate effectiveness (ROI) for each minion type
        // Effectiveness = actual impact / expected impact based on spawn weight
        // If effectiveness < 1, minion underperformed → increase weight gradually
        // If effectiveness > 1, minion overperformed → decrease weight slightly
        // If minions didn't spawn, skip adaptation (not enough data)

        // Calculate effectiveness for all minion types
        float effExpl = CalculateEffectiveness(hitsExpl, dmgExpl, weights.SpawnExploder);
        float effBoun = CalculateEffectiveness(hitsBoun, dmgBoun, weights.SpawnBouncer);
        float effTank = CalculateEffectiveness(hitsTank, dmgTank, weights.SpawnTank);

        // Exploder adaptation (only if spawned)
        if (summary.EnemySpawnExploder > 0)
        {
            float beforeExpl = weights.SpawnExploder;
            float targetExpl = CalculateTargetWeight(weights.SpawnExploder, effExpl);
            weights.SpawnExploder = Mathf.Lerp(weights.SpawnExploder, targetExpl, adaptationRate);
            AdaptiveLogger.Detailed($"[Summoning] Exploder: {beforeExpl:F2} → target={targetExpl:F2} → lerped={weights.SpawnExploder:F2} (spawned={summary.EnemySpawnExploder}, hits={hitsExpl}, eff={effExpl:F2})");
        }

        // Bouncer adaptation (only if spawned)
        if (summary.EnemySpawnBouncer > 0)
        {
            float beforeBoun = weights.SpawnBouncer;
            float targetBoun = CalculateTargetWeight(weights.SpawnBouncer, effBoun);
            weights.SpawnBouncer = Mathf.Lerp(weights.SpawnBouncer, targetBoun, adaptationRate);
            AdaptiveLogger.Detailed($"[Summoning] Bouncer: {beforeBoun:F2} → target={targetBoun:F2} → lerped={weights.SpawnBouncer:F2} (spawned={summary.EnemySpawnBouncer}, hits={hitsBoun}, eff={effBoun:F2})");
        }

        // Tank adaptation (only if spawned)
        if (summary.EnemySpawnTank > 0)
        {
            float targetTank = CalculateTargetWeight(weights.SpawnTank, effTank);
            weights.SpawnTank = Mathf.Lerp(weights.SpawnTank, targetTank, adaptationRate);
        }

        AdaptiveLogger.Verbose($"[Summoning] Expl:{weights.SpawnExploder:F1} (eff={effExpl:F2}), Boun:{weights.SpawnBouncer:F1} (eff={effBoun:F2}), Tank:{weights.SpawnTank:F1} (eff={effTank:F2})");
    }

    /// <summary>
    /// Calculate target weight based on effectiveness.
    /// Prevents explosive growth by capping increases.
    /// </summary>
    private float CalculateTargetWeight(float currentWeight, float effectiveness)
    {
        // If effective enough (>=50% of expected), reduce weight slightly
        if (effectiveness >= 0.5f)
        {
            return currentWeight * 0.9f;
        }
        
        // If completely ineffective (0 hits), only small increase
        if (effectiveness <= 0.01f)
        {
            // Max +2 points or +20% (much more conservative)
            float smallIncrease = Mathf.Min(currentWeight * 0.2f, 2f);
            return Mathf.Min(currentWeight + smallIncrease, 100f);  // Hard cap at 100
        }
        
        // Partial effectiveness: moderate increase
        // Max increase: +30% or +5 points (whichever is smaller)
        float increase = (1f - effectiveness) * 0.3f;
        float maxModerateIncrease = Mathf.Min(currentWeight * 0.3f, 5f);
        float actualIncrease = currentWeight * increase;
        actualIncrease = Mathf.Min(actualIncrease, maxModerateIncrease);
        
        return Mathf.Min(currentWeight + actualIncrease, 100f);  // Hard cap at 100
    }

    /// <summary>
    /// Calculate effectiveness as (actual hits) / (spawn weight).
    /// Returns normalized value where 1.0 = expected performance.
    /// </summary>
    private float CalculateEffectiveness(float hits, float damage, float spawnWeight)
    {
        // Avoid division by zero
        if (spawnWeight < MIN_DIVISOR) return 1f;
        
        // Simple ROI: hits per weight point
        // Could be extended to include damage: (hits * damage) / spawnWeight
        float effectiveness = hits / spawnWeight;
        
        // Normalize to reasonable range (0-2, where 1 = expected)
        return Mathf.Clamp(effectiveness, 0f, 2f);
    }

    private float GetHits(List<LogParser.DetailedStats> detailed, string actionType)
    {
        return detailed
            .Where(d => d.ActionType.Equals(actionType, System.StringComparison.OrdinalIgnoreCase))
            .Where(d => d.ActorName == "Enemy")
            .Select(d => (float)d.HitCount)
            .DefaultIfEmpty(0f)
            .Sum();
    }

    private float GetMedianDamage(List<LogParser.DetailedStats> detailed, string actionType)
    {
        return detailed
            .Where(d => d.ActionType.Equals(actionType, System.StringComparison.OrdinalIgnoreCase))
            .Where(d => d.ActorName == "Enemy")
            .Select(d => d.MedianDamage)
            .DefaultIfEmpty(0f)
            .FirstOrDefault();
    }
}

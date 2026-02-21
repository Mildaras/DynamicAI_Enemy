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
        // If effectiveness < 1, minion underperformed → increase weight
        // If effectiveness > 1, minion overperformed → decrease weight slightly

        // Exploder adaptation
        float effExpl = CalculateEffectiveness(hitsExpl, dmgExpl, weights.SpawnExploder);
        float targetExpl = weights.SpawnExploder * (1f + (1f - effExpl) * ADJUST_SCALE);
        weights.SpawnExploder = Mathf.Lerp(weights.SpawnExploder, targetExpl, adaptationRate);

        // Bouncer adaptation
        float effBoun = CalculateEffectiveness(hitsBoun, dmgBoun, weights.SpawnBouncer);
        float targetBoun = weights.SpawnBouncer * (1f + (1f - effBoun) * ADJUST_SCALE);
        weights.SpawnBouncer = Mathf.Lerp(weights.SpawnBouncer, targetBoun, adaptationRate);

        // Tank adaptation
        float effTank = CalculateEffectiveness(hitsTank, dmgTank, weights.SpawnTank);
        float targetTank = weights.SpawnTank * (1f + (1f - effTank) * ADJUST_SCALE);
        weights.SpawnTank = Mathf.Lerp(weights.SpawnTank, targetTank, adaptationRate);

        AdaptiveLogger.Verbose($"[Summoning] Expl:{weights.SpawnExploder:F1} (eff={effExpl:F2}), Boun:{weights.SpawnBouncer:F1} (eff={effBoun:F2}), Tank:{weights.SpawnTank:F1} (eff={effTank:F2})");
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

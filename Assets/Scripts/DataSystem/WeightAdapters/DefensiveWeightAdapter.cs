using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Adapts defensive weights (cover*, wall*, heal*) based on player threat profile.
/// Logic:
/// - High ranged accuracy → increase cover/wall usage (player dangerous at range)
/// - Frequent Blink → increase heal weight (can't rely on static cover)
/// - Frequent Stun → reduce cover weight (gets interrupted in cover)
/// - Low player health average → more aggressive defense (more healing)
/// - High aggression → more wall usage (quick defense against rushdown)
/// </summary>
public class DefensiveWeightAdapter : IWeightAdapter
{
    private const float RANGED_ACCURACY_THRESHOLD = 0.5f;
    private const float LOW_HEALTH_THRESHOLD = 0.4f;
    private const float AGGRESSION_THRESHOLD = 1.5f;

    public void Adapt(
        PlayerProfile profile,
        LogParser.Summary summary,
        List<LogParser.DetailedStats> detailed,
        EnemyWeightProfile weights,
        float adaptationRate)
    {
        // Analyze player threat patterns
        bool playerGoodAtRanged = profile.RangedAccuracy > RANGED_ACCURACY_THRESHOLD;
        bool playerUsesBlinkOften = profile.UsesAbilityFrequently("blink");
        bool playerUsesStunOften = profile.UsesAbilityFrequently("stun");
        bool playerLowHealth = profile.AverageHealth < LOW_HEALTH_THRESHOLD;
        bool playerAggressive = profile.AggressionScore > AGGRESSION_THRESHOLD;

        AdaptiveLogger.Detailed($"[Defensive] Player: Ranged={profile.RangedAccuracy:P1}, Blink={playerUsesBlinkOften}, Stun={playerUsesStunOften}, Health={profile.AverageHealth:P0}");

        // Store current values
        float currentCoverHealth = weights.CoverForHealth;
        float currentCoverAttacks = weights.CoverFromAttacks;
        float currentWallRanged = weights.WallRangedAttack;
        float currentHealHigh = weights.HealHigh;
        float currentHealCover = weights.HealCover;
        float currentWallHeal = weights.WallHeal;

        // Cover weights: increase vs good ranged player, decrease vs Stun user
        float targetCoverHealth = currentCoverHealth;
        float targetCoverAttacks = currentCoverAttacks;
        
        if (playerGoodAtRanged)
        {
            targetCoverHealth *= 1.3f; // More cover vs ranged threat
            targetCoverAttacks *= 1.4f; // React to ranged attacks with cover
        }
        
        if (playerUsesStunOften)
        {
            targetCoverHealth *= 0.6f; // Cover less effective vs Stun
            targetCoverAttacks *= 0.6f;
        }

        // Wall weights: increase vs ranged player and aggressive playstyle
        float targetWallRanged = currentWallRanged;
        float targetWallHeal = currentWallHeal;
        
        if (playerGoodAtRanged)
        {
            targetWallRanged *= 1.5f; // Wall blocks ranged attacks effectively
        }
        
        if (playerAggressive)
        {
            targetWallRanged *= 1.3f; // Quick defense vs rushdown
            targetWallHeal *= 1.2f; // Use wall for breathing room to heal
        }

        // Heal weights: increase vs Blink user (mobile threat) and if player plays low health
        float targetHealHigh = currentHealHigh;
        float targetHealCover = currentHealCover;
        
        if (playerUsesBlinkOften)
        {
            targetHealHigh *= 1.4f; // Prioritize healing over cover vs mobile player
            targetHealCover *= 0.8f; // Cover less valuable
        }
        
        if (playerLowHealth)
        {
            // Player plays risky, enemy can be more aggressive with healing
            targetHealHigh *= 1.2f;
        }

        // Apply adaptations
        weights.CoverForHealth = Mathf.Lerp(currentCoverHealth, targetCoverHealth, adaptationRate);
        weights.CoverFromAttacks = Mathf.Lerp(currentCoverAttacks, targetCoverAttacks, adaptationRate);
        weights.WallRangedAttack = Mathf.Lerp(currentWallRanged, targetWallRanged, adaptationRate);
        weights.HealHigh = Mathf.Lerp(currentHealHigh, targetHealHigh, adaptationRate);
        weights.HealCover = Mathf.Lerp(currentHealCover, targetHealCover, adaptationRate);
        weights.WallHeal = Mathf.Lerp(currentWallHeal, targetWallHeal, adaptationRate);

        AdaptiveLogger.Verbose($"[Defensive] Cover:{currentCoverHealth:F1}→{weights.CoverForHealth:F1}, Wall:{currentWallRanged:F1}→{weights.WallRangedAttack:F1}, Heal:{currentHealHigh:F1}→{weights.HealHigh:F1}");
    }
}

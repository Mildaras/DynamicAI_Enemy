using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Adapts offensive weights (fastSpell, mediumSpell, slowSpell, atck) based on player combat style.
/// Logic:
/// - High melee accuracy → increase atck (player is dangerous up close)
/// - Player stays at range → increase spell weights
/// - High aggression → favor fast spells (quick response to aggressive player)
/// - Low aggression → favor slow spells (player is cautious, can set up powerful attacks)
/// </summary>
public class OffensiveWeightAdapter : IWeightAdapter
{
    private const float MELEE_ACCURACY_THRESHOLD = 0.4f;
    private const float RANGE_THRESHOLD = 0.5f;
    private const float AGGRESSION_THRESHOLD = 1.5f;

    public void Adapt(
        PlayerProfile profile,
        LogParser.Summary summary,
        List<LogParser.DetailedStats> detailed,
        EnemyWeightProfile weights,
        float adaptationRate)
    {
        // Analyze player combat preferences
        bool playerGoodAtMelee = profile.MeleeAccuracy > MELEE_ACCURACY_THRESHOLD;
        bool playerStaysAtRange = profile.CombatRange > RANGE_THRESHOLD;
        bool playerAggressive = profile.AggressionScore > AGGRESSION_THRESHOLD;

        AdaptiveLogger.Detailed($"[Offensive] Player: Melee={profile.MeleeAccuracy:P1}, Range={profile.CombatRange:F2}, Aggression={profile.AggressionScore:F2}");

        // Calculate target weights based on player behavior
        float currentAtk = weights.Attack;
        float currentFast = weights.FastSpell;
        float currentMed = weights.MediumSpell;
        float currentSlow = weights.SlowSpell;

        // Attack weight: increase if player is good at melee (become more cautious)
        // Decrease if player stays at range (melee less effective)
        float targetAtk = currentAtk;
        if (playerGoodAtMelee)
        {
            targetAtk *= 0.7f; // Reduce melee attempts against skilled melee player
        }
        if (playerStaysAtRange)
        {
            targetAtk *= 0.8f; // Further reduce if player avoids close combat
        }

        // Spell weights: increase if player stays at range
        float spellMultiplier = playerStaysAtRange ? 1.3f : 1.0f;
        
        // Fast spell: favor if player is aggressive (need quick response)
        float targetFast = currentFast * spellMultiplier;
        if (playerAggressive)
        {
            targetFast *= 1.4f; // Significantly boost fast spells vs aggressive player
        }

        // Medium spell: balanced option, slight boost at range
        float targetMed = currentMed * spellMultiplier;

        // Slow spell: favor if player is cautious (low aggression, gives time to cast)
        float targetSlow = currentSlow * spellMultiplier;
        if (!playerAggressive)
        {
            targetSlow *= 1.3f; // Boost powerful spells vs cautious player
        }

        // Apply adaptations with lerp
        weights.Attack = Mathf.Lerp(currentAtk, targetAtk, adaptationRate);
        weights.FastSpell = Mathf.Lerp(currentFast, targetFast, adaptationRate);
        weights.MediumSpell = Mathf.Lerp(currentMed, targetMed, adaptationRate);
        weights.SlowSpell = Mathf.Lerp(currentSlow, targetSlow, adaptationRate);

        AdaptiveLogger.Verbose($"[Offensive] Atk:{currentAtk:F1}→{weights.Attack:F1}, Fast:{currentFast:F1}→{weights.FastSpell:F1}, Med:{currentMed:F1}→{weights.MediumSpell:F1}, Slow:{currentSlow:F1}→{weights.SlowSpell:F1}");
    }
}

using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Analyzes and summarizes player behavior patterns from combat logs.
/// Used by weight adapters to make informed decisions.
/// </summary>
public class PlayerProfile
{
    public float MeleeAccuracy { get; private set; }
    public float RangedAccuracy { get; private set; }
    public float AggressionScore { get; private set; }
    public float AverageHealth { get; private set; }
    public float CombatRange { get; private set; }
    public int TotalAbilityUses { get; private set; }
    
    // Individual ability usage counts
    public int BlinkUses { get; private set; }
    public int StunUses { get; private set; }
    public int ReflectUses { get; private set; }
    public int SmiteUses { get; private set; }

    /// <summary>
    /// Build a player profile from parsed combat data.
    /// </summary>
    public static PlayerProfile Build(LogParser.Summary summary, List<LogParser.DetailedStats> detailed, float maxMedianDistance = 5f)
    {
        var profile = new PlayerProfile
        {
            MeleeAccuracy = Mathf.Clamp01(summary.PlayerMeleeAccuracy),
            RangedAccuracy = Mathf.Clamp01(summary.PlayerProjectileAccuracy),
            AggressionScore = summary.PlayerAggressionScore,
            AverageHealth = summary.AveragePlayerHealth,
            
            BlinkUses = summary.PlayerBlinkUses,
            StunUses = summary.PlayerStunUses,
            ReflectUses = summary.PlayerReflectUses,
            SmiteUses = summary.PlayerSmiteUses
        };

        profile.TotalAbilityUses = profile.BlinkUses + profile.StunUses + 
                                    profile.ReflectUses + profile.SmiteUses;

        // Calculate median combat distance from player actions
        float medianDist = detailed
            .Where(d => d.ActorName == "Player")
            .Select(d => d.MedianDistance)
            .DefaultIfEmpty(0f)
            .Median();

        profile.CombatRange = Mathf.Clamp01(medianDist / maxMedianDistance);

        return profile;
    }

    /// <summary>
    /// Get normalized ability usage (0-1 range, 10+ abilities = 1.0).
    /// </summary>
    public float GetNormalizedAbilityUsage()
    {
        return Mathf.Clamp01(TotalAbilityUses / 10f);
    }

    /// <summary>
    /// Check if player uses a specific ability frequently (>25% of total ability uses).
    /// </summary>
    public bool UsesAbilityFrequently(string abilityName)
    {
        if (TotalAbilityUses == 0) return false;

        int count = abilityName.ToLower() switch
        {
            "blink" => BlinkUses,
            "stun" => StunUses,
            "reflect" => ReflectUses,
            "smite" => SmiteUses,
            _ => 0
        };

        return (count / (float)TotalAbilityUses) > 0.25f;
    }

    public override string ToString()
    {
        return $"PlayerProfile:\n" +
               $"  Combat: Melee={MeleeAccuracy:P1}, Ranged={RangedAccuracy:P1}, Range={CombatRange:F2}\n" +
               $"  Behavior: Aggression={AggressionScore:F2}, Abilities={TotalAbilityUses}\n" +
               $"  Health: Avg={AverageHealth:P0}\n" +
               $"  Abilities: Blink={BlinkUses}, Stun={StunUses}, Reflect={ReflectUses}, Smite={SmiteUses}";
    }
}

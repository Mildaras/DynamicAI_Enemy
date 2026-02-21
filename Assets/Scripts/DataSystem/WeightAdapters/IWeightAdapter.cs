using System.Collections.Generic;

/// <summary>
/// Interface for weight adaptation strategies.
/// Each adapter is responsible for learning one category of weights.
/// </summary>
public interface IWeightAdapter
{
    /// <summary>
    /// Adapt weights based on player behavior and combat results.
    /// </summary>
    /// <param name="profile">Analyzed player behavior profile</param>
    /// <param name="summary">High-level combat statistics</param>
    /// <param name="detailed">Detailed per-action statistics</param>
    /// <param name="currentWeights">Current enemy weight profile</param>
    /// <param name="adaptationRate">How aggressively to adapt (0-1)</param>
    void Adapt(
        PlayerProfile profile, 
        LogParser.Summary summary,
        List<LogParser.DetailedStats> detailed,
        EnemyWeightProfile currentWeights,
        float adaptationRate
    );
}

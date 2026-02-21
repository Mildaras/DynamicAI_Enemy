using UnityEngine;

/// <summary>
/// Centralized logging for the adaptive AI system.
/// Control verbosity from Inspector without commenting code.
/// </summary>
public static class AdaptiveLogger
{
    public enum LogLevel
    {
        None = 0,        // No logs at all
        Critical = 1,    // Only errors and critical info (file paths, weight changes)
        Important = 2,   // Key events (adaptation started, player profile, summary)
        Detailed = 3,    // Adapter-level logs (each adapter's decisions)
        Verbose = 4      // Everything (including intermediate calculations)
    }

    private static LogLevel _currentLevel = LogLevel.Important;

    /// <summary>
    /// Set the logging level. Call this from Inspector-exposed field.
    /// </summary>
    public static void SetLevel(LogLevel level)
    {
        _currentLevel = level;
    }

    public static void Critical(string message)
    {
        if (_currentLevel >= LogLevel.Critical)
            Debug.Log($"<color=red>[AI-CRITICAL]</color> {message}");
    }

    public static void Important(string message)
    {
        if (_currentLevel >= LogLevel.Important)
            Debug.Log($"<color=yellow>[AI]</color> {message}");
    }

    public static void Detailed(string message)
    {
        if (_currentLevel >= LogLevel.Detailed)
            Debug.Log($"<color=cyan>[AI-Detail]</color> {message}");
    }

    public static void Verbose(string message)
    {
        if (_currentLevel >= LogLevel.Verbose)
            Debug.Log($"<color=gray>[AI-Verbose]</color> {message}");
    }

    public static void Warning(string message)
    {
        if (_currentLevel >= LogLevel.Critical)
            Debug.LogWarning($"[AI-WARNING] {message}");
    }
}

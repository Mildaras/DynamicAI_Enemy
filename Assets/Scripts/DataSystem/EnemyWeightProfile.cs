using UnityEngine;

/// <summary>
/// Value object that encapsulates all 19 enemy decision weights.
/// Handles validation, persistence, and application to EnemyController.
/// </summary>
public class EnemyWeightProfile
{
    // Offensive weights
    public float FastSpell { get; set; }
    public float MediumSpell { get; set; }
    public float SlowSpell { get; set; }
    public float Attack { get; set; }
    public float JustRun { get; set; }

    // Defensive weights
    public float CoverForHealth { get; set; }
    public float CoverFromAttacks { get; set; }
    public float CoverToWall { get; set; }
    public float TooFar { get; set; }
    public float WallRangedAttack { get; set; }
    public float HealHigh { get; set; }
    public float HealCover { get; set; }
    public float WallHeal { get; set; }

    // Summoning weights
    public float SpawnExploder { get; set; }
    public float SpawnBouncer { get; set; }
    public float SpawnTank { get; set; }
    public float DefendTank { get; set; }

    // Distraction weights
    public float DistractionFast { get; set; }
    public float DistractionMedium { get; set; }
    public float DistractionSlow { get; set; }

    private const float MIN_WEIGHT = 0f;
    private const float MAX_WEIGHT = 1000f;

    /// <summary>
    /// Load weights from an EnemyController instance.
    /// </summary>
    public void LoadFrom(EnemyController enemy)
    {
        FastSpell = enemy.fastSpell;
        MediumSpell = enemy.mediumSpell;
        SlowSpell = enemy.slowSpell;
        Attack = enemy.atck;
        JustRun = enemy.justRun;

        CoverForHealth = enemy.coverForHealth;
        CoverFromAttacks = enemy.coverFromAttacks;
        CoverToWall = enemy.coverToWall;
        TooFar = enemy.tooFar;
        WallRangedAttack = enemy.wallRangedAttack;
        HealHigh = enemy.healHigh;
        HealCover = enemy.healCover;
        WallHeal = enemy.wallHeal;

        SpawnExploder = enemy.spawnExpl;
        SpawnBouncer = enemy.spawnBoun;
        SpawnTank = enemy.spawnTnk;
        DefendTank = enemy.defendTank;

        DistractionFast = enemy.distractionFast;
        DistractionMedium = enemy.distractionMedium;
        DistractionSlow = enemy.distractionSlow;
    }

    /// <summary>
    /// Apply weights to an EnemyController instance.
    /// </summary>
    public void ApplyTo(EnemyController enemy)
    {
        enemy.fastSpell = FastSpell;
        enemy.mediumSpell = MediumSpell;
        enemy.slowSpell = SlowSpell;
        enemy.atck = Attack;
        enemy.justRun = JustRun;

        enemy.coverForHealth = CoverForHealth;
        enemy.coverFromAttacks = CoverFromAttacks;
        enemy.coverToWall = CoverToWall;
        enemy.tooFar = TooFar;
        enemy.wallRangedAttack = WallRangedAttack;
        enemy.healHigh = HealHigh;
        enemy.healCover = HealCover;
        enemy.wallHeal = WallHeal;

        enemy.spawnExpl = SpawnExploder;
        enemy.spawnBoun = SpawnBouncer;
        enemy.spawnTnk = SpawnTank;
        enemy.defendTank = DefendTank;

        enemy.distractionFast = DistractionFast;
        enemy.distractionMedium = DistractionMedium;
        enemy.distractionSlow = DistractionSlow;
    }

    /// <summary>
    /// Load weights from PlayerPrefs using the specified key prefix.
    /// </summary>
    public void LoadFromPrefs(string keyPrefix, EnemyController fallbackEnemy)
    {
        FastSpell = LoadPref(keyPrefix + "fastSpell", fallbackEnemy.fastSpell);
        MediumSpell = LoadPref(keyPrefix + "mediumSpell", fallbackEnemy.mediumSpell);
        SlowSpell = LoadPref(keyPrefix + "slowSpell", fallbackEnemy.slowSpell);
        Attack = LoadPref(keyPrefix + "atck", fallbackEnemy.atck);
        JustRun = LoadPref(keyPrefix + "justRun", fallbackEnemy.justRun);

        CoverForHealth = LoadPref(keyPrefix + "coverForHealth", fallbackEnemy.coverForHealth);
        CoverFromAttacks = LoadPref(keyPrefix + "coverFromAttacks", fallbackEnemy.coverFromAttacks);
        CoverToWall = LoadPref(keyPrefix + "coverToWall", fallbackEnemy.coverToWall);
        TooFar = LoadPref(keyPrefix + "tooFar", fallbackEnemy.tooFar);
        WallRangedAttack = LoadPref(keyPrefix + "wallRangedAttack", fallbackEnemy.wallRangedAttack);
        HealHigh = LoadPref(keyPrefix + "healHigh", fallbackEnemy.healHigh);
        HealCover = LoadPref(keyPrefix + "healCover", fallbackEnemy.healCover);
        WallHeal = LoadPref(keyPrefix + "wallHeal", fallbackEnemy.wallHeal);

        SpawnExploder = LoadPref(keyPrefix + "spawnExpl", fallbackEnemy.spawnExpl);
        SpawnBouncer = LoadPref(keyPrefix + "spawnBoun", fallbackEnemy.spawnBoun);
        SpawnTank = LoadPref(keyPrefix + "spawnTnk", fallbackEnemy.spawnTnk);
        DefendTank = LoadPref(keyPrefix + "defendTank", fallbackEnemy.defendTank);

        DistractionFast = LoadPref(keyPrefix + "distractionFast", fallbackEnemy.distractionFast);
        DistractionMedium = LoadPref(keyPrefix + "distractionMedium", fallbackEnemy.distractionMedium);
        DistractionSlow = LoadPref(keyPrefix + "distractionSlow", fallbackEnemy.distractionSlow);
    }

    /// <summary>
    /// Save weights to PlayerPrefs using the specified key prefix.
    /// </summary>
    public void SaveToPrefs(string keyPrefix)
    {
        SavePref(keyPrefix + "fastSpell", FastSpell);
        SavePref(keyPrefix + "mediumSpell", MediumSpell);
        SavePref(keyPrefix + "slowSpell", SlowSpell);
        SavePref(keyPrefix + "atck", Attack);
        SavePref(keyPrefix + "justRun", JustRun);

        SavePref(keyPrefix + "coverForHealth", CoverForHealth);
        SavePref(keyPrefix + "coverFromAttacks", CoverFromAttacks);
        SavePref(keyPrefix + "coverToWall", CoverToWall);
        SavePref(keyPrefix + "tooFar", TooFar);
        SavePref(keyPrefix + "wallRangedAttack", WallRangedAttack);
        SavePref(keyPrefix + "healHigh", HealHigh);
        SavePref(keyPrefix + "healCover", HealCover);
        SavePref(keyPrefix + "wallHeal", WallHeal);

        SavePref(keyPrefix + "spawnExpl", SpawnExploder);
        SavePref(keyPrefix + "spawnBoun", SpawnBouncer);
        SavePref(keyPrefix + "spawnTnk", SpawnTank);
        SavePref(keyPrefix + "defendTank", DefendTank);

        SavePref(keyPrefix + "distractionFast", DistractionFast);
        SavePref(keyPrefix + "distractionMedium", DistractionMedium);
        SavePref(keyPrefix + "distractionSlow", DistractionSlow);

        PlayerPrefs.Save();
    }

    /// <summary>
    /// Validate and clamp all weights to safe ranges.
    /// Replaces NaN/Infinity with MIN_WEIGHT.
    /// </summary>
    public void Validate()
    {
        FastSpell = ClampWeight(FastSpell);
        MediumSpell = ClampWeight(MediumSpell);
        SlowSpell = ClampWeight(SlowSpell);
        Attack = ClampWeight(Attack);
        JustRun = ClampWeight(JustRun);

        CoverForHealth = ClampWeight(CoverForHealth);
        CoverFromAttacks = ClampWeight(CoverFromAttacks);
        CoverToWall = ClampWeight(CoverToWall);
        TooFar = ClampWeight(TooFar);
        WallRangedAttack = ClampWeight(WallRangedAttack);
        HealHigh = ClampWeight(HealHigh);
        HealCover = ClampWeight(HealCover);
        WallHeal = ClampWeight(WallHeal);

        SpawnExploder = ClampWeight(SpawnExploder);
        SpawnBouncer = ClampWeight(SpawnBouncer);
        SpawnTank = ClampWeight(SpawnTank);
        DefendTank = ClampWeight(DefendTank);

        DistractionFast = ClampWeight(DistractionFast);
        DistractionMedium = ClampWeight(DistractionMedium);
        DistractionSlow = ClampWeight(DistractionSlow);
    }

    private float ClampWeight(float weight)
    {
        if (float.IsNaN(weight) || float.IsInfinity(weight))
        {
            Debug.LogWarning($"[EnemyWeightProfile] Invalid weight detected: {weight}, resetting to {MIN_WEIGHT}");
            return MIN_WEIGHT;
        }
        return Mathf.Clamp(weight, MIN_WEIGHT, MAX_WEIGHT);
    }

    private float LoadPref(string key, float fallback)
    {
        return Mathf.Clamp(PlayerPrefs.GetFloat(key, fallback), MIN_WEIGHT, MAX_WEIGHT);
    }

    private void SavePref(string key, float value)
    {
        PlayerPrefs.SetFloat(key, Mathf.Clamp(value, MIN_WEIGHT, MAX_WEIGHT));
    }

    public override string ToString()
    {
        return $"EnemyWeightProfile:\n" +
               $"  Offensive: fast={FastSpell:F1}, med={MediumSpell:F1}, slow={SlowSpell:F1}, atk={Attack:F1}\n" +
               $"  Defensive: cover={CoverForHealth:F1}, wall={WallRangedAttack:F1}, heal={HealHigh:F1}\n" +
               $"  Summoning: expl={SpawnExploder:F1}, boun={SpawnBouncer:F1}, tank={SpawnTank:F1}";
    }
}

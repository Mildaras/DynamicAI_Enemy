using UnityEngine;
using System;

public enum EnemyRole
{
    Main,
    Exploder,
    Bouncer,
    Tank
}
public class EnemyController : MonoBehaviour
{
    #region Constants
    // Distance thresholds
    private const float MELEE_RANGE = 2f;
    private const float SPELL_MIN_RANGE = 3f;
    private const float CLOSE_RANGE = 5f;
    private const float EXPLODER_SPAWN_RANGE = 7f;
    private const float HEAL_SAFE_DISTANCE = 10f;
    private const float TANK_SPAWN_RANGE = 15f;
    private const float COVER_TOO_FAR = 20f;
    
    // Health thresholds
    private const float LOW_HEALTH_ABSOLUTE = 300f;
    private const float HEALTH_PERCENT_HIGH = 0.8f;
    private const float HEALTH_PERCENT_MEDIUM = 0.7f;
    
    // Weight validation
    private const float MIN_WEIGHT = 0f;
    private const float MAX_WEIGHT = 1000f;
    private const float DEFAULT_RETURN_WEIGHT = 100f;
    #endregion
    
    [Header("Role Settings")]
    public EnemyRole role;
    private EnemyRefrences enemyRefrences;
    private StateMachine  stateMachine;
    private WeightedTransitionManager  decider;

    [Header("Ability Settings")]
    private bool  _isStunned;
    private float _stunEndTime;
    [SerializeField] private Animator _animator;        // assign in inspector or GetComponent in Awake

    [Header("Weights")]
    public float fastSpell = 3f;                        //Weight of easy spell
    public float mediumSpell = 2f;                      //Weight of medium spell
    public float slowSpell = 1f;                        //Weight of slow spell
    public float atck = 70f;                            //Weight of attacking player with melee
    public float justRun = 60f;                         //Weight of just running doing nothing
    public float coverForHealth = 10f;                  //Weight looking for cover when low on health
    public float coverFromAttacks = 5f;                 //Weight looking for cover when attacked
    public float coverToWall = 30f;                     //Weight of placing wall when cant find cover
    public float tooFar = 50f;                          //Weight of giving up on cover when too far from player
    public float wallRangedAttack = 5f;                 //Weight of using wall when attacked with Ranged attack   
    public float healHigh = 5f;                         //Weight of healing when being below 80% health
    public float healCover = 30f;                       //Weight of healing when being in cover 
    public float wallHeal = 50f;                        //Weight of healing after using wall(low health)
    public float spawnExpl = 5f;                        //Weight of spawning exploders when player is near
    public float spawnBoun = 5f;                        //Weight of spawning bouncers when not full health
    public float defendTank = 0f;                       //Weight of spawning tank as a defense from attacks
    public float spawnTnk = 0f;                         //Weight of spawning tank passively
    public float distractionFast = 0f;                  //Weight of enemy distraction when casting a fast spell
    public float distractionMedium = 0f;                //Weight of enemy distraction when casting a medium spell
    public float distractionSlow = 0f;                  //Weight of enemy distraction when casting a slow spell


    private bool playerFiredStaff = false;
    
    #region Helper Methods
    /// <summary>
    /// Check if player is visible and within specified distance.
    /// </summary>
    private bool CanSeePlayer(float maxDistance)
    {
        return enemyRefrences?.transform != null && 
               enemyRefrences?.player != null &&
               Vector3.Distance(enemyRefrences.transform.position, 
                               enemyRefrences.player.position) <= maxDistance;
    }
    
    /// <summary>
    /// Check if player is within min/max distance range.
    /// </summary>
    private bool IsPlayerInRange(float minDistance, float maxDistance)
    {
        if (enemyRefrences?.transform == null || enemyRefrences?.player == null)
            return false;
            
        float dist = Vector3.Distance(enemyRefrences.transform.position, 
                                      enemyRefrences.player.position);
        return dist >= minDistance && dist <= maxDistance;
    }
    
    /// <summary>
    /// Get distance to player (returns float.MaxValue if invalid).
    /// </summary>
    private float GetPlayerDistance()
    {
        if (enemyRefrences?.transform == null || enemyRefrences?.player == null)
            return float.MaxValue;
            
        return Vector3.Distance(enemyRefrences.transform.position, 
                               enemyRefrences.player.position);
    }
    
    /// <summary>
    /// Check if enemy health is below threshold.
    /// </summary>
    private bool IsHealthBelow(Enemy enemy, float threshold)
    {
        return enemy != null && enemy.CurrentHealth < threshold;
    }
    
    /// <summary>
    /// Check if enemy health is below percentage threshold.
    /// </summary>
    private bool IsHealthBelowPercent(Enemy enemy, float percent)
    {
        return enemy != null && enemy.CurrentHealth <= (enemy.maxHealth * percent);
    }
    
    /// <summary>
    /// Validate and clamp weight value.
    /// </summary>
    private float ClampWeight(float weight, string weightName)
    {
        if (float.IsNaN(weight) || float.IsInfinity(weight))
        {
            Debug.LogWarning($"[EnemyController] Invalid weight '{weightName}': {weight}, resetting to 0");
            return 0f;
        }
        
        if (weight < MIN_WEIGHT || weight > MAX_WEIGHT)
        {
            Debug.LogWarning($"[EnemyController] Weight '{weightName}' out of bounds: {weight}, clamping to [{MIN_WEIGHT}, {MAX_WEIGHT}]");
            return Mathf.Clamp(weight, MIN_WEIGHT, MAX_WEIGHT);
        }
        
        return weight;
    }
    #endregion

    private void Awake()
    {
        if (_animator == null)
            _animator = GetComponent<Animator>();
            
        ValidateWeights();
    }
    
    /// <summary>
    /// Validate all weight values on startup.
    /// </summary>
    private void ValidateWeights()
    {
        fastSpell = ClampWeight(fastSpell, nameof(fastSpell));
        mediumSpell = ClampWeight(mediumSpell, nameof(mediumSpell));
        slowSpell = ClampWeight(slowSpell, nameof(slowSpell));
        atck = ClampWeight(atck, nameof(atck));
        justRun = ClampWeight(justRun, nameof(justRun));
        coverForHealth = ClampWeight(coverForHealth, nameof(coverForHealth));
        coverFromAttacks = ClampWeight(coverFromAttacks, nameof(coverFromAttacks));
        coverToWall = ClampWeight(coverToWall, nameof(coverToWall));
        tooFar = ClampWeight(tooFar, nameof(tooFar));
        wallRangedAttack = ClampWeight(wallRangedAttack, nameof(wallRangedAttack));
        healHigh = ClampWeight(healHigh, nameof(healHigh));
        healCover = ClampWeight(healCover, nameof(healCover));
        wallHeal = ClampWeight(wallHeal, nameof(wallHeal));
        spawnExpl = ClampWeight(spawnExpl, nameof(spawnExpl));
        spawnBoun = ClampWeight(spawnBoun, nameof(spawnBoun));
        defendTank = ClampWeight(defendTank, nameof(defendTank));
        spawnTnk = ClampWeight(spawnTnk, nameof(spawnTnk));
        distractionFast = ClampWeight(distractionFast, nameof(distractionFast));
        distractionMedium = ClampWeight(distractionMedium, nameof(distractionMedium));
        distractionSlow = ClampWeight(distractionSlow, nameof(distractionSlow));
    }

    /// <summary>
    /// Debug method to log all current weight values (for testing/debugging).
    /// </summary>
    [ContextMenu("Log All Weights")]
    public void LogAllWeights()
    {
        Debug.Log($"[{name}] Weight Values:\n" +
                  $"Offensive: Fast={fastSpell}, Med={mediumSpell}, Slow={slowSpell}, Atk={atck}\n" +
                  $"Defensive: CoverHealth={coverForHealth}, CoverAtk={coverFromAttacks}, CoverWall={coverToWall}, TooFar={tooFar}\n" +
                  $"           WallRanged={wallRangedAttack}, HealHigh={healHigh}, HealCover={healCover}, WallHeal={wallHeal}\n" +
                  $"Summoning: Expl={spawnExpl}, Boun={spawnBoun}, Tank={spawnTnk}, DefTank={defendTank}\n" +
                  $"Distraction: Fast={distractionFast}, Med={distractionMedium}, Slow={distractionSlow}\n" +
                  $"Movement: JustRun={justRun}");
    }

    void Start()
    {
        // Initialize core components
        enemyRefrences = GetComponent<EnemyRefrences>();
        stateMachine = GetComponent<StateMachine>() ?? gameObject.AddComponent<StateMachine>();
        decider = gameObject.AddComponent<WeightedTransitionManager>();
        
        var follow = new FollowPlayer(enemyRefrences);
        decider.SetDefaultTarget(follow);

        // Setup behavior based on role
        switch (role)
        {
            case EnemyRole.Main:
                SetupMainEnemyBehavior(follow);
                break;
            case EnemyRole.Exploder:
                SetupExploderBehavior();
                break;
            case EnemyRole.Bouncer:
                SetupBouncerBehavior();
                break;
            case EnemyRole.Tank:
                SetupTankBehavior();
                break;
        }
    }
    
    #region Enemy Setup Methods
    /// <summary>
    /// Setup Main enemy boss behavior with all states and weighted transitions.
    /// </summary>
    private void SetupMainEnemyBehavior(FollowPlayer follow)
    {
        Enemy enemy = enemyRefrences.GetComponent<Enemy>();

                var cover  = new RunToCover(enemyRefrences);
                var heal = new Heal(enemyRefrences);
                var attack = new Attack(enemyRefrences);
                var spawnExploders = new SpawnExploders(enemyRefrences);
                var spawnBouncers = new SpawnBouncers(enemyRefrences);
                var spawnTank = new SpawnTank(enemyRefrences);
                var castSlow = new CastSpell(enemyRefrences, SpellType.Slow);
                var castMed = new CastSpell(enemyRefrences, SpellType.Medium);
                var castFast = new CastSpell(enemyRefrences, SpellType.Fast);
                var wall = new SummonWall(enemyRefrences);

                stateMachine.AddTransition(follow, spawnTank, () => 
                    Input.GetKeyDown(KeyCode.T)
                );

                stateMachine.AddTransition(follow, wall, () => 
                    Input.GetKeyDown(KeyCode.Y)
                );

                stateMachine.AddTransition(spawnTank, follow, () => 
                    spawnTank.HasSpawned
                );

                stateMachine.AddTransition(wall, follow, () => 
                    wall.IsDone
                );

                //Offensive rules
                decider.AddRule(new WeightedTransition(
                    follow, attack,
                    () => enemyRefrences.transform != null && enemyRefrences.player != null &&
                          Vector3.Distance(enemyRefrences.transform.position, enemyRefrences.player.position) <= 2f
                            ? atck 
                            : 0f
                ));

                decider.AddRule(new WeightedTransition(
                    follow, castFast,
                    () => enemyRefrences.transform != null && enemyRefrences.player != null &&
                          Vector3.Distance(enemyRefrences.transform.position, enemyRefrences.player.position) >= 3f
                            ? fastSpell 
                            : 1f
                ));

                decider.AddRule(new WeightedTransition(
                    follow, castMed,
                    () => enemyRefrences.transform != null && enemyRefrences.player != null &&
                          Vector3.Distance(enemyRefrences.transform.position, enemyRefrences.player.position) >= 3f
                            ? mediumSpell 
                            : 1f
                ));

                decider.AddRule(new WeightedTransition(
                    follow, castSlow,
                    () => enemyRefrences.transform != null && enemyRefrences.player != null &&
                          Vector3.Distance(enemyRefrences.transform.position, enemyRefrences.player.position) >= 3f
                            ? slowSpell 
                            : 0f
                ));
                
                //Defensive rules
                decider.AddRule(new WeightedTransition(
                    follow, cover,
                    () => enemyRefrences.transform != null && enemyRefrences.player != null &&
                          Vector3.Distance(enemyRefrences.transform.position, enemyRefrences.player.position) < 5f
                                            && enemy.CurrentHealth < 300f
                            ? coverForHealth 
                            : 0f
                ));

                decider.AddRule(new WeightedTransition(
                    follow, cover,
                    () => playerFiredStaff
                            ? coverFromAttacks 
                            : 0f
                ));

                
                decider.AddRule(new WeightedTransition(
                    follow, wall,
                    () => enemyRefrences.transform != null && enemyRefrences.player != null &&
                          Vector3.Distance(enemyRefrences.transform.position, enemyRefrences.player.position) > 4f && playerFiredStaff
                            ? wallRangedAttack 
                            : 0f
                ));

                decider.AddRule(new WeightedTransition(
                    follow, heal,
                    () => enemyRefrences.transform != null && enemyRefrences.player != null &&
                          enemy.CurrentHealth <= (enemy.maxHealth * 0.8f) && 
                        Vector3.Distance(enemyRefrences.transform.position, enemyRefrences.player.position) > 10f
                            ? healHigh 
                            : 0f
                ));

                decider.AddRule(new WeightedTransition(
                    cover, heal,
                    () => cover.IsHidden && enemy.CurrentHealth < 300f
                            ? healCover 
                            : 0f
                ));

                decider.AddRule(new WeightedTransition(
                    cover, wall,
                    () => enemyRefrences.transform != null && enemyRefrences.player != null &&
                          !cover.IsHidden && enemy.CurrentHealth < 300f && 
                        Vector3.Distance(enemyRefrences.transform.position, enemyRefrences.player.position) <= 15f
                        && Vector3.Distance(enemyRefrences.transform.position, enemyRefrences.player.position) >= 4f

                            ? coverToWall 
                            : 0f
                ));

                decider.AddRule(new WeightedTransition(
                    wall, heal,
                    () => wall.IsDone && enemy.CurrentHealth < 300f
                            ? wallHeal 
                            : 0f
                ));

                decider.AddRule(new WeightedTransition(
                    follow, spawnExploders,
                    () => enemyRefrences.transform != null && enemyRefrences.player != null &&
                          Vector3.Distance(enemyRefrences.transform.position, enemyRefrences.player.position) <= 7f
                            ? spawnExpl
                            : 0f
                ));

                decider.AddRule(new WeightedTransition(
                    follow, spawnBouncers,
                    () => enemy.CurrentHealth < (enemy.maxHealth * 0.7f)
                            ? spawnBoun 
                            : 0f
                ));

                decider.AddRule(new WeightedTransition(
                    follow, spawnTank,
                    () => enemyRefrences.transform != null && enemyRefrences.player != null &&
                          Vector3.Distance(enemyRefrences.transform.position, enemyRefrences.player.position) <= 15f
                    && Vector3.Distance(enemyRefrences.transform.position, enemyRefrences.player.position) > 2f
                    && playerFiredStaff
                            ? defendTank 
                            : 0f
                ));

                decider.AddRule(new WeightedTransition(
                    follow, spawnTank,
                    () => enemyRefrences.transform != null && enemyRefrences.player != null &&
                          Vector3.Distance(enemyRefrences.transform.position, enemyRefrences.player.position) <= 15f
                    && Vector3.Distance(enemyRefrences.transform.position, enemyRefrences.player.position) > 2f
                            ? spawnTnk 
                            : 0f
                ));

                decider.AddRule(new WeightedTransition(
                    spawnTank, castFast,
                    () => spawnTank.HasSpawned
                            ? distractionFast 
                            : 0f
                ));

                decider.AddRule(new WeightedTransition(
                    spawnTank, castMed,
                    () => spawnTank.HasSpawned
                            ? distractionMedium 
                            : 0f
                ));

                decider.AddRule(new WeightedTransition(
                    spawnTank, castSlow,
                    () => spawnTank.HasSpawned
                            ? distractionSlow
                            : 0f
                ));

                decider.AddRule(new WeightedTransition(
                    spawnExploders, castFast,
                    () => spawnExploders.HasSpawned
                            ? distractionFast 
                            : 0f
                ));

                decider.AddRule(new WeightedTransition(
                    spawnExploders, castMed,
                    () => spawnExploders.HasSpawned
                            ? distractionMedium 
                            : 0f
                ));

                decider.AddRule(new WeightedTransition(
                    spawnExploders, castSlow,
                    () => spawnExploders.HasSpawned
                            ? distractionSlow 
                            : 0f
                ));

                decider.AddRule(new WeightedTransition(
                    spawnBouncers, castFast,
                    () => spawnBouncers.HasSpawned
                            ? distractionFast 
                            : 0f
                ));

                decider.AddRule(new WeightedTransition(
                    spawnBouncers, castMed,
                    () => spawnBouncers.HasSpawned
                            ? distractionMedium 
                            : 0f
                ));

                decider.AddRule(new WeightedTransition(
                    spawnBouncers, castSlow,
                    () => spawnBouncers.HasSpawned
                            ? distractionSlow 
                            : 0f
                ));
                

                //Close states
                decider.AddRule(
                    follow,
                    () => enemyRefrences.transform != null && enemyRefrences.player != null &&
                          Vector3.Distance(enemyRefrences.transform.position, enemyRefrences.player.position) > 0f
                            ? justRun 
                            : 0f
                );

                decider.AddRule(
                    cover,
                    () => enemyRefrences.transform != null && enemyRefrences.player != null &&
                          !cover.IsHidden && Vector3.Distance(enemyRefrences.transform.position, enemyRefrences.player.position) > 20f
                            ? tooFar 
                            : 0f
                );

                decider.AddRule(
                    wall,
                    () => wall.IsDone 
                            ? 50f
                            : 0f
                );

                decider.AddRule(
                    heal,
                    () => heal.IsDone
                            ? 100f 
                            : 0f
                );

                decider.AddRule(
                    castFast,
                    () => castFast.IsDone
                            ? 100f 
                            : 0f
                );

                decider.AddRule(
                    castMed,
                    () => castMed.IsDone
                            ? 100f 
                            : 0f
                );

                decider.AddRule(
                    castSlow,
                    () => castSlow.IsDone
                            ? 100f 
                            : 0f
                );

                decider.AddRule(
                    attack,
                    () => attack.IsDone
                            ? 100f 
                            : 0f
                );

                decider.AddRule(
                    spawnExploders,
                    () => spawnExploders.HasSpawned
                            ? 100f 
                            : 0f
                );

                decider.AddRule(
                    spawnBouncers,
                    () => spawnBouncers.HasSpawned
                            ? 100f 
                            : 0f
                );

                decider.AddRule(
                    spawnTank,
                    () => spawnTank.HasSpawned
                            ? 100f 
                            : 0f
                );

        // Set initial state
        stateMachine.SetState(follow);
    }
    
    /// <summary>
    /// Setup Exploder enemy behavior.
    /// </summary>
    private void SetupExploderBehavior()
    {
        var explode = new ExplodeState(enemyRefrences);
        stateMachine.SetState(explode);
    }
    
    /// <summary>
    /// Setup Bouncer enemy behavior with guard and attack states.
    /// </summary>
    private void SetupBouncerBehavior()
    {
                var followMaster = new FollowMasterState(enemyRefrences);
                var guard        = new GuardState(enemyRefrences);
                var attackBouncer= new BouncerAttackState(enemyRefrences);
                var dieBouncer   = new DieState(enemyRefrences);

                float guardDist  = 3f;              // how close before guarding
                float attackDist = enemyRefrences.attackRange; // assume also 3f

                stateMachine.AddTransition(followMaster,guard,() =>
                    enemyRefrences.transform != null && enemyRefrences.master != null &&
                    Vector3.Distance(enemyRefrences.transform.position,enemyRefrences.master.position) > guardDist
                );

                stateMachine.AddTransition(guard, attackBouncer, () =>
                    enemyRefrences.transform != null && enemyRefrences.player != null &&
                    Vector3.Distance(enemyRefrences.transform.position, enemyRefrences.player.position) 
                                    <= attackDist && ((BouncerAttackState)attackBouncer).IsReady
                );

                stateMachine.AddTransition(attackBouncer, guard,() => 
                    ((BouncerAttackState)attackBouncer).HasHit
                );

                stateMachine.AddTransition(guard,followMaster,() => 
                    enemyRefrences.transform != null && enemyRefrences.master != null &&
                    Vector3.Distance(enemyRefrences.transform.position, enemyRefrences.master.position) > guardDist
                );

                stateMachine.AddTransition(guard, dieBouncer, () => 
                    guard.TimeUp
                );
        
        stateMachine.SetState(followMaster);
    }
    
    /// <summary>
    /// Setup Tank enemy behavior with idle, chase, and attack states.
    /// </summary>
    private void SetupTankBehavior()
    {
                var idleTank  = new IdleState(enemyRefrences);
                var chaseTank = new ChasePlayerState(enemyRefrences);
                var attackTank = new TankAttackState(enemyRefrences);

                float detect = enemyRefrences.detectDistance;
                // DON'T capture attackDistance - read it dynamically from enemyRefrences so it updates when scaled

                // Idle -> Chase when player detected
                stateMachine.AddTransition(
                    idleTank, chaseTank,
                    () => enemyRefrences.transform != null && enemyRefrences.player != null &&
                          Vector3.Distance(
                            enemyRefrences.transform.position,
                            enemyRefrences.player.position)
                            <= detect
                );
                // Chase -> Attack when in close range (read current attackDistance dynamically)
                stateMachine.AddTransition(
                    chaseTank, attackTank,
                    () => enemyRefrences.transform != null && enemyRefrences.player != null &&
                          Vector3.Distance(
                            enemyRefrences.transform.position,
                            enemyRefrences.player.position)
                            <= enemyRefrences.attackDistance
                );
                // Attack -> Chase if player moves out of attack range (read current attackDistance dynamically)
                stateMachine.AddTransition(
                    attackTank, chaseTank,
                    () => enemyRefrences.transform != null && enemyRefrences.player != null &&
                          Vector3.Distance(
                            enemyRefrences.transform.position,
                            enemyRefrences.player.position)
                            > enemyRefrences.attackDistance
                );

                
                // Chase -> Idle if player leaves detect range
                stateMachine.AddTransition(
                    chaseTank, idleTank,
                    () => enemyRefrences.transform != null && enemyRefrences.player != null &&
                          Vector3.Distance(
                            enemyRefrences.transform.position,
                            enemyRefrences.player.position)
                            > detect
                );

        // Start in Idle
        stateMachine.SetState(idleTank);
    }
    #endregion

    public void Stun(float duration)
    {
        _isStunned   = true;
        _stunEndTime = Time.time + duration;

        if (_animator != null)
        {
            _animator.ResetTrigger("stun");   // clear any leftover
            _animator.SetTrigger("stun");     // fire the stun transition
        }
    }

    void Update()
    {
        // Stop AI if enemy is destroyed
        if (enemyRefrences == null || enemyRefrences.transform == null)
            return;
            
        if (_isStunned)
        {
            if (Time.time >= _stunEndTime)
                _isStunned = false;
            else
                return; // skip all AI while stunned
        }

        stateMachine.Tick();
    }

    void OnEnable()
    {
        StaffController.OnStaffFired += OnPlayerFiredStaff;
    }

    void OnDisable()
    {
        StaffController.OnStaffFired -= OnPlayerFiredStaff;
    }

    private void OnPlayerFiredStaff()
    {
        playerFiredStaff = true;
        Invoke(nameof(ResetFireFlag), 3f);
    }

    private void ResetFireFlag()
    {
        playerFiredStaff = false;
    }

    private void OnDrawGizmos()
    {
        if (stateMachine != null)
        {
            Gizmos.color = stateMachine.GizmoColor();
            Gizmos.DrawSphere(transform.position + Vector3.up * 3, 0.5f);
        }
    }
}

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

    private void Awake()
    {
        if (_animator == null)
        _animator = GetComponent<Animator>();
    }

    public void LogAll()
    {
        Debug.Log("fastSpell: " + fastSpell);
        Debug.Log("mediumSpell: " + mediumSpell);
        Debug.Log("slowSpell: " + slowSpell);
        Debug.Log("atck: " + atck);
        Debug.Log("justRun: " + justRun);
        Debug.Log("coverForHealth: " + coverForHealth);
        Debug.Log("coverFromAttacks: " + coverFromAttacks);
        Debug.Log("coverToWall: " + coverToWall);
        Debug.Log("tooFar: " + tooFar);
        Debug.Log("wallRangedAttack: " + wallRangedAttack);
        Debug.Log("healHigh: " + healHigh);
        Debug.Log("healCover: " + healCover);
        Debug.Log("wallHeal: " + wallHeal);
        Debug.Log("spawnExpl: " + spawnExpl);
        Debug.Log("spawnBoun: " + spawnBoun);
        Debug.Log("defendTank: " + defendTank);
        Debug.Log("spawnTnk: " + spawnTnk);
        Debug.Log("distractionFast: " + distractionFast);
        Debug.Log("distractionMedium: " + distractionMedium);
        Debug.Log("distractionSlow: " + distractionSlow);
    }

    void Start()
    {
        enemyRefrences = GetComponent<EnemyRefrences>();
        //stateMachine   = new StateMachine();
        stateMachine = GetComponent<StateMachine>()
                    ?? gameObject.AddComponent<StateMachine>();
        Enemy enemy     = enemyRefrences.GetComponent<Enemy>();
        Transform player = enemyRefrences.player;
        Transform self   = enemyRefrences.transform;
        decider = gameObject.AddComponent<WeightedTransitionManager>();
        var follow = new FollowPlayer(enemyRefrences);
        decider.SetDefaultTarget(follow);

        switch (role)
        {
            case EnemyRole.Main:

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
                    () => Vector3.Distance(enemyRefrences.transform.position, enemyRefrences.player.position) <= 2f
                            ? atck 
                            : 0f
                ));

                decider.AddRule(new WeightedTransition(
                    follow, castFast,
                    () => Vector3.Distance(enemyRefrences.transform.position, enemyRefrences.player.position) >= 3f
                            ? fastSpell 
                            : 1f
                ));

                decider.AddRule(new WeightedTransition(
                    follow, castMed,
                    () => Vector3.Distance(enemyRefrences.transform.position, enemyRefrences.player.position) >= 3f
                            ? mediumSpell 
                            : 1f
                ));

                decider.AddRule(new WeightedTransition(
                    follow, castSlow,
                    () => Vector3.Distance(enemyRefrences.transform.position, enemyRefrences.player.position) >= 3f
                            ? slowSpell 
                            : 0f
                ));
                
                //Defensive rules
                decider.AddRule(new WeightedTransition(
                    follow, cover,
                    () => Vector3.Distance(enemyRefrences.transform.position, enemyRefrences.player.position) < 5f
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
                    () => Vector3.Distance(enemyRefrences.transform.position, enemyRefrences.player.position) > 4f && playerFiredStaff
                            ? wallRangedAttack 
                            : 0f
                ));

                decider.AddRule(new WeightedTransition(
                    follow, heal,
                    () => enemy.CurrentHealth <= (enemy.maxHealth * 0.8f) && 
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
                    () => !cover.IsHidden && enemy.CurrentHealth < 300f && 
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
                    () => Vector3.Distance(enemyRefrences.transform.position, enemyRefrences.player.position) <= 7f
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
                    () => Vector3.Distance(enemyRefrences.transform.position, enemyRefrences.player.position) <= 15f
                    && Vector3.Distance(enemyRefrences.transform.position, enemyRefrences.player.position) > 2f
                    && playerFiredStaff
                            ? defendTank 
                            : 0f
                ));

                decider.AddRule(new WeightedTransition(
                    follow, spawnTank,
                    () => Vector3.Distance(enemyRefrences.transform.position, enemyRefrences.player.position) <= 15f
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
                    () => Vector3.Distance(enemyRefrences.transform.position, enemyRefrences.player.position) > 0f
                            ? justRun 
                            : 0f
                );

                decider.AddRule(
                    cover,
                    () => !cover.IsHidden && Vector3.Distance(enemyRefrences.transform.position, enemyRefrences.player.position) > 20f
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
            break;
            case EnemyRole.Exploder:
                var explode = new ExplodeState(enemyRefrences);
                stateMachine.SetState(explode);
            break;
            case EnemyRole.Bouncer:
                var followMaster = new FollowMasterState(enemyRefrences);
                var guard        = new GuardState(enemyRefrences);
                var attackBouncer= new BouncerAttackState(enemyRefrences);
                var dieBouncer   = new DieState(enemyRefrences);

                float guardDist  = 3f;              // how close before guarding
                float attackDist = enemyRefrences.attackRange; // assume also 3f

                stateMachine.AddTransition(followMaster,guard,() =>
                    Vector3.Distance(enemyRefrences.transform.position,enemyRefrences.master.position) > guardDist
                );

                // stateMachine.AddTransition(guard,attackBouncer,() => 
                //     Vector3.Distance(enemyRefrences.transform.position, enemyRefrences.player.position) <= attackDist
                // );

                stateMachine.AddTransition(guard, attackBouncer, () =>
                    Vector3.Distance(enemyRefrences.transform.position, enemyRefrences.player.position) 
                                    <= attackDist && ((BouncerAttackState)attackBouncer).IsReady
                );

                stateMachine.AddTransition(attackBouncer, guard,() => 
                    ((BouncerAttackState)attackBouncer).HasHit
                );

                stateMachine.AddTransition(guard,followMaster,() => 
                    Vector3.Distance(enemyRefrences.transform.position, enemyRefrences.master.position) > guardDist
                );  

                stateMachine.AddTransition(guard, dieBouncer, () => 
                    guard.TimeUp
                );
                
                stateMachine.SetState(followMaster);
            break;
            case EnemyRole.Tank:
                var idleTank  = new IdleState(enemyRefrences);
                var chaseTank = new ChasePlayerState(enemyRefrences);
                var attackTank = new TankAttackState(enemyRefrences);

                float detect = enemyRefrences.detectDistance;
                float attackDistTank = enemyRefrences.attackDistance;

                // Idle -> Chase when player detected
                stateMachine.AddTransition(
                    idleTank, chaseTank,
                    () => Vector3.Distance(
                            enemyRefrences.transform.position,
                            enemyRefrences.player.position)
                            <= detect
                );
                // Chase -> Attack when in close range
                stateMachine.AddTransition(
                    chaseTank, attackTank,
                    () => Vector3.Distance(
                            enemyRefrences.transform.position,
                            enemyRefrences.player.position)
                            <= attackDistTank
                );
                // Attack -> Chase if player moves out of attack range but still in detect range
                stateMachine.AddTransition(
                    attackTank, chaseTank,
                    () => Vector3.Distance(
                            enemyRefrences.transform.position,
                            enemyRefrences.player.position)
                            > attackDistTank
                );

                
                // Chase -> Idle if player leaves detect range
                stateMachine.AddTransition(
                    chaseTank, idleTank,
                    () => Vector3.Distance(
                            enemyRefrences.transform.position,
                            enemyRefrences.player.position)
                            > detect
                );

                // Start in Idle
                stateMachine.SetState(idleTank);
            break;
        }
    }

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

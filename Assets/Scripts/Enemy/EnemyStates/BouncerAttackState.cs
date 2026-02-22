using UnityEngine;
using UnityEngine.AI;

public class BouncerAttackState : IState
{
    private readonly EnemyRefrences _refs;
    private const float COOLDOWN = 1f;       // 1 second between attacks
    private const float WINDUP_TIME = 0.4f;  // Telegraph before impact
    private float       _lastHitTime = -COOLDOWN;
    private float       _windupStartTime;
    private bool        _isWinding;
    private bool        _damageDealt;
    private readonly Transform      _player;
    public  bool                   HasHit { get; private set; }
    public bool IsReady => Time.time >= _lastHitTime + COOLDOWN;

    public Animator _anim; 

    public BouncerAttackState(EnemyRefrences refs)
    {
        _refs   = refs;
        var go  = GameObject.FindWithTag("Player");
        _anim   = refs.animator;
        _player = go != null ? go.transform : null;
    }

    public void OnEnter()
    {
        HasHit = false;
        _isWinding = false;
        _damageDealt = false;
        if (_refs.agent != null && _refs.agent.isOnNavMesh && _refs.agent.isActiveAndEnabled)
            _refs.agent.isStopped = false;
    }

    private void StartWindup()
    {
        if (_player == null || _isWinding || _damageDealt) return;
        if (!IsReady) return;

        float dist = Vector3.Distance(_refs.transform.position, _player.position);
        
        // Use attack range directly from _refs (already scaled by TelekinesisController if enlarged)
        float attackRange = _refs.attackRange;
        
        if (dist > attackRange) return;
        
        // Start windup
        _isWinding = true;
        _windupStartTime = Time.time;
        _anim?.SetTrigger("attack");
        
        // Stop moving during windup
        if (_refs.agent != null && _refs.agent.isOnNavMesh && _refs.agent.isActiveAndEnabled)
            _refs.agent.isStopped = true;
    }
    
    private void CheckHit()
    {
        if (!_isWinding || _damageDealt) return;
        if (Time.time < _windupStartTime + WINDUP_TIME) return;
        
        // Windup complete, check if player is still in range
        if (_player == null) return;
        
        float dist = Vector3.Distance(_refs.transform.position, _player.position);
        
        // Use attack range directly from _refs (already scaled by TelekinesisController if enlarged)
        float attackRange = _refs.attackRange;
        
        bool hitLanded = dist <= attackRange;
        
        var enemy = _refs.GetComponent<Enemy>();
        
        if (hitLanded)
        {
            // Hit! Apply damage and knockback
            PlayerData.takeDamage(_refs.attackDamage);
            
            ActionLogger.Instance?.LogActionWithContext(
                actor:     "Enemy",
                actionType:"Enemy_BouncerAttack",   
                target:     "Player", 
                isHit:      true,
                damage:     _refs.attackDamage,
                distance:   dist,
                actorHealthPercent: enemy?.CurrentHealth / enemy?.maxHealth ?? -1f,
                targetHealthPercent: PlayerData.playerHealth / 100f,
                actorState: "Attacking",
                wasSuccessful: true
            );
            
            // Knockback
            var rb = _refs.playerRigidbody;
            if (rb != null)
            {
                Vector3 dir = (_player.position - _refs.transform.position).normalized;
                rb.AddForce(dir * _refs.knockbackForce, ForceMode.Impulse);
            }
        }
        else
        {
            // Miss! Player dodged
            ActionLogger.Instance?.LogActionWithContext(
                actor:     "Enemy",
                actionType:"Enemy_BouncerAttack",   
                target:     "Player", 
                isHit:      false,
                damage:     0f,
                distance:   dist,
                actorHealthPercent: enemy?.CurrentHealth / enemy?.maxHealth ?? -1f,
                targetHealthPercent: PlayerData.playerHealth / 100f,
                actorState: "Attacking",
                wasSuccessful: false
            );
        }
        
        _damageDealt = true;
        _lastHitTime = Time.time;
        HasHit = true;
    }

    public void Tick() 
    { 
        if (HasHit) return;
        
        // Check if destroyed or not on NavMesh
        if (_player == null || _refs == null || _refs.agent == null) return;
        if (!_refs.agent.isOnNavMesh || !_refs.agent.isActiveAndEnabled) return;

        // Try to start windup if not already winding up
        if (!_isWinding)
        {
            _refs.agent.SetDestination(_player.position);
            StartWindup();
        }
        else
        {
            // During windup, check if hit should land
            CheckHit();
        }
    }
    public void OnExit()
    {
        _anim?.ResetTrigger("attack"); 
    }
    public Color GizmoColor() => Color.red;
}
using UnityEngine;
using UnityEngine.AI;

public class BouncerAttackState : IState
{
    private readonly EnemyRefrences _refs;
    private const float COOLDOWN = 1f;       // 1 second between attacks
    private float       _lastHitTime = -COOLDOWN;
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
        if (_refs.agent != null && _refs.agent.isOnNavMesh && _refs.agent.isActiveAndEnabled)
            _refs.agent.isStopped = false;

        TryHit();
    }

    private void TryHit()
    {
        if (_player == null) return;


        // Check if player is within attack range
        float dist = Vector3.Distance(_refs.transform.position, _player.position);
        if (dist > _refs.attackRange || !IsReady) return;
        
        // Damage
        _anim?.SetTrigger("attack");
        PlayerData.takeDamage(_refs.attackDamage);
        
        var enemy = _refs.GetComponent<Enemy>();
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
        else
        {
            Debug.LogWarning("Player does not have a Rigidbody component for knockback.");
        }

        _lastHitTime = Time.time;
        HasHit       = true;
        if (_refs.agent != null && _refs.agent.isOnNavMesh && _refs.agent.isActiveAndEnabled)
            _refs.agent.isStopped = true;
        
    }

    public void Tick() 
    { 
        if (HasHit) return;
        
        // Check if destroyed or not on NavMesh
        if (_player == null || _refs == null || _refs.agent == null) return;
        if (!_refs.agent.isOnNavMesh || !_refs.agent.isActiveAndEnabled) return;

        // keep chasing
        _refs.agent.SetDestination(_player.position);
        TryHit();
    }
    public void OnExit()
    {
        _anim?.ResetTrigger("attack"); 
    }
    public Color GizmoColor() => Color.red;
}
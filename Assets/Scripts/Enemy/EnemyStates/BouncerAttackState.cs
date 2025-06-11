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
        ActionLogger.Instance.LogAction(
            actor:     "Enemy",
            actionType:"Bouncer",   
            target:     "Player", 
            isHit:      true,
            damage:     _refs.attackDamage,
            distance:   dist
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
        _refs.agent.isStopped = true;
        
    }

    public void Tick() 
    { 
        if (HasHit) return;

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
using UnityEngine;

public class TankAttackState : IState
{
    private readonly EnemyRefrences _refs;
    private readonly Transform      _player;
    private float                   _lastAttack;
    private const float WINDUP_TIME = 1.5f;  // Slower, heavier attacks
    private float                   _attackStartTime;
    private bool                    _isWinding;
    private bool                    _damageDealt;

    private Animator _anim;

    public TankAttackState(EnemyRefrences refs)
    {
        _refs   = refs;
        _player = refs.player;
        _anim   = refs.animator;
        _lastAttack = -refs.attackCooldown;
    }

    public void OnEnter()
    {
        _isWinding = false;
        _damageDealt = false;
    }

    public void Tick()
    {
        if (_player == null) return;
        float dist = Vector3.Distance(_refs.transform.position, _player.position);

        // Face the player
        Vector3 dir = (_player.position - _refs.transform.position).normalized;
        if (dir.sqrMagnitude > 0.001f)
            _refs.transform.rotation = Quaternion.Slerp(
                _refs.transform.rotation,
                Quaternion.LookRotation(dir),
                Time.deltaTime * 5f
            );

        // Start attack windup if in range and off cooldown
        if (!_isWinding && !_damageDealt && dist <= _refs.attackDistance && Time.time >= _lastAttack + _refs.attackCooldown)
        {
            _isWinding = true;
            _attackStartTime = Time.time;
            _lastAttack = Time.time;
            _anim?.SetTrigger("attack");
        }
        
        // Apply damage after windup completes
        if (_isWinding && !_damageDealt && Time.time >= _attackStartTime + WINDUP_TIME)
        {
            // Check if player is still in range (they might have dodged)
            bool hitLanded = dist <= _refs.attackDistance;
            var enemy = _refs.GetComponent<Enemy>();
            
            if (hitLanded)
            {
                PlayerData.takeDamage(_refs.tankAttackDamage);
                
                ActionLogger.Instance?.LogActionWithContext(
                    actor:     "Enemy",
                    actionType:"Enemy_TankAttack",   
                    target:     "Player", 
                    isHit:      true,
                    damage:     _refs.tankAttackDamage,
                    distance:   dist,
                    actorHealthPercent: enemy?.CurrentHealth / enemy?.maxHealth ?? -1f,
                    targetHealthPercent: PlayerData.playerHealth / 100f,
                    actorState: "Attacking",
                    wasSuccessful: true
                );
            }
            else
            {
                // Player dodged!
                ActionLogger.Instance?.LogActionWithContext(
                    actor:     "Enemy",
                    actionType:"Enemy_TankAttack",   
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
            _isWinding = false;
        }
    }

    public void OnExit()
    {
        _anim?.ResetTrigger("attack");
    }

    public Color GizmoColor() => Color.red;
}
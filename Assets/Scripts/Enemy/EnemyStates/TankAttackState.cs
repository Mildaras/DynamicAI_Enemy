using UnityEngine;

public class TankAttackState : IState
{
    private readonly EnemyRefrences _refs;
    private readonly Transform      _player;
    private float                   _lastAttack;

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
        _anim?.SetTrigger("attack");
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

        // Attack on cooldown
        if (dist <= _refs.attackDistance && Time.time >= _lastAttack + _refs.attackCooldown)
        {
            _lastAttack = Time.time;
            _anim?.SetTrigger("attack");
            PlayerData.takeDamage(_refs.tankAttackDamage);
            ActionLogger.Instance.LogAction(
                actor:     "Enemy",
                actionType:"Tank",   
                target:     "Player", 
                isHit:      true,
                damage:     _refs.tankAttackDamage,
                distance:   0f
            );
        }
    }

    public void OnExit()
    {
        _anim?.ResetTrigger("attack");
    }

    public Color GizmoColor() => Color.red;
}
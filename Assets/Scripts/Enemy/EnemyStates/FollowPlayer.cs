using UnityEngine;
using UnityEngine.AI;

public class FollowPlayer : IState
{
    private readonly EnemyRefrences _refs;
    private readonly Transform      _player;
    private readonly NavMeshAgent   _agent;
    private readonly Animator       _anim;
    private float                   _keepDistance;
    private readonly float          _tolerance = 0.2f;

    public FollowPlayer(EnemyRefrences refs)
    {
        _refs         = refs;
        _player       = refs.player;
        _agent        = refs.agent;
        _anim         = refs.animator;
    }

    public void OnEnter()
    {
        if (_agent != null && _agent.isActiveAndEnabled && _agent.isOnNavMesh)
        {
            _agent.isStopped = false;
            _anim?.SetBool("hiding", false);
            _anim?.SetFloat("speed", _agent.speed);
        }
    }

    public void Tick()
    {
        _keepDistance = _refs.followDistance;
        // 1) Early out if agent/player invalid
        if (_agent == null 
            || !_agent.isActiveAndEnabled 
            || !_agent.isOnNavMesh 
            || _player == null)
            return;

        // 2) Compute distance & decide chase/retreat/hold
        Vector3 toPlayer = _player.position - _refs.transform.position;
        float   dist     = toPlayer.magnitude;

        Vector3 targetPos;
        Vector3 moveDir;

        if (dist > _keepDistance + _tolerance)
        {
            // chase
            targetPos = _player.position;
            moveDir   = toPlayer.normalized;
        }
        else if (dist < _keepDistance - _tolerance)
        {
            // back away
            moveDir   = (-toPlayer).normalized;
            targetPos = _refs.transform.position + moveDir * _keepDistance;
        }
        else
        {
            // hold
            targetPos = _refs.transform.position;
            moveDir   = _refs.transform.forward;
        }

        // 3) Face movement dir
        if (moveDir.sqrMagnitude > 0.001f)
        {
            Quaternion look = Quaternion.LookRotation(moveDir);
            _refs.transform.rotation = Quaternion.Slerp(
                _refs.transform.rotation,
                look,
                Time.deltaTime * 5f
            );
        }

        // 4) Move
        _agent.SetDestination(targetPos);

        // 5) Animate
        _anim?.SetFloat("speed", _agent.velocity.magnitude);
    }

    public void OnExit()
    {
        if (_agent != null && _agent.isActiveAndEnabled)
        {
            _agent.isStopped = true;
            _anim?.SetFloat("speed", 0f);
        }
    }

    public Color GizmoColor() => Color.blue;
}

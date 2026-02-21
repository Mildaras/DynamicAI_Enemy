using UnityEngine;
using UnityEngine.AI;

public class ChasePlayerState : IState
{
    private readonly EnemyRefrences _refs;
    private readonly NavMeshAgent   _agent;
    private readonly Transform      _player;
    private Animator       _anim;

    public ChasePlayerState(EnemyRefrences refs)
    {
        _refs  = refs;
        _agent = refs.agent;
        _player = refs.player;
        _anim  = refs.animator;
    }

    public void OnEnter()
    {
        if (_agent != null) _agent.isStopped = false;

        _anim?.SetFloat("speed", 1.5f);
    }

    public void Tick()
    {
        if (_agent == null || _player == null) return;
        _agent.SetDestination(_player.position);
    }

    public void OnExit()
    {
        if (_agent != null) _agent.ResetPath();

        _anim?.SetFloat("speed", 0f);
    }

    public Color GizmoColor() => Color.blue;
}
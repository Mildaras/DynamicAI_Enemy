using UnityEngine;
using UnityEngine.AI;

public class GuardState : IState
{
    private readonly EnemyRefrences _refs;
    private readonly NavMeshAgent   _agent;
    private readonly Transform      _master;
    private Vector3                _guardPosition;
    private bool                   _reached;
    private float                  _timer;
    public  bool                   TimeUp { get; private set; }

    public Animator _anim;

    public GuardState(EnemyRefrences refs)
    {
        _refs   = refs;
        _agent  = refs.agent;
        _anim   = refs.animator;
        _master = refs.master;
    }

    public void OnEnter()
    {
        _timer    = 0f;
        TimeUp    = false;
        _reached  = false;

        if (_agent != null && _master != null)
        {
            _agent.isStopped = false;

            // Compute guard position between master and player
            Vector3 masterPos = _master.position;
            Vector3 playerPos = _refs.player.position;
            Vector3 dir       = (playerPos - masterPos).normalized;
            _guardPosition    = masterPos + dir * _refs.guardDistance;

            // Snap to NavMesh
            if (NavMesh.SamplePosition(_guardPosition, out var hit, 1f, NavMesh.AllAreas))
                _guardPosition = hit.position;

            _agent.SetDestination(_guardPosition);
        }
    }

    public void Tick()
    {
        if (!_reached)
        {
            if (!_agent.pathPending && _agent.remainingDistance <= _agent.stoppingDistance)
            {
                _reached = true;
                _agent.isStopped = true;
            }
            return;
        }

        _timer += Time.deltaTime;
        _anim?.SetTrigger("defend");
        if (_timer >= _refs.guardTime)
            TimeUp = true;
    }

    public void OnExit()
    {
        if (_agent != null)
            _agent.isStopped = false;

        _anim?.ResetTrigger("defend");
    }

    public Color GizmoColor() => Color.yellow;
}
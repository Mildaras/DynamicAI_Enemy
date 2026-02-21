using UnityEngine;
using UnityEngine.AI;

public class FollowMasterState : IState
{
    private readonly EnemyRefrences _refs;
    private readonly NavMeshAgent   _agent;
    private readonly Transform      _master;
    private readonly float          _stopDistance = 3f;

    public FollowMasterState(EnemyRefrences refs)
    {
        _refs   = refs;
        _agent  = refs.agent;
        _master = refs.master;
    }

    public void OnEnter()
    {
        if (_agent != null && _agent.isOnNavMesh && _agent.isActiveAndEnabled)
            _agent.isStopped = false;
    }

    public void Tick()
    {
        if (_agent == null || _master == null || _refs.transform == null) return;
        if (!_agent.isOnNavMesh || !_agent.isActiveAndEnabled) return;

        float dist = Vector3.Distance(_refs.transform.position, _master.position);

        if (dist > _stopDistance)
        {
            // still too far → move toward him
            _agent.isStopped = false;
            _agent.SetDestination(_master.position);
        }
        else
        {
            // close enough → stop right here
            _agent.isStopped = true;
        }
    }

    public void OnExit()
    {
        if (_agent != null && _agent.isOnNavMesh && _agent.isActiveAndEnabled)
            _agent.ResetPath();
    }

    public Color GizmoColor() => Color.cyan;
}

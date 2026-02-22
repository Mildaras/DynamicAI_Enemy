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
        if (_agent != null && _agent.isOnNavMesh && _agent.isActiveAndEnabled)
            _agent.isStopped = false;

        _anim?.SetFloat("speed", 1.5f);
    }

    public void Tick()
    {
        if (_agent == null || _player == null) return;
        if (!_agent.isOnNavMesh || !_agent.isActiveAndEnabled) return;
        
        _agent.SetDestination(_player.position);
        
        // Fix rotation bug: manually rotate to face player if NavMesh rotation is off
        // This fixes the issue where enemies face wrong direction after being pushed
        if (_agent.velocity.sqrMagnitude > 0.1f) // Only rotate when moving
        {
            Vector3 direction = (_player.position - _refs.transform.position).normalized;
            direction.y = 0; // Keep rotation on horizontal plane only
            
            if (direction.sqrMagnitude > 0.01f)
            {
                Quaternion targetRotation = Quaternion.LookRotation(direction);
                _refs.transform.rotation = Quaternion.Slerp(
                    _refs.transform.rotation,
                    targetRotation,
                    Time.deltaTime * 5f // Smooth rotation speed
                );
            }
        }
    }

    public void OnExit()
    {
        if (_agent != null && _agent.isOnNavMesh && _agent.isActiveAndEnabled)
            _agent.ResetPath();

        _anim?.SetFloat("speed", 0f);
    }

    public Color GizmoColor() => Color.blue;
}
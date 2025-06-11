using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// State for an enemy that runs toward the player and explodes.
/// If it reaches within explosionRange, it deals damage and explodes immediately.
/// If lifespan elapses (e.g., 5s) without reaching, it explodes with no damage.
/// </summary>
public class ExplodeState : IState
{
    private readonly EnemyRefrences _refs;
    private readonly NavMeshAgent   _agent;
    private readonly Transform      _player;
    private readonly GameObject     _explosionPrefab;
    private readonly float          _explosionRange;
    private readonly float          _explosionDamage;
    private readonly float          _lifeSpan;

    private Animator _anim;

    private float _timer;
    private bool  _hasExploded;

    public ExplodeState(EnemyRefrences refs)
    {
        _refs              = refs;
        _agent             = refs.agent;
        _player            = refs.player;
        _explosionPrefab   = refs.explosionPrefab;
        _explosionRange    = refs.explosionRange;
        _explosionDamage   = refs.explosionDamage;
        _lifeSpan          = refs.explosionLifeSpan;
        _anim              = refs.animator;
    }

    public void OnEnter()
    {
        _timer       = 0f;
        _hasExploded = false;

        // Start running toward player
        if (_agent != null)
        {
            _agent.isStopped = false;
            _agent.SetDestination(_player.position);
        }
    }

    public void Tick()
    {
        if (_hasExploded)
            return;

        _timer += Time.deltaTime;

        // Continuously update destination
        if (_agent != null)
            _agent.SetDestination(_player.position);

        float distToPlayer = Vector3.Distance(_refs.transform.position, _player.position);

        // Explode if in range or lifespan elapsed
        if (distToPlayer <= _explosionRange || _timer >= _lifeSpan)
        {
            Explode(distToPlayer <= _explosionRange);
            
        }
    }

    private void Explode(bool dealDamage)
    {
        _hasExploded = true;

        // Spawn explosion effect
        if (_explosionPrefab != null)
            Object.Instantiate(_explosionPrefab, _refs.transform.position, Quaternion.identity);

        // Deal damage if close enough
        if (dealDamage)
        {
            ActionLogger.Instance.LogAction(
                actor:     "Enemy",
                actionType:"Exploder",   
                target:     "Player", 
                isHit:      true,
                damage:     _explosionDamage,
                distance:   0f
            );
            PlayerData.takeDamage(_explosionDamage);
        }
        else
        {
            ActionLogger.Instance.LogAction(
                actor:     "Enemy",
                actionType:"Exploder",   
                target:     "Player", 
                isHit:      false,
                damage:     0f,
                distance:   -1f
            );
        }

        // Destroy this enemy
        CameraShake.Instance.Shake(0.1f, 0.1f);
        Object.Destroy(_refs.gameObject);
    }

    public void OnExit() { }

    public Color GizmoColor() => Color.red;
}

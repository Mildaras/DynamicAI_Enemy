using UnityEngine;
using UnityEngine.AI;

public class SummonWall : IState
{
    private readonly EnemyRefrences _refs;
    private readonly NavMeshAgent   _agent;
    private readonly Animator       _anim;
    private readonly GameObject     _wallPrefab;
    private readonly float          _summonDistance;
    private readonly float          _riseDistance;
    private readonly float          _riseDuration;
    private readonly float          _wallLifetime;
    private bool                     _isDone;
    public  bool                     IsDone => _isDone;

    public SummonWall(EnemyRefrences refs)
    {
        _refs           = refs;
        _agent          = refs.agent;
        _anim           = refs.animator;
        _wallPrefab     = refs.wallPrefab;
        _summonDistance = refs.wallSummonDistance;
        _riseDistance   = refs.wallRiseDistance;
        _riseDuration   = refs.wallRiseDuration;
        _wallLifetime   = refs.wallLifetime;
        _isDone         = false;
    }

    public void OnEnter()
    {
        // 0) Face the player immediately
        if (_refs.player != null)
        {
            Vector3 toPlayer = _refs.player.position - _refs.transform.position;
            toPlayer.y = 0f;
            if (toPlayer.sqrMagnitude > 0.001f)
                _refs.transform.rotation = Quaternion.LookRotation(toPlayer);
        }

        // 1) Play summon‐wall animation & stop movement
        _anim?.SetTrigger("spawnExploders");
        if (_agent != null)
            _agent.isStopped = true;

        // 2) Compute spawn position in front of caster, snapped to NavMesh
        Vector3 origin = _refs.transform.position;
        Vector3 dir    = _refs.transform.forward;
        Vector3 pos    = origin + dir * _summonDistance;
        if (NavMesh.SamplePosition(pos, out var hit, 1f, NavMesh.AllAreas))
            pos = hit.position;

        // 3) Instantiate the wall with caster’s rotation
        if (_wallPrefab != null)
        {
            var wall = Object.Instantiate(
                _wallPrefab,
                pos,
                _refs.transform.rotation
            );

            // 4) Add carving obstacle so agents path around it
            var obstacle = wall.AddComponent<NavMeshObstacle>();
            obstacle.carving = true;

            // 5) Attach rise‐from‐ground effect with correct full distance
            var rise = wall.AddComponent<GroundSpawnEffect>();
            rise.riseDistance = _riseDistance;   // ← full distance here
            rise.riseDuration = _riseDuration;
            rise.buriedDepth = 1f; // ← distance to bury the wall
            CameraShake.Instance.Shake(0.8f, 0.1f);
            // 6) Schedule wall removal
            //Object.Destroy(wall, _wallLifetime);
        }

        // Mark state done so FSM can transition next frame
        _isDone = true;
    }

    public void Tick()
    {
        // nothing per‐frame needed
    }

    public void OnExit()
    {
        // Log wall summoning
        var enemy = _refs.GetComponent<Enemy>();
        ActionLogger.Instance?.LogActionWithContext(
            actor: "Enemy",
            actionType: "SummonWall",
            target: "Player",
            isHit: false,
            damage: 0f,
            distance: _refs.player != null ? Vector3.Distance(_refs.transform.position, _refs.player.position) : 0f,
            actorHealthPercent: enemy?.CurrentHealth / enemy?.maxHealth ?? -1f,
            targetHealthPercent: PlayerData.playerHealth / 100f,
            actorState: "Defensive",
            wasSuccessful: _isDone
        );
        
        // resume movement & clear trigger
        if (_agent != null)
            _agent.isStopped = false;
        _anim?.ResetTrigger("spawnExploders");
    }

    public Color GizmoColor() => Color.gray;
}

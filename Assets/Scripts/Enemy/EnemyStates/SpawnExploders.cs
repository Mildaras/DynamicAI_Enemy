using UnityEngine;
using UnityEngine.AI;

public class SpawnExploders : IState
{
    private readonly EnemyRefrences _refs;
    private bool                    _spawned;
    private Animator               _anim;

    public SpawnExploders(EnemyRefrences refs)
    {
        _refs    = refs;
        _anim    = refs.animator;
        _spawned = false;
    }

    public bool HasSpawned => _spawned;

    public void OnEnter()
    {
        _anim?.SetTrigger("spawnExploders");
        Vector3 center    = _refs.transform.position;
        float   angleStep = 360f / _refs.exploderCount;

        for (int i = 0; i < _refs.exploderCount; i++)
        {
            float angle = i * angleStep * Mathf.Deg2Rad;
            Vector3 offset = new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle))
                             * _refs.exploderRadius;
            Vector3 spawnPos = center + offset;

            if (NavMesh.SamplePosition(spawnPos, out var hit, 1f, NavMesh.AllAreas))
                spawnPos = hit.position;

            if (_refs.exploderLaunchProjectile != null)
            {
                // Launch visual projectile
                var projGO = Object.Instantiate(
                    _refs.exploderLaunchProjectile,
                    center,
                    Quaternion.identity
                );
                var launcher = projGO.AddComponent<ExploderLaunchProjectile>();
                launcher.targetPosition   = spawnPos;
                launcher.exploderPrefab   = _refs.exploderPrefab;
                launcher.travelTime       = _refs.launchTravelTimeExp;
                launcher.startScale       = _refs.launchStartScaleExp;
            }
            else
            {
                // fallback immediate spawn
                if (_refs.exploderPrefab != null)
                    Object.Instantiate(_refs.exploderPrefab, spawnPos, Quaternion.identity);
            }
        }

        _spawned = true;
    }

    public void Tick()  { }
    public void OnExit() 
    {
        ActionLogger.Instance?.LogAction(
                actor:     "Enemy",
                actionType:"Enemy_SpawnExploders",   
                target:     "Player", 
                isHit:      false,
                damage:     0f,
                distance:   0f
            );
        _anim?.ResetTrigger("spawnExploders");
    }
    public Color GizmoColor() => Color.yellow;
}
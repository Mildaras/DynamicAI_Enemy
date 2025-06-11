using UnityEngine;
using UnityEngine.AI;

public class SpawnBouncers : IState
{
    private readonly EnemyRefrences _refs;
    private bool _spawned;
    private Animator _anim;

    public bool HasSpawned => _spawned;

    public SpawnBouncers(EnemyRefrences refs)
    {
        _refs = refs;
        _anim = refs.animator;
        _spawned = false;
    }

    public void OnEnter()
    {
        _anim?.SetTrigger("spawnBouncers");
        var spawnerCollider = _refs.GetComponent<Collider>();
        Vector3 center = _refs.transform.position;
        Vector3 playerPos = _refs.player != null ? _refs.player.position : center;
        Vector3 dir = (playerPos - center).normalized;

        float minDist = 5f; // minimum distance from spawner to stop

        for (int i = 0; i < _refs.bouncerCount; i++)
        {
            // Evenly distribute between 1/(count+1) to count/(count+1)
            float frac = (i + 1) / (float)(_refs.bouncerCount + 1);
            // compute desired spawn distance along dir
            float rawDist = _refs.bouncerRadius * frac;
            float spawnDist = Mathf.Max(rawDist, minDist);

            Vector3 spawnPos = center + dir * spawnDist;

            // Snap to NavMesh
            if (NavMesh.SamplePosition(spawnPos, out var hit, 1f, NavMesh.AllAreas))
                spawnPos = hit.position;

            if (_refs.bouncerLaunchProjectile != null)
            {
                var projGO = Object.Instantiate(
                    _refs.bouncerLaunchProjectile,
                    center,
                    Quaternion.identity
                );
                var launcher = projGO.AddComponent<BouncerLaunchProjectile>();
                launcher.targetPosition = spawnPos;
                launcher.bouncerPrefab = _refs.bouncerPrefab;
                launcher.travelTime = _refs.launchTravelTime;
                launcher.startScale = _refs.launchStartScale;

                // Prevent launch projectile colliding with spawner
                var projCollider = projGO.GetComponent<Collider>();
                if (projCollider != null && spawnerCollider != null)
                {
                    Physics.IgnoreCollision(spawnerCollider, projCollider);
                }
            }
            else if (_refs.bouncerPrefab != null)
            {
                var bouncerGO = Object.Instantiate(_refs.bouncerPrefab, spawnPos, Quaternion.identity);
                var bouncerCollider = bouncerGO.GetComponent<Collider>();
                if (bouncerCollider != null && spawnerCollider != null)
                {
                    Physics.IgnoreCollision(spawnerCollider, bouncerCollider);
                }
            }
        }

        _spawned = true;
    }

    public void Tick() { }
    public void OnExit() 
    {
        ActionLogger.Instance.LogAction(
                actor:     "Enemy",
                actionType:"SpawnBouncers",   
                target:     "Player", 
                isHit:      false,
                damage:     0f,
                distance:   0f
            );
        _anim?.ResetTrigger("spawnBouncers");
    }

    public Color GizmoColor() => Color.cyan;
}
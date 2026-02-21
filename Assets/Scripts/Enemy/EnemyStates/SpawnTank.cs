using UnityEngine;
using UnityEngine.AI;

public class SpawnTank : IState
{
    private readonly EnemyRefrences _refs;
    private readonly Animator       _anim;
    private bool                    _hasSpawned;

    public bool HasSpawned => _hasSpawned;



    public SpawnTank(EnemyRefrences refs)
    {
        _refs       = refs;
        _anim       = refs.animator;
        _hasSpawned = false;
    }

    public void OnEnter()
    {
        _anim?.SetTrigger("spawnTanks");

        Vector3 center      = _refs.transform.position;
        Vector3 playerPos   = _refs.player.position;
        Vector3 direction   = (playerPos - center).normalized;
        float   fullDistance= Vector3.Distance(center, playerPos);

        for (int i = 0; i < _refs.spawnCount; i++)
        {
            float frac      = (i + 1) / (float)(_refs.spawnCount + 1);
            Vector3 spawnPos = center + direction * (fullDistance * frac);

            if (NavMesh.SamplePosition(spawnPos, out var hit, 1f, NavMesh.AllAreas))
                spawnPos = hit.position;

            if (_refs.spawnShatterEffectPrefab != null)
            {
                Debug.Log($"[SpawnTank] Spawning shatter VFX at {spawnPos}");
                var vfx = Object.Instantiate(
                    _refs.spawnShatterEffectPrefab,
                    spawnPos,
                    Quaternion.Euler(90, 0, 0)
                );
            }

            // ── 2) Instantiate the tank itself ────────────────────────────────────
            if (_refs.tankPrefab != null)
            {
                var tankGO = Object.Instantiate(
                    _refs.tankPrefab,
                    spawnPos,
                    Quaternion.identity
                );

                // rise‐from‐ground effect
                var rise = tankGO.AddComponent<TankSpawnEffect>();
                rise.riseDistance = 0.001f;
                rise.riseDuration = 3f;
                CameraShake.Instance.Shake(3f, 0.1f);

                // play its “rise” trigger
                var tankAnim = tankGO.GetComponent<Animator>();
                if (tankAnim != null)
                    tankAnim.SetTrigger("rise");
            }
        }

        _hasSpawned = true;
    }

    public void Tick()  { }
    public void OnExit()
    {
        ActionLogger.Instance?.LogAction(
                actor:     "Enemy",
                actionType:"Enemy_SpawnTank",   
                target:     "Player", 
                isHit:      false,
                damage:     0f,
                distance:   0f
            );
        _anim?.ResetTrigger("spawnTanks");
    }
    public Color GizmoColor() => Color.green;
}

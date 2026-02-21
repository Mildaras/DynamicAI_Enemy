using UnityEngine;

[RequireComponent(typeof(Transform))]
public class GroundSpawnEffect : MonoBehaviour
{
    [Header("Rise/Sink Settings")]
    [Tooltip("How far below the final buried position the object starts.")]
    public float riseDistance  = 5f;
    [Tooltip("Seconds it takes to rise (and to sink).")]
    public float riseDuration  = 0.8f;
    [Tooltip("How much remains buried at the top.")]
    public float buriedDepth   = 1f;
    [Tooltip("How long to wait at the top (in seconds) before sinking.")]
    public float topHoldTime   = 2f;

    [Header("Shard VFX (while rising)")]
    [Tooltip("Prefab of the little rock‐shard ParticleSystem.")]
    public GameObject shardEffectPrefab;
    [Tooltip("Seconds between each shard burst while rising.")]
    public float shardInterval = 0.2f;
    [Tooltip("Radius around the base where shards should burst.")]
    public float shardRadius   = 1f;

    Vector3 _startPos;
    Vector3 _endPos;
    float   _startTime;
    bool    _isRising = true;
    float   _lastShardTime;

    void Awake()
    {
        // Compute buried end position
        _endPos   = transform.position - Vector3.up * buriedDepth;
        // Start fully underground
        _startPos = _endPos - Vector3.up * riseDistance;
        transform.position = _startPos;

        _startTime     = Time.time;
        _lastShardTime = Time.time;
    }

    void Update()
    {
        float elapsed = Time.time - _startTime;

        if (_isRising)
        {
            // Phase 1: rise up
            if (elapsed < riseDuration)
            {
                float t = elapsed / riseDuration;
                t = t * t * (3f - 2f * t); // smoothstep
                transform.position = Vector3.Lerp(_startPos, _endPos, t);

                // spawn shards
                if (shardEffectPrefab != null
                    && Time.time - _lastShardTime >= shardInterval)
                {
                    _lastShardTime = Time.time;
                    SpawnShardBurst();
                }
            }
            else if (elapsed < riseDuration + topHoldTime)
            {
                // Phase 2: hold at top, nothing moves
                transform.position = _endPos;
            }
            else
            {
                // Phase 3: begin sinking
                _isRising  = false;
                _startTime = Time.time; // reset timer for sink
            }
        }
        else
        {
            // sinking down
            if (elapsed < riseDuration)
            {
                CameraShake.Instance.Shake(0.3f, 0.1f);
                float t = elapsed / riseDuration;
                t = t * t * (3f - 2f * t); // smoothstep
                transform.position = Vector3.Lerp(_endPos, _startPos, t);
            }
            else
            {
                // done — disable this component so it stops running
                enabled = false;
            }
        }
    }

    private void SpawnShardBurst()
    {
        // pick a random spot around the _endPos circle
        Vector2 c = Random.insideUnitCircle * shardRadius;
        Vector3 spawn = _endPos + new Vector3(c.x, 0, c.y);

        var vfxGO = Instantiate(
            shardEffectPrefab,
            spawn,
            Quaternion.LookRotation(Vector3.up)
        );
        var ps = vfxGO.GetComponent<ParticleSystem>();
        if (ps != null)
        {
            ps.Play();
            float life = ps.main.duration + ps.main.startLifetime.constantMax;
            Destroy(vfxGO, life);
        }
        else
        {
            Destroy(vfxGO, 2f);
        }
    }
}

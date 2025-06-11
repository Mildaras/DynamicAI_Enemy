using UnityEngine;

[RequireComponent(typeof(Collider))]
public class ImpactOnGround : MonoBehaviour
{
    [Tooltip("Prefab of the shatter‐rocks ParticleSystem")]
    public GameObject impactEffectPrefab;

    [Tooltip("Tag that denotes the ground")]
    public string groundTag = "Ground";

    [Tooltip("Destroy this projectile on impact")]
    public bool destroySelf = true;

    void Reset()
    {
        var col = GetComponent<Collider>();
        col.isTrigger = true;
        if (!TryGetComponent<Rigidbody>(out var rb))
            rb = gameObject.AddComponent<Rigidbody>();
        rb.isKinematic = true;
    }

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag(groundTag))
            return;

        // spawn at our position
        Vector3 spawnPos = transform.position;

        // estimate ground normal
        Vector3 closest = other.ClosestPoint(spawnPos);
        Vector3 normal  = (spawnPos - closest).normalized;
        if (normal.sqrMagnitude < 0.01f) normal = Vector3.up;

        if (impactEffectPrefab != null)
        {
            // 1) Spawn the particle effect
            var go = Instantiate(
                impactEffectPrefab,
                spawnPos,
                Quaternion.LookRotation(normal)
            );
            CameraShake.Instance.Shake(0.1f, 0.1f);

            // 2) Copy the ground's material onto the particle renderer
            if (other.TryGetComponent<Renderer>(out var hitRenderer))
            {
                var psr = go.GetComponent<ParticleSystemRenderer>();
                if (psr != null && hitRenderer.sharedMaterial != null)
                {
                    psr.material = new Material(hitRenderer.sharedMaterial);
                }
            }
        }

        if (destroySelf)
            Destroy(gameObject);
    }
}

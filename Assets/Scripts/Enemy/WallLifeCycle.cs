using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Transform))]
public class WallLifecycle : MonoBehaviour
{
    [Tooltip("Total seconds before the wall shatters and disappears")]
    public float   lifetime = 5f;
    [Tooltip("Prefab of the shatter‐rocks ParticleSystem")]
    public GameObject shatterEffectPrefab;

    private IEnumerator Start()
    {
        // wait full lifetime
        yield return new WaitForSeconds(lifetime);

        // spawn shatter effect at the wall's feet
        if (shatterEffectPrefab != null)
        {
            var vfx = Instantiate(
                shatterEffectPrefab,
                transform.position,
                Quaternion.identity
            );
            var ps = vfx.GetComponent<ParticleSystem>();
            if (ps != null)
            {
                float life = ps.main.duration + ps.main.startLifetime.constantMax;
                Destroy(vfx, life);
            }
            else
            {
                Destroy(vfx, 3f);
            }
        }

        // finally destroy the wall
        Destroy(gameObject);
    }
}
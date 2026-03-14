using UnityEngine;

/// <summary>
/// Added to a movable object when launched by the telekinesis weapon.
/// Enemy hit = damage + knockback. Object always survives (component removed on impact).
/// </summary>
public class TelekinesisProjectile : MonoBehaviour
{
    private float _damage;
    private float _knockbackForce;
    private bool _hasHit;
    private float _lifetime = 10f;
    private float _timer;
    private GameObject _shatterEffect;

    public void Initialize(float damage, float knockbackForce, GameObject shatterEffect = null)
    {
        _damage = damage;
        _knockbackForce = knockbackForce;
        _shatterEffect = shatterEffect;
    }

    void Update()
    {
        _timer += Time.deltaTime;
        if (_timer >= _lifetime)
            Destroy(this);
    }

    void OnCollisionEnter(Collision collision)
    {
        if (_hasHit) return;

        var enemy = collision.gameObject.GetComponent<Enemy>();
        if (enemy != null)
        {
            _hasHit = true;
            enemy.TakeDamage(_damage);

            // Knockback in the direction the object was traveling
            enemy.allowExternalVelocity = true;
            var enemyRb = collision.gameObject.GetComponent<Rigidbody>();
            if (enemyRb != null)
            {
                var myRb = GetComponent<Rigidbody>();
                Vector3 knockDir = myRb != null
                    ? myRb.linearVelocity.normalized
                    : (collision.transform.position - transform.position).normalized;
                enemyRb.AddForce(knockDir * _knockbackForce, ForceMode.Impulse);
            }

            // Reset enemy after knockback wears off
            var reset = collision.gameObject.AddComponent<TelekinesisKnockbackReset>();
            reset.Initialize(enemy, 0.5f);

            ActionLogger.Instance?.LogAction(
                actor:      "Player",
                actionType: "Player_Telekinesis_Throw",
                target:      collision.gameObject.name,
                isHit:       true,
                damage:      _damage,
                distance:    0f
            );

            Debug.Log($"<color=cyan>[Telekinesis Throw] Hit {collision.gameObject.name} for {_damage:F0} damage + knockback</color>");

            // Shatter — spawn VFX and destroy the rock
            if (_shatterEffect != null)
                Instantiate(_shatterEffect, transform.position, Quaternion.identity);
            Destroy(gameObject);
            return;
        }

        // Missed enemy — object survives, remove projectile component
        Destroy(this);
    }
}

public class TelekinesisKnockbackReset : MonoBehaviour
{
    private Enemy _enemy;
    private float _duration;
    private float _timer;

    public void Initialize(Enemy enemy, float duration)
    {
        _enemy = enemy;
        _duration = duration;
    }

    void Update()
    {
        _timer += Time.deltaTime;
        if (_timer >= _duration)
        {
            if (_enemy != null)
            {
                _enemy.allowExternalVelocity = false;
                var rb = _enemy.GetComponent<Rigidbody>();
                if (rb != null)
                {
                    rb.linearVelocity = Vector3.zero;
                    rb.angularVelocity = Vector3.zero;
                }
            }
            Destroy(this);
        }
    }
}

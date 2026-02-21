using UnityEngine;

public class SpellProjectile : MonoBehaviour
{
    private Vector3 _direction;
    private float   _speed;
    private float   _damage;
    private float   _lifeTime;

    private bool _isReflected = false;
    public void MarkReflected() => _isReflected = true;
    public bool IsReflected  => _isReflected;

    /// <summary>
    /// Call right after Instantiate.
    /// </summary>
    public void Initialize(Vector3 targetPos, float speed, float damage, float lifeTime)
    {
        // Compute and store a normalized direction once
        _direction = (targetPos - transform.position).normalized;
        _speed     = speed;
        _damage    = damage;
        _lifeTime  = lifeTime;

        // Clean up after its lifetime
        Destroy(gameObject, _lifeTime);
    }

    void Update()
    {
        // Fly in that fixed direction every frame
        transform.position += _direction * _speed * Time.deltaTime;
    }

    void OnTriggerEnter(Collider other)
    {
        // 1) If this is a reflected bolt, only hit enemies
        if (_isReflected)
        {
            if (!other.CompareTag("Enemy")) return;

            var d = other.GetComponent<IDamageable>();
            if (d != null)
            {
                d.TakeDamage(_damage);
                ActionLogger.Instance?.LogAction(
                    actor:     "Player",
                    actionType:"Player_ReflectedSpell",   
                    target:     other.gameObject.name, 
                    isHit:      true,
                    damage:     _damage,
                    distance:   Vector3.Distance(transform.position, other.transform.position)
                );
                Destroy(gameObject);
            }
            
        }
        // 2) If this is a normal (enemy-fired) bolt, only hit the player
        else
        {
            if (!other.CompareTag("Buyer")) return;
            PlayerData.takeDamage(_damage);
            ActionLogger.Instance?.LogAction(
                actor:     "Enemy",
                actionType:"Enemy_SpellHit",   
                target:     "Player", 
                isHit:      true,
                damage:     _damage,
                distance:   Vector3.Distance(transform.position, other.transform.position)
            );
            Destroy(gameObject);
        }
    }
    
    public float GetDamage()    => _damage;
    public float GetSpeed()     => _speed;
    public float GetLifeTime()  => _lifeTime;

    
}

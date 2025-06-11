using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Collider))]
public class Projectile : MonoBehaviour
{
    private Vector3 _lastTargetPos;
    private Vector3 _spawnPos;
    private float   _speed;
    private float   _damage;
    private float   _lifeTime;

    private bool    _isReflected = false;
    public void    MarkReflected() => _isReflected = true;
    public bool    IsReflected   => _isReflected;

    /// <summary>
    /// Call this immediately after Instantiate.
    /// </summary>
    public void Initialize(Transform targetTransform, float damage, float speed, float lifeTime)
    {
        _lastTargetPos = targetTransform.position;

        _spawnPos   = transform.position;
        _damage     = damage;
        _speed      = speed;
        _lifeTime   = lifeTime;

        Destroy(gameObject, lifeTime);
    }


    public void UpdateTarget(Vector3 newTargetPos)
    {
        _lastTargetPos = newTargetPos;
    }

    void Update()
    {
        Vector3 dir = (_lastTargetPos - transform.position).normalized;
        transform.position += dir * _speed * Time.deltaTime;
        transform.forward  = dir;
    }

    void OnTriggerEnter(Collider other)
    {
        // ── If I’ve reflected this bolt, only hit enemies ─────────────
        if (_isReflected)
        {
            if (!other.CompareTag("Enemy"))
                return;
        }


        // now apply damage to whatever DID implement IDamageable
        var d = other.GetComponent<IDamageable>();
        if (d != null)
        {
            // log before you apply
            float dist = Vector3.Distance(_spawnPos, other.transform.position);

            d.TakeDamage(_damage);
            ActionLogger.Instance.LogAction(
                actor:     "Player",
                actionType:"Spell",   
                target:     other.gameObject.name, 
                isHit:      true,
                damage:     _damage,
                distance:   dist
            );
            SpawnFloatingText($"{(int)_damage}", Color.magenta, transform.position);
            Destroy(gameObject);
        }
        else
        {
            ActionLogger.Instance.LogAction(
                actor:     "Player",
                actionType:"Spell",   
                target:     "Enemy", 
                isHit:      false,
                damage:     0f,
                distance:   Vector3.Distance(_spawnPos, other.transform.position)
            );
        }
    }

    [SerializeField] private GameObject floatingTextPrefab;

    private void SpawnFloatingText(string text, Color color, Vector3 worldPos)
    {
        if (floatingTextPrefab == null) return;
        var go = Instantiate(floatingTextPrefab, worldPos, Quaternion.identity);
        var ft = go.GetComponent<FloatingText>();
        if (ft != null)
            ft.Initialize(text, color);
    }
}

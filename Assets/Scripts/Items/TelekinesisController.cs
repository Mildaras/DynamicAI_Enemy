using UnityEngine;
using System.Collections.Generic;

public class TelekinesisController : MonoBehaviour
{

    private Dictionary<EnemyController, float> chargeTimers = new();
    [SerializeField] private float overloadTime = 3f;
    [SerializeField] private float maxScale = 2f;
    [SerializeField] private GameObject explosionEffect;

    [Header("Telekinesis Settings")]
    [SerializeField] private float pullForce = 30f;
    [SerializeField] private float maxRange = 20f;
    [SerializeField] private float cooldown = 1.5f;
    [SerializeField] private LayerMask enemyLayer;

    [Header("Indicator")]
    [SerializeField] private GameObject indicatorPrefab;
    [SerializeField] private float indicatorYOffset = 0.1f;

    [Header("Aiming")]
    [SerializeField] private Camera cam;
    [SerializeField] private float maxAimDistance = 100f;

    private float _nextAllowedCast = 0f;
    private GameObject _indicatorInst;

    

    void Awake()
    {
        if (cam == null)
            cam = Camera.main;

        if (cam == null)
            Debug.LogError("TelekinesisController: No camera assigned or tagged MainCamera!");
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(0) && Time.time >= _nextAllowedCast)
        {
            _indicatorInst = Instantiate(indicatorPrefab);
            _nextAllowedCast = Time.time + cooldown;
        }

        if (Input.GetMouseButton(0) && _indicatorInst != null)
        {
            UpdateIndicatorPosition();
            PullEnemiesTowardIndicator();
        }

        if (Input.GetMouseButtonUp(0) && _indicatorInst != null)
        {
            Destroy(_indicatorInst);
            _indicatorInst = null;
        }

        if (Input.GetMouseButtonUp(0) && _indicatorInst != null)
        {
            Destroy(_indicatorInst);
            _indicatorInst = null;
            chargeTimers.Clear(); // Reset charging
        }

    }

    private void UpdateIndicatorPosition()
    {
        Ray ray = cam.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit, maxAimDistance))
        {
            Vector3 point = hit.point + Vector3.up * indicatorYOffset;
            _indicatorInst.transform.position = point;
        }
        else
        {
            Vector3 point = ray.origin + ray.direction * maxAimDistance;
            _indicatorInst.transform.position = point + Vector3.up * indicatorYOffset;
        }
    }

    private void PullEnemiesTowardIndicator()
    {

        Collider[] colliders = Physics.OverlapSphere(_indicatorInst.transform.position, maxRange);

        foreach (var col in colliders)
        {
            if (!col.TryGetComponent<EnemyController>(out var enemy)) continue;
            if (enemy.role == EnemyRole.Main) continue; // skip main boss

            Rigidbody rb = col.GetComponent<Rigidbody>();
            if (rb == null) continue;

            Vector3 dir = (_indicatorInst.transform.position - col.transform.position).normalized;
            rb.AddForce(dir * pullForce, ForceMode.Acceleration);

            // Start or continue charging
            if (!chargeTimers.ContainsKey(enemy))
                chargeTimers[enemy] = 0f;

            chargeTimers[enemy] += Time.deltaTime;

            // Scale up based on time held
            float t = Mathf.Clamp01(chargeTimers[enemy] / overloadTime);
            enemy.transform.localScale = Vector3.one * Mathf.Lerp(1f, maxScale, t);

            // Overload check
            if (chargeTimers[enemy] >= overloadTime)
            {
                ExplodeEnemy(enemy, col);
            }
        }
    }

   private void ExplodeEnemy(EnemyController enemy, Collider col)
    {
        if (explosionEffect != null)
            Instantiate(explosionEffect, enemy.transform.position, Quaternion.identity);
        float currentHealth = col.GetComponent<Enemy>().CurrentHealth;

        float dist = Vector3.Distance(_indicatorInst.transform.position, enemy.transform.position);

        ActionLogger.Instance.LogAction(
                actor:     "Player",
                actionType:"Wand",   
                target:     enemy.gameObject.name, 
                isHit:      true,
                damage:     currentHealth,
                distance:   dist
            );

        Destroy(enemy.gameObject);
        chargeTimers.Remove(enemy);
    }
}

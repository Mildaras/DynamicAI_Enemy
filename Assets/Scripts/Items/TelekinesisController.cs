using UnityEngine;
using System.Collections.Generic;

public class TelekinesisController : MonoBehaviour
{

    private Dictionary<EnemyController, float> chargeTimers = new();
    private HashSet<EnemyController> affectedEnemies = new HashSet<EnemyController>();
    private Dictionary<EnemyController, Vector3> originalScales = new Dictionary<EnemyController, Vector3>();
    private Dictionary<EnemyController, float> originalStoppingDistances = new Dictionary<EnemyController, float>();
    
    [SerializeField] private float overloadTime = 3f;
    [SerializeField] private float maxScale = 2f;
    [SerializeField] private GameObject explosionEffect;
    [SerializeField] private float stopDistance = 2f; // Stop pulling when this close to indicator
    
    [Header("Explosion Settings")]
    [SerializeField] private float explosionRadius = 5f;
    [SerializeField] private float explosionDamage = 100f;
    [SerializeField] private bool damagePlayer = true;
    [SerializeField] private float cameraShakeDuration = 0.3f;
    [SerializeField] private float cameraShakeMagnitude = 0.2f;

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
            
            // IMPORTANT: Reset velocity and rotation for all affected enemies
            foreach (var enemy in affectedEnemies)
            {
                if (enemy != null)
                {
                    var rb = enemy.GetComponent<Rigidbody>();
                    if (rb != null)
                    {
                        rb.linearVelocity = Vector3.zero; // Stop all movement
                        rb.angularVelocity = Vector3.zero; // Stop rotation
                    }
                    
                    // Re-enable auto velocity reset and mark as enlarged
                    var enemyScript = enemy.GetComponent<Enemy>();
                    if (enemyScript != null)
                    {
                        enemyScript.allowExternalVelocity = false;
                        // Mark enemy as enlarged - they can never be affected again
                        enemyScript.hasBeenEnlarged = true;
                    }
                    
                    // CRITICAL FIX: Permanently scale attack ranges in EnemyRefrences to match enlarged size
                    if (originalScales.ContainsKey(enemy))
                    {
                        float finalScale = enemy.transform.localScale.x;
                        float originalScale = originalScales[enemy].x;
                        float scaleMultiplier = finalScale / originalScale;
                        
                        var refs = enemy.GetComponent<EnemyRefrences>();
                        if (refs != null)
                        {
                            // Scale attack distances (used by Tank and Bouncer attack states)
                            refs.attackDistance *= scaleMultiplier;
                            refs.attackRange *= scaleMultiplier;
                            
                            // Scale damage values
                            refs.attackDamage *= scaleMultiplier;
                            refs.tankAttackDamage *= scaleMultiplier;
                            refs.knockbackForce *= scaleMultiplier;
                            refs.explosionDamage *= scaleMultiplier;
                            refs.fastSpellDamage *= scaleMultiplier;
                            refs.medSpellDamage *= scaleMultiplier;
                            refs.slowSpellDamage *= scaleMultiplier;
                            
                            // Scale health
                            var enemyHP = enemy.GetComponent<Enemy>();
                            if (enemyHP != null)
                            {
                                enemyHP.ScaleHealth(scaleMultiplier);
                            }
                            
                            // Scale NavMeshAgent stopping distance
                            var navAgent = enemy.GetComponent<UnityEngine.AI.NavMeshAgent>();
                            if (navAgent != null && navAgent.isOnNavMesh)
                            {
                                // If original stopping distance was 0 or very small, set it based on attack range
                                // Otherwise scale it proportionally
                                if (originalStoppingDistances.ContainsKey(enemy) && originalStoppingDistances[enemy] > 0.1f)
                                {
                                    navAgent.stoppingDistance = originalStoppingDistances[enemy] * scaleMultiplier;
                                }
                                else
                                {
                                    // Set stopping distance to 80% of attack range so enemy stops before reaching attack range
                                    // This accounts for the enlarged collider size
                                    float attackRange = Mathf.Max(refs.attackDistance, refs.attackRange);
                                    navAgent.stoppingDistance = attackRange * 0.6f;
                                }
                            }
                            
                            float stoppingDist = navAgent != null ? navAgent.stoppingDistance : -1f;
                            Debug.Log($"<color=green>[Telekinesis] Scaled {enemy.name} by {scaleMultiplier:F2}x | attackDistance: {refs.attackDistance:F2}m | attackRange: {refs.attackRange:F2}m | stoppingDistance: {stoppingDist:F2}m</color>");
                        }
                    }
                    
                    // Fix rotation bug: reset enemy to face forward
                    // NavMeshAgent will take over rotation on next frame
                    var enemyRefs = enemy.GetComponent<EnemyRefrences>();
                    if (enemyRefs != null && enemyRefs.player != null)
                    {
                        Vector3 dirToPlayer = (enemyRefs.player.position - enemy.transform.position).normalized;
                        if (dirToPlayer.sqrMagnitude > 0.01f)
                        {
                            enemy.transform.rotation = Quaternion.LookRotation(dirToPlayer);
                        }
                    }
                }
            }
            
            chargeTimers.Clear();
            affectedEnemies.Clear();
            originalScales.Clear();
            originalStoppingDistances.Clear();
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
            
            // Skip enemies that have already been enlarged once
            var enemyScript = col.GetComponent<Enemy>();
            if (enemyScript != null && enemyScript.hasBeenEnlarged) continue;

            Rigidbody rb = col.GetComponent<Rigidbody>();
            if (rb == null) continue;
            
            // Track this enemy as affected
            affectedEnemies.Add(enemy);
            
            // Allow this enemy to be controlled by external velocity (disable auto-reset)
            if (enemyScript != null)
            {
                enemyScript.allowExternalVelocity = true;
            }
            
            // Store original scale and stopping distance on first contact
            if (!originalScales.ContainsKey(enemy))
            {
                originalScales[enemy] = enemy.transform.localScale;
                
                // Store original NavMeshAgent stopping distance
                var agent = enemy.GetComponent<UnityEngine.AI.NavMeshAgent>();
                if (agent != null && agent.isOnNavMesh)
                {
                    originalStoppingDistances[enemy] = agent.stoppingDistance;
                }
            }
            
            // Calculate distance to indicator
            float distanceToIndicator = Vector3.Distance(col.transform.position, _indicatorInst.transform.position);
            
            // Only apply pull force if far enough away
            if (distanceToIndicator > stopDistance)
            {
                Vector3 dir = (_indicatorInst.transform.position - col.transform.position).normalized;
                rb.AddForce(dir * pullForce, ForceMode.Acceleration);
            }
            else
            {
                // Enemy is close - actively dampen velocity to prevent sliding
                rb.linearVelocity = Vector3.Lerp(rb.linearVelocity, Vector3.zero, Time.deltaTime * 10f);
            }

            // Start or continue charging
            if (!chargeTimers.ContainsKey(enemy))
                chargeTimers[enemy] = 0f;

            chargeTimers[enemy] += Time.deltaTime;

            // Scale up based on time held (from current scale, not from 1)
            float t = Mathf.Clamp01(chargeTimers[enemy] / overloadTime);
            Vector3 startScale = originalScales.ContainsKey(enemy) ? originalScales[enemy] : Vector3.one;
            float targetScale = startScale.x * maxScale; // Scale from original, not from 1
            float currentScale = Mathf.Lerp(startScale.x, targetScale, t);
            enemy.transform.localScale = Vector3.one * currentScale;
            
            // Scale NavMeshAgent stopping distance proportionally
            var navAgent = enemy.GetComponent<UnityEngine.AI.NavMeshAgent>();
            if (navAgent != null && navAgent.isOnNavMesh && originalStoppingDistances.ContainsKey(enemy))
            {
                float scaleMultiplier = currentScale / startScale.x;
                navAgent.stoppingDistance = originalStoppingDistances[enemy] * scaleMultiplier;
            }

            // Overload check
            if (chargeTimers[enemy] >= overloadTime)
            {
                ExplodeEnemy(enemy, col);
            }
        }
    }

   private void ExplodeEnemy(EnemyController enemy, Collider col)
    {
        Vector3 explosionPos = enemy.transform.position;
        float enemyScale = enemy.transform.localScale.x;
        float scaledRadius = explosionRadius * enemyScale; // Larger enemies = bigger explosion
        
        // Spawn VFX
        if (explosionEffect != null)
            Instantiate(explosionEffect, explosionPos, Quaternion.identity);
        
        // Camera shake
        if (CameraShake.Instance != null)
        {
            CameraShake.Instance.Shake(cameraShakeDuration, cameraShakeMagnitude * enemyScale);
        }
        
        // Check if player is in range and damage them directly
        var enemyRefs = enemy.GetComponent<EnemyRefrences>();
        if (damagePlayer && enemyRefs != null && enemyRefs.player != null)
        {
            float playerDistance = Vector3.Distance(explosionPos, enemyRefs.player.position);
            if (playerDistance <= scaledRadius)
            {
                float distancePercent = playerDistance / scaledRadius;
                float damageFalloff = Mathf.Lerp(1f, 0.25f, distancePercent);
                float playerDamage = explosionDamage * damageFalloff * enemyScale;
                
                PlayerData.takeDamage(playerDamage);
                Debug.Log($"<color=red>[Explosion] Hit PLAYER for {playerDamage:F0} damage (distance: {playerDistance:F1}m, falloff: {damageFalloff:F2})</color>");
            }
        }
        
        // Find all entities in explosion radius
        Collider[] hitColliders = Physics.OverlapSphere(explosionPos, scaledRadius);
        int enemiesHit = 0;
        
        foreach (var hitCol in hitColliders)
        {
            float distance = Vector3.Distance(explosionPos, hitCol.transform.position);
            
            // Calculate damage with falloff (full damage at center, 25% at edge)
            float distancePercent = distance / scaledRadius;
            float damageFalloff = Mathf.Lerp(1f, 0.25f, distancePercent);
            float finalDamage = explosionDamage * damageFalloff * enemyScale; // Scale damage with enemy size
            
            // Damage other enemies
            var enemyComponent = hitCol.GetComponent<Enemy>();
            if (enemyComponent != null && hitCol.gameObject != enemy.gameObject)
            {
                enemyComponent.TakeDamage(finalDamage);
                enemiesHit++;
                Debug.Log($"<color=orange>[Explosion] Hit {hitCol.name} for {finalDamage:F0} damage (distance: {distance:F1}m)</color>");
            }
        }
        
        // Log the explosion
        float currentHealth = col.GetComponent<Enemy>().CurrentHealth;
        float dist = _indicatorInst != null ? Vector3.Distance(_indicatorInst.transform.position, explosionPos) : 0f;
        
        ActionLogger.Instance?.LogAction(
            actor:     "Player",
            actionType:"Player_Telekinesis",   
            target:     $"{enemy.gameObject.name} (Explosion)", 
            isHit:      true,
            damage:     currentHealth + (enemiesHit * explosionDamage * 0.5f), // Approximate total damage
            distance:   dist
        );
        
        Debug.Log($"<color=yellow>[Explosion] Radius: {scaledRadius:F1}m | Enemies Hit: {enemiesHit}</color>");
        
        // Destroy the exploding enemy
        Destroy(enemy.gameObject);
        chargeTimers.Remove(enemy);
    }
}

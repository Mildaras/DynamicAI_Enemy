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

    [Header("Right Click - Object Throw")]
    [SerializeField] private GameObject throwIndicatorPrefab;
    [SerializeField] private float pickupRange = 10f;
    [SerializeField] private float leftClickMaxAimDistance = 8f;
    [SerializeField] private float grabDistance = 1.5f;
    [SerializeField] private float objectPullForce = 5f;
    [SerializeField] private float launchForce = 50f;
    [SerializeField] private float objectDamage = 75f;
    [SerializeField] private float objectHoverHeight = 1.5f;
    [SerializeField] private float knockbackForce = 15f;
    [SerializeField] private LayerMask movableLayer;
    [SerializeField] private GameObject shatterEffect;

    private GameObject _throwIndicatorInst;
    private Rigidbody _targetObjectRb;
    private Renderer _targetObjectRenderer;
    private bool _isObjectGrabbed;

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

        // Right Click - Object Throw
        if (Input.GetMouseButtonDown(1) && throwIndicatorPrefab != null)
        {
            _throwIndicatorInst = Instantiate(throwIndicatorPrefab);
        }

        if (Input.GetMouseButton(1) && _throwIndicatorInst != null)
        {
            UpdateLeftIndicatorPosition();
            if (!_isObjectGrabbed)
                PullNearestMovable();
            else
                HoldGrabbedObject();
        }

        if (Input.GetMouseButtonUp(1))
        {
            if (_isObjectGrabbed && _targetObjectRb != null)
                LaunchObject();
            else if (_targetObjectRb != null)
            {
                SetObjectGlow(_targetObjectRenderer, false);
                IgnoreEnemyCollisions(_targetObjectRb.gameObject, false);
            }

            if (_throwIndicatorInst != null)
            {
                Destroy(_throwIndicatorInst);
                _throwIndicatorInst = null;
            }

            _targetObjectRb = null;
            _targetObjectRenderer = null;
            _isObjectGrabbed = false;
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

    private void UpdateLeftIndicatorPosition()
    {
        Ray ray = cam.ScreenPointToRay(Input.mousePosition);
        Vector3 point;

        // Temporarily disable grabbed object's collider so raycast doesn't hit it
        Collider grabbedCol = _isObjectGrabbed && _targetObjectRb != null
            ? _targetObjectRb.GetComponent<Collider>() : null;
        if (grabbedCol != null) grabbedCol.enabled = false;

        if (Physics.Raycast(ray, out RaycastHit hit, maxAimDistance))
            point = hit.point;
        else
            point = ray.origin + ray.direction * maxAimDistance;

        if (grabbedCol != null) grabbedCol.enabled = true;

        // Clamp distance from player so indicator stays closer than left-click one
        Vector3 playerPos = transform.position;
        Vector3 toPoint = point - playerPos;
        if (toPoint.magnitude > leftClickMaxAimDistance)
            point = playerPos + toPoint.normalized * leftClickMaxAimDistance;

        _throwIndicatorInst.transform.position = point + Vector3.up * indicatorYOffset;
    }

    private void PullNearestMovable()
    {
        Collider[] colliders = Physics.OverlapSphere(_throwIndicatorInst.transform.position, pickupRange, movableLayer);
        float closestDist = float.MaxValue;
        Rigidbody closestRb = null;

        foreach (var col in colliders)
        {
            Rigidbody rb = col.GetComponent<Rigidbody>();
            if (rb == null) continue;

            float dist = Vector3.Distance(col.transform.position, _throwIndicatorInst.transform.position);
            if (dist < closestDist)
            {
                closestDist = dist;
                closestRb = rb;
            }
        }

        if (closestRb == null)
        {
            if (_targetObjectRenderer != null)
                SetObjectGlow(_targetObjectRenderer, false);
            _targetObjectRb = null;
            _targetObjectRenderer = null;
            return;
        }

        // Switched to a different object - clear glow and re-enable collisions on the old one
        if (_targetObjectRb != null && _targetObjectRb != closestRb)
        {
            SetObjectGlow(_targetObjectRenderer, false);
            IgnoreEnemyCollisions(_targetObjectRb.gameObject, false);
        }

        // First time targeting this object - disable enemy collisions immediately
        if (_targetObjectRb != closestRb)
            IgnoreEnemyCollisions(closestRb.gameObject, true);

        _targetObjectRb = closestRb;
        _targetObjectRenderer = closestRb.GetComponent<Renderer>();

        // Red glow while not yet grabbed
        SetObjectGlow(_targetObjectRenderer, true);

        // Pull toward indicator
        Vector3 dir = (_throwIndicatorInst.transform.position - closestRb.transform.position).normalized;
        closestRb.AddForce(dir * objectPullForce, ForceMode.Acceleration);

        // Close enough to grab
        if (closestDist <= grabDistance)
        {
            var existingProj = closestRb.GetComponent<TelekinesisProjectile>();
            if (existingProj != null)
                Destroy(existingProj);

            SetObjectGlow(_targetObjectRenderer, false);

            _targetObjectRb.useGravity = false;
            _targetObjectRb.linearVelocity = Vector3.zero;
            _targetObjectRb.angularVelocity = Vector3.zero;
            _targetObjectRb.isKinematic = true;
            _isObjectGrabbed = true;
        }
    }

    private void HoldGrabbedObject()
    {
        if (_targetObjectRb == null)
        {
            _isObjectGrabbed = false;
            return;
        }

        _targetObjectRb.transform.position = _throwIndicatorInst.transform.position;
    }

    private void LaunchObject()
    {
        // Re-enable enemy collisions for the throw
        IgnoreEnemyCollisions(_targetObjectRb.gameObject, false);

        _targetObjectRb.isKinematic = false;
        _targetObjectRb.useGravity = true;

        Ray ray = cam.ScreenPointToRay(Input.mousePosition);
        _targetObjectRb.AddForce(ray.direction * launchForce, ForceMode.Impulse);

        var proj = _targetObjectRb.gameObject.AddComponent<TelekinesisProjectile>();
        proj.Initialize(objectDamage, knockbackForce, shatterEffect);

        ActionLogger.Instance?.LogAction(
            actor:     "Player",
            actionType:"Player_Telekinesis_Throw",
            target:     _targetObjectRb.gameObject.name,
            isHit:      false,
            damage:     0f,
            distance:   0f
        );
    }

    private void IgnoreEnemyCollisions(GameObject obj, bool ignore)
    {
        Collider objCol = obj.GetComponent<Collider>();
        if (objCol == null) return;

        foreach (var enemyObj in GameObject.FindGameObjectsWithTag("Enemy"))
        {
            Collider enemyCol = enemyObj.GetComponent<Collider>();
            if (enemyCol != null)
                Physics.IgnoreCollision(objCol, enemyCol, ignore);
        }
    }

    private void SetObjectGlow(Renderer renderer, bool enabled)
    {
        if (renderer == null) return;
        MaterialPropertyBlock block = new MaterialPropertyBlock();
        if (enabled)
        {
            renderer.GetPropertyBlock(block);
            block.SetColor("_EmissionColor", Color.red * 2f);
            block.SetColor("_BaseColor", new Color(1f, 0.3f, 0.3f, 1f));
            block.SetColor("_Color", new Color(1f, 0.3f, 0.3f, 1f));
        }
        renderer.SetPropertyBlock(block);
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

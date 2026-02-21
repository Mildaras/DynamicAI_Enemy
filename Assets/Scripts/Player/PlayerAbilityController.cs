using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerAbilityController : MonoBehaviour
{
    [Header("Blink")]
    public KeyCode blinkKey = KeyCode.F;
    public float blinkDistance = 6f;
    public float blinkCooldown = 10f;
    private float lastBlinkTime = -999f;

    [Header("Stun Pulse")]
    public KeyCode stunKey      = KeyCode.G;
    public float   stunRadius   = 5f;
    public float   stunDuration = 5f;
    public float   stunCooldown = 5f;             
    private float  _lastStunTime = -Mathf.Infinity; 
    public LayerMask enemyMask;
    public GameObject stunVFX;

    [Header("Reflect Spell")]
    public KeyCode reflectKey      = KeyCode.R;
    public float   reflectDuration = 3f;
    public float   reflectCooldown = 10f;
    public LayerMask projectileLayer;       // set to the layer you use for enemy projectiles
    public GameObject reflectVFX;          // optional visual effect on player
    private float _lastReflectTime = -Mathf.Infinity; 

    private bool  _isReflecting = false;
    private float _reflectEndTime;

     [Header("Smite")]
    public KeyCode smiteKey      = KeyCode.H;
    public float   smiteRadius   = 8f;
    public float   smiteDamage   = 50f;
    public float   smiteCooldown = 8f;
    public GameObject smiteVFX;              // optional particle effect prefab
    private float _lastSmiteTime = -Mathf.Infinity;

    public CooldownDisplay cooldownUI;

    
    [SerializeField] private Camera cam;


    void Start()
    {
        if (cam == null)
            cam = Camera.main;

    }

    void Update()
    {
        if (Input.GetKeyDown(smiteKey)
            && PlayerData.hasSmite
            && Time.time >= _lastSmiteTime + smiteCooldown)
        {
            SmiteNearbyEnemies();
            cooldownUI.TriggerCooldown(smiteKey);
            _lastSmiteTime = Time.time;
        }

        //Blink spell
        if (Input.GetKeyDown(blinkKey) && PlayerData.hasBlink && Time.time >= lastBlinkTime + blinkCooldown)
        {
            Blink();
            cooldownUI.TriggerCooldown(blinkKey);
            lastBlinkTime = Time.time;  // Update cooldown here too
        }

        //Stun spell
        if (Input.GetKeyDown(stunKey)
            && Time.time >= (_lastStunTime + stunCooldown) && PlayerData.hasStunPulse)
        {
            StunNearbyEnemies();
            cooldownUI.TriggerCooldown(stunKey);
            _lastStunTime = Time.time;  // reset cooldown
        }

        // Reflect spell
        if (Input.GetKeyDown(reflectKey)
            && PlayerData.hasReflect
            && Time.time >= _lastReflectTime + reflectCooldown)  // ← guard
        {
            StartReflect();
            _lastReflectTime = Time.time;                        // ← reset cooldown
        }

        if (_isReflecting && Time.time >= _reflectEndTime)
        {
            _isReflecting = false;

            // destroy the VFX now that reflect ended
            if (reflectVFX != null)
            {
                Destroy(reflectVFX);
                reflectVFX = null;
            }

            Debug.Log("Reflect ended");
        }
    }

    private void SmiteNearbyEnemies()
    {
        Vector3 origin = transform.position;

        // find all enemies in range
        Collider[] hits = Physics.OverlapSphere(origin, smiteRadius, enemyMask);
        int count = 0;

        foreach (var col in hits)
        {
            if (col.CompareTag("Enemy") && col.TryGetComponent<IDamageable>(out var dmg))
            {
                // 1) Spawn the VFX above each enemy
                if (smiteVFX != null)
                {
                    Vector3 vfxPos = col.transform.position + Vector3.up * 5f; // 2 units above enemy
                    Quaternion downRot = Quaternion.LookRotation(Vector3.down);
                    var vfxInst = Instantiate(smiteVFX, vfxPos, downRot);

                    Destroy(vfxInst, 1f);
                }

                // 2) Log & apply damage
                float dist = Vector3.Distance(origin, col.transform.position);

                ActionLogger.Instance?.LogAction(
                    actor:     "Player",
                    actionType:"Player_Smite",   
                    target:     col.gameObject.name, 
                    isHit:      true,
                    damage:     smiteDamage,
                    distance:   dist
                );
                dmg.TakeDamage(smiteDamage);
                count++;
            }
            else
            {
                // log a miss
                ActionLogger.Instance?.LogAction(
                    actor:     "Player",
                    actionType:"Player_Smite",   
                    target:     col.gameObject.name, 
                    isHit:      false,
                    damage:     0f,
                    distance:   Vector3.Distance(origin, col.transform.position)
                );
            }
        }

        Debug.Log($"Smite hit {count} enemies for {smiteDamage} damage (radius {smiteRadius})");
    }

    private void StartReflect()
    {
        _isReflecting   = true;
        _reflectEndTime = Time.time + reflectDuration;

        // if there's an old VFX hanging around, clear it first
        if (reflectVFX != null)
            Destroy(reflectVFX);

        // spawn & keep a reference
        if (reflectVFX != null)
            reflectVFX = Instantiate(
                reflectVFX,
                transform.position,
                Quaternion.identity,
                transform
            );

        cooldownUI.TriggerCooldown(reflectKey);
        
        // Log reflect activation
        ActionLogger.Instance?.LogActionWithContext(
            actor: "Player",
            actionType: "UseAbility_Reflect",
            target: "Self",
            isHit: true,
            damage: 0f,
            distance: 0f,
            actorHealthPercent: PlayerData.playerHealth / 100f,
            targetHealthPercent: -1f,
            actorState: "Defensive",
            wasSuccessful: true
        );

        Debug.Log($"Reflect ON for {reflectDuration:F1}s (cooldown {reflectCooldown}s)");
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!_isReflecting) return;

        // only pick up real enemy-fired spells
        var incoming = other.GetComponent<SpellProjectile>();
        if (incoming == null || incoming.IsReflected) 
            return;

        // layer‐mask check
        if (((1 << other.gameObject.layer) & projectileLayer.value) == 0)
            return;

        // find where to send it
        Transform nearest = FindNearestEnemy(other.transform.position);

        if (nearest == null) 
            return;

        // destroy the original
        Vector3 spawnPos = other.transform.position;
        Destroy(other.gameObject);

       
        // spawn a new SpellProjectile clone
        var clone = Instantiate(incoming.gameObject, spawnPos, Quaternion.identity);
        var spell = clone.GetComponent<SpellProjectile>();
        var groundEffect = clone.GetComponent<ImpactOnGround>();
        spell.enabled = true;
        groundEffect.enabled = true; // enable the ground effect script

        Vector3 targetPos = nearest.position;
        targetPos.y = 2f; // make sure it’s on the ground

        spell.MarkReflected();
        spell.Initialize(
            targetPos,      // Vector3 target
            incoming.GetSpeed(),   // speed
            incoming.GetDamage(),  // damage
            incoming.GetLifeTime() // lifetime
        );

    }


    private Transform FindNearestEnemy(Vector3 from)
    {
        // Get all colliders on the enemyMask layer within 20 units
        Collider[] hits = Physics.OverlapSphere(from, 20f, enemyMask);
        Transform best = null;
        float   closest = float.MaxValue;

        foreach (var c in hits)
        {
            // Only consider objects actually tagged "Enemy"
            if (!c.CompareTag("Enemy"))
                continue;

            float dist = Vector3.Distance(from, c.transform.position);
            if (dist < closest)
            {
                closest = dist;
                best    = c.transform;
            }
        }
        return best;
    }


    void Blink()
    {
        Vector3 dir = transform.forward;
        Vector3 target = transform.position + dir * blinkDistance;

        bool success = false;
        if (!Physics.Raycast(transform.position, dir, blinkDistance))
        {
            transform.position = target;
            success = true;
            Debug.Log("Blink successful.");
        }
        else
        {
            Debug.Log("Blink blocked.");
        }
        
        // Log blink usage
        ActionLogger.Instance?.LogActionWithContext(
            actor: "Player",
            actionType: "UseAbility_Blink",
            target: "Self",
            isHit: success,
            damage: 0f,
            distance: success ? blinkDistance : 0f,
            actorHealthPercent: PlayerData.playerHealth / 100f,
            targetHealthPercent: -1f,
            actorState: "Mobile",
            wasSuccessful: success
        );
    }

    private void StunNearbyEnemies()
    {
        Vector3 origin = transform.position;
        // show a VFX at your feet, if you have one
        if (stunVFX != null)
            Instantiate(stunVFX, origin, Quaternion.identity);

        Collider[] hits = Physics.OverlapSphere(origin, stunRadius, enemyMask);
        int stunCount = 0;
        foreach (var col in hits)
        {
            if (col.TryGetComponent<EnemyController>(out var enemy))
            {
                enemy.Stun(stunDuration);
                stunCount++;
                
                // Log each stun
                ActionLogger.Instance?.LogActionWithContext(
                    actor: "Player",
                    actionType: "UseAbility_Stun",
                    target: col.gameObject.name,
                    isHit: true,
                    damage: 0f,
                    distance: Vector3.Distance(origin, col.transform.position),
                    actorHealthPercent: PlayerData.playerHealth / 100f,
                    targetHealthPercent: -1f,
                    actorState: "Offensive",
                    wasSuccessful: true
                );
            }
        }

        Debug.Log($"Stunned {stunCount} enemies for {stunDuration}s");
    }

}
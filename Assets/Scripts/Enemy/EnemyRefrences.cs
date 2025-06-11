using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using Unity;

public class EnemyRefrences : MonoBehaviour
{

    [Header("Enemy Components")]
    [HideInInspector] public NavMeshAgent agent;
    public Transform player;
    [HideInInspector] public Animator animator;
    public Rigidbody playerRigidbody; // for physics
    public WeaponIKHandler ikHandler; // for IK
    public Transform    weaponHolder;
    public GameObject swordPrefab;

    [Header("Spell Settings")]
    public Transform  castPoint;      // A child transform (e.g. at hand tip)
    public GameObject healAuraPrefab; // assign your heal aura prefab
    
    [Header("Fast Spell Settings")]
    public GameObject fastChargePrefab;
    public GameObject fastSpellPrefab;
    public float      fastSpellSpeed;
    public float      fastSpellDamage;
    public float      fastSpellLifeTime;
    public float      fastCastTime = 1.0f;   // ← fast is quick

    [Header("Medium Spell Settings")]
    public GameObject medChargePrefab;
    public GameObject medSpellPrefab;
    public float      medSpellSpeed;
    public float      medSpellDamage;
    public float      medSpellLifeTime;
    public float      medCastTime  = 3.0f;   // ← medium takes longer

    [Header("Slow Spell Settings")]
    public GameObject slowChargePrefab;
    public GameObject slowSpellPrefab;
    public float      slowSpellSpeed;
    public float      slowSpellDamage;
    public float      slowSpellLifeTime;
    public float      slowCastTime = 5.0f;   // ← slow is the longest

    [Header("Explosion Settings")]
    public GameObject explosionPrefab;    // assign your explosion VFX prefab
    public float      explosionRange = 2f;
    public float      explosionDamage = 100f;
    public float      explosionLifeSpan = 5f;
    [Header("Exploder Spawn Settings")]
    public GameObject exploderPrefab;
    public int        exploderCount;
    public float      exploderRadius;
    public GameObject exploderLaunchProjectile;
    public float      launchTravelTimeExp = 0.5f;
    public float      launchStartScaleExp  = 0.1f;
    [Header("Bouncer Settings")]
    public Transform master;          // assign main enemy transform
    public float     guardTime = 20f;  // seconds to guard before dying
    public float     guardDistance = 2f; // distance to player before guarding
    public float     attackRange = 3f;
    public float     attackDamage = 5f;
    public float     knockbackForce = 10f;
    [Header("Bouncer Spawn Settings")]
    public GameObject bouncerPrefab;   // your Bouncer prefab
    public int        bouncerCount = 3;
    public float      bouncerRadius = 5f;
    public GameObject bouncerLaunchProjectile;
    public float launchTravelTime = 0.5f;
    public float launchStartScale = 0.1f;

    [Header("Tank Settings")]
    public float detectDistance = 10f;
    public float attackDistance = 2f;
    public float tankAttackDamage = 20f;
    public float attackCooldown = 1f;

    [Header("Tank Spawner Settings")]
    public GameObject tankPrefab;   // your Tank enemy prefab
    public int        spawnCount  = 3;
    public float      spawnRadius = 5f;

    [Header("Spawn VFX")]
    public GameObject spawnShatterEffectPrefab;

    [Header("Health Bar")]
    public GameObject floatingHealthBarPrefab; 

    [Header("Wall Settings")]

    public GameObject wallPrefab;
    public float wallSummonDistance = 3f;
    public float wallRiseDistance   = 1f;
    public float wallRiseDuration   = 0.8f;
    public float wallLifetime       = 7f;

    [Header("Follow Settings")]
    [Tooltip("How close the enemy tries to stay to the player.  AI can change this at runtime.")]
    public float followDistance = 10f;



    void Awake()
    {

        if (master == null)
        {
            var go = GameObject.FindWithTag("Enemy");
            if (go != null)
                master = go.transform;
            else
                Debug.LogWarning($"[{name}] EnemyRefrences.master is null and no object tagged 'Master' found.");
        }

        // auto-find the player by tag if none assigned
        if (player == null)
        {
            var go = GameObject.FindWithTag("Player");
            if (go != null)
                player = go.transform;
            else
                Debug.LogError("[EnemyRefrences] No Player tagged object found!");
        }

        if (playerRigidbody == null && player != null)
            playerRigidbody = player.GetComponent<Rigidbody>();

        // auto-get required components if missing
        if (agent == null)
            agent = GetComponent<NavMeshAgent>();
        if (animator == null)
            animator = GetComponent<Animator>();
        if (ikHandler == null)
            ikHandler = GetComponent<WeaponIKHandler>();
    }
}
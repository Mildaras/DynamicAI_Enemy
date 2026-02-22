// Enemy.cs
using UnityEngine;
using System;
using UnityEngine.AI;

[RequireComponent(typeof(Collider))]
public class Enemy : MonoBehaviour, IDamageable
{
    [Header("Stats")]
    [SerializeField] public float maxHealth = 1000f;
    public float CurrentHealth { get; private set; }
    public bool IsDead => CurrentHealth <= 0f;
    public event Action<float> OnHealthChanged;

    [Header("Health Bar (optional)")]
    [SerializeField] private bool showHealthBar = false;
    [SerializeField] private HealthBar healthBarPrefab = null;

    private HealthBar healthBarInstance;
    [SerializeField] private Animator animator;
    public event Action OnDeath;
    [SerializeField] private float deathDelay = 2f;

    private Rigidbody rb;
    
    /// <summary>
    /// Set this to true to allow external velocity control (e.g., telekinesis weapon).
    /// When true, Enemy.cs won't reset velocity in FixedUpdate.
    /// </summary>
    [HideInInspector] public bool allowExternalVelocity = false;
    
    /// <summary>
    /// Tracks if this enemy has been affected by telekinesis.
    /// Once true, telekinesis can no longer affect this enemy.
    /// </summary>
    [HideInInspector] public bool hasBeenEnlarged = false;
    
    void Awake()
    {
        CurrentHealth = maxHealth;
        animator = GetComponent<Animator>();
        rb = GetComponent<Rigidbody>();

        if (showHealthBar && healthBarPrefab != null)
        {
            SpawnHealthBar();
        }
        
        // Prevent player from pushing this enemy
        IgnorePlayerCollision();
    }
    
    void FixedUpdate()
    {
        // Reset any unwanted velocity to prevent enemies from sliding
        // NavMeshAgent handles movement, so Rigidbody velocity should always be zero
        // UNLESS external systems (like telekinesis) are controlling velocity
        if (rb != null && !IsDead && !allowExternalVelocity)
        {
            // Reset horizontal velocity (allow gravity on Y axis)
            rb.linearVelocity = new Vector3(0f, rb.linearVelocity.y, 0f);
            rb.angularVelocity = Vector3.zero;
        }
    }

    private void SpawnHealthBar()
    {
        // instantiate once
        healthBarInstance = Instantiate(
            healthBarPrefab,
            transform.position + Vector3.up * 2f,  // adjust in prefab too
            Quaternion.identity
        );
        healthBarInstance.Initialize(transform, CurrentHealth / maxHealth);
        // subscribe so the bar updates automatically
        OnHealthChanged += healthBarInstance.SetHealth;
    }

    /// <summary>
    /// Scales max health (and current health proportionally) by the given multiplier.
    /// Called by TelekinesisController when an enemy is enlarged.
    /// </summary>
    public void ScaleHealth(float multiplier)
    {
        float healthPercent = CurrentHealth / maxHealth;
        maxHealth *= multiplier;
        CurrentHealth = maxHealth * healthPercent;
        OnHealthChanged?.Invoke(CurrentHealth / maxHealth);
    }

    public void Heal(float amount)
    {
        if (IsDead) return;
        CurrentHealth = Mathf.Min(CurrentHealth + amount, maxHealth);
        OnHealthChanged?.Invoke(CurrentHealth / maxHealth);
    }

    public void TakeDamage(float amount)
    {
        if (IsDead) return;

        CurrentHealth = Mathf.Max(CurrentHealth - amount, 0f);
        OnHealthChanged?.Invoke(CurrentHealth / maxHealth);

        if (CurrentHealth <= 0f)
        {
            animator?.SetTrigger("die");
            Die();
        }
        else
        {
            animator?.SetTrigger("hit");
        }
    }

    private void Die()
    {
        OnDeath?.Invoke();
        // stop navigation & collisions
        var col = GetComponent<Collider>();
        if (col) col.enabled = false;
        var agent = GetComponent<NavMeshAgent>();
        if (agent) { agent.isStopped = true; agent.enabled = false; }

        // cleanup health bar subscription + object
        if (healthBarInstance)
        {
            OnHealthChanged -= healthBarInstance.SetHealth;
            Destroy(healthBarInstance.gameObject);
        }

        Destroy(gameObject, deathDelay);
    }
    
    /// <summary>
    /// Disable physics collision with player to prevent pushing.
    /// </summary>
    private void IgnorePlayerCollision()
    {
        var player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            var playerMovement = player.GetComponent<PlayerMovement>();
            if (playerMovement != null)
            {
                Collider enemyCollider = GetComponent<Collider>();
                if (enemyCollider != null)
                {
                    playerMovement.IgnoreEnemyCollision(enemyCollider);
                }
            }
        }
    }
}

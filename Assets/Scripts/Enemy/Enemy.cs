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

    void Awake()
    {
        CurrentHealth = maxHealth;
        animator = GetComponent<Animator>();

        if (showHealthBar && healthBarPrefab != null)
        {
            SpawnHealthBar();
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
}

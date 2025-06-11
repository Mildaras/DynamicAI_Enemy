using UnityEngine;
public interface IDamageable
{
    /// <summary>
    /// Called when this object takes damage.
    /// </summary>
    /// <param name="amount">How much damage to apply.</param>
    void TakeDamage(float amount);

    /// <summary>
    /// True once health ≤ 0.
    /// </summary>
    bool IsDead { get; }
}

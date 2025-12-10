using UnityEngine;

public interface IDamageable
{
    float CurrentHealth { get; }
    void TakeDamage(float damageAmount, Vector3 knockbackSource);
}
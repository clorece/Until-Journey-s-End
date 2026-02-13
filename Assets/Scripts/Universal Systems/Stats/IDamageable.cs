using UnityEngine;

public interface IDamageable
{
    float CurrentHealth { get; }
    bool TakeDamage(float damageAmount, Vector3 knockbackSource);
}
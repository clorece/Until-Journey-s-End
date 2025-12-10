using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(EntityStats))]
public class CombatSystem : MonoBehaviour
{
    [Header("Targeting Settings")]
    public LayerMask targetLayers; 
    public Transform attackPoint;

    [Header("Debug Visualization")]
    public bool showGizmos = true;
    public AttackType debugType = AttackType.Cone;
    public float debugRange = 5f;
    public float debugAngle = 90f;     
    public float debugWidth = 2f;      
    
    public enum AttackType { Cone, Line, Radial }

    private EntityStats myStats;

    void Start()
    {
        myStats = GetComponent<EntityStats>();
        if (attackPoint == null)
            Debug.LogError("CombatSystem: ATTACK POINT IS MISSING! Please assign it in the Inspector.");
    }

    // --- CONE ATTACK (Omni-Directional) ---
    public void PerformConeAttack(float range, float angle, float knockbackForce, StatType damageType)
    {
        Vector3 origin = attackPoint.position;
        
        // REVERTED: Use the exact aim direction (Mouse Position)
        // We do NOT snap to Left/Right anymore.
        Vector3 forwardDir = attackPoint.forward; 

        Collider[] hits = Physics.OverlapSphere(origin, range, targetLayers);

        foreach (Collider hit in hits)
        {
            if (hit.gameObject == gameObject) continue; 

            Vector3 directionToTarget = (hit.transform.position - origin).normalized;
            
            // Flatten Y so height differences don't break aiming
            // This ensures you can hit enemies on slopes
            Vector3 flatTargetDir = directionToTarget;
            flatTargetDir.y = 0;
            flatTargetDir.Normalize();

            Vector3 flatForward = forwardDir;
            flatForward.y = 0;
            flatForward.Normalize();

            // Precise Angle Check
            if (Vector3.Angle(flatForward, flatTargetDir) < angle / 2)
            {
                ApplyDamage(hit.gameObject, forwardDir, knockbackForce, damageType);
            }
        }
    }

    // --- LINE ATTACK (Omni-Directional) ---
    public void PerformLineAttack(float length, float width, float knockbackForce, StatType damageType)
    {
        Vector3 origin = attackPoint.position;
        
        // Use exact aim direction
        Vector3 forwardDir = attackPoint.forward; 

        // Calculate center of box based on aim
        Vector3 center = origin + (forwardDir * (length / 2));
        Vector3 halfExtents = new Vector3(width / 2, 2f, length / 2);
        
        // Use exact aim rotation for the box orientation
        Quaternion orientation = attackPoint.rotation; 

        Collider[] hits = Physics.OverlapBox(center, halfExtents, orientation, targetLayers);

        foreach (Collider hit in hits)
        {
            if (hit.gameObject == gameObject) continue;
            ApplyDamage(hit.gameObject, forwardDir, knockbackForce, damageType);
        }
    }

    // --- RADIAL ATTACK (Omni-Directional / AoE) ---
    public void PerformRadialAttack(Vector3 targetPosition, float radius, float knockbackForce, StatType damageType)
    {
        Collider[] hits = Physics.OverlapSphere(targetPosition, radius, targetLayers);

        foreach (Collider hit in hits)
        {
            if (hit.gameObject == gameObject) continue;
            
            // Knockback is radial (away from center)
            Vector3 knockbackDir = (hit.transform.position - targetPosition).normalized;
            ApplyDamage(hit.gameObject, knockbackDir, knockbackForce, damageType);
        }
    }

    private void ApplyDamage(GameObject target, Vector3 knockbackDir, float force, StatType damageType)
    {
        IDamageable damageable = target.GetComponent<IDamageable>();
        if (damageable != null)
        {
            float damageToDeal = myStats.CalculateOutgoingDamage(damageType);
            damageable.TakeDamage(damageToDeal, knockbackDir * force);
        }
    }

    // --- DEBUG GIZMOS ---
    void OnDrawGizmos()
    {
        if (!showGizmos || attackPoint == null) return;
        Gizmos.color = new Color(1f, 0f, 0f, 0.3f); 

        switch (debugType)
        {
            case AttackType.Cone:
                // Visualize the EXACT aim direction
                Vector3 forward = attackPoint.forward;
                
                Quaternion leftRayRotation = Quaternion.AngleAxis(-debugAngle / 2, Vector3.up);
                Vector3 leftRayDirection = leftRayRotation * forward;
                
                Quaternion rightRayRotation = Quaternion.AngleAxis(debugAngle / 2, Vector3.up);
                Vector3 rightRayDirection = rightRayRotation * forward;

                Vector3 origin = attackPoint.position;
                Gizmos.DrawLine(origin, origin + leftRayDirection * debugRange);
                Gizmos.DrawLine(origin, origin + rightRayDirection * debugRange);
                Gizmos.DrawLine(origin + leftRayDirection * debugRange, origin + forward * debugRange);
                Gizmos.DrawLine(origin + rightRayDirection * debugRange, origin + forward * debugRange);
                break;
                
            case AttackType.Line:
                Gizmos.matrix = attackPoint.localToWorldMatrix;
                Vector3 center = new Vector3(0, 0, debugRange / 2);
                Vector3 size = new Vector3(debugWidth, 1f, debugRange);
                Gizmos.DrawWireCube(center, size);
                break;
                
            case AttackType.Radial:
                Gizmos.DrawWireSphere(attackPoint.position, debugRange);
                break;
        }
    }
}
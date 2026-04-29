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
    
    public enum AttackType { Cone, Line, Radial, Projectile }

    private EntityStats myStats;

    void Start()
    {
        myStats = GetComponent<EntityStats>();
        if (attackPoint == null)
            Debug.LogError("CombatSystem: ATTACK POINT IS MISSING! Please assign it in the Inspector.");
    }

    public void PerformConeAttack(float range, float angle, float knockbackForce, StatType damageType)
    {
        Vector3 origin = attackPoint.position;

        Vector3 forwardDir = attackPoint.forward; 

        Collider[] hits = Physics.OverlapSphere(origin, range, targetLayers);

        foreach (Collider hit in hits)
        {
            if (hit.gameObject == gameObject) continue; 

            Vector3 directionToTarget = (hit.transform.position - origin).normalized;
            
            Vector3 flatTargetDir = directionToTarget;
            flatTargetDir.y = 0;
            flatTargetDir.Normalize();

            Vector3 flatForward = forwardDir;
            flatForward.y = 0;
            flatForward.Normalize();

            // Precise Angle Check
            if (Vector3.Angle(flatForward, flatTargetDir) < angle / 2)
            {
                ApplyDamage(hit.gameObject, directionToTarget, knockbackForce, damageType);
            }
        }
    }

    public void PerformLineAttack(float length, float width, float knockbackForce, StatType damageType)
    {
        Vector3 origin = attackPoint.position;

        Vector3 forwardDir = attackPoint.forward; 

        Vector3 center = origin + (forwardDir * (length / 2));
        Vector3 halfExtents = new Vector3(width / 2, 2f, length / 2);
        
        Quaternion orientation = attackPoint.rotation; 

        Collider[] hits = Physics.OverlapBox(center, halfExtents, orientation, targetLayers);

        foreach (Collider hit in hits)
        {
            if (hit.gameObject == gameObject) continue;
            Vector3 directionToTarget = (hit.transform.position - origin).normalized;
            ApplyDamage(hit.gameObject, directionToTarget, knockbackForce, damageType);
        }
    }

    public void PerformRadialAttack(Vector3 targetPosition, float radius, float knockbackForce, StatType damageType)
    {
        Collider[] hits = Physics.OverlapSphere(targetPosition, radius, targetLayers);

        foreach (Collider hit in hits)
        {
            if (hit.gameObject == gameObject) continue;
            
            Vector3 knockbackDir = (hit.transform.position - targetPosition).normalized;
            ApplyDamage(hit.gameObject, knockbackDir, knockbackForce, damageType);
        }
    }

    public event System.Action<GameObject> OnTargetKilled;

    private void ApplyDamage(GameObject target, Vector3 knockbackDir, float force, StatType damageType)
    {
        IDamageable damageable = target.GetComponentInParent<IDamageable>();
        if (damageable != null)
        {
            float damageToDeal = myStats.CalculateOutgoingDamage(damageType);
            bool killed = damageable.TakeDamage(damageToDeal, knockbackDir * force);
            
            if (killed)
            {
                Debug.Log($"[Combat] Kill detected: {target.name}");
                OnTargetKilled?.Invoke(target);
            }
        }
    }

    /// <summary>
    /// Spawns a projectile aimed at a target with predictive aiming. Universal — works for any entity.
    /// </summary>
    public void SpawnProjectile(GameObject prefab, Transform target, float speed, float knockbackForce, StatType damageType, bool penetrates)
    {
        if (prefab == null || attackPoint == null) return;

        GameObject projObj = Instantiate(prefab, attackPoint.position, Quaternion.identity);
        Projectile proj = projObj.GetComponent<Projectile>();

        if (proj == null)
        {
            Debug.LogError($"[Combat] Projectile prefab '{prefab.name}' is missing the Projectile component!");
            Destroy(projObj);
            return;
        }

        // Initialize projectile with combat data
        proj.damage = myStats.CalculateOutgoingDamage(damageType);
        proj.knockbackForce = knockbackForce;
        proj.penetrates = penetrates;
        proj.targetLayers = targetLayers;
        proj.owner = gameObject;

        // Predictive aiming: estimate where the target will be when the arrow arrives
        Vector3 targetPos;
        if (target != null)
        {
            targetPos = PredictTargetPosition(target, attackPoint.position, speed);
        }
        else
        {
            targetPos = attackPoint.position + attackPoint.forward * 10f;
        }

        proj.Launch(targetPos, speed);
    }

    /// <summary>
    /// Predicts where a target will be based on its current velocity and the projectile's travel time.
    /// Checks ORCAAgent (movement system) and Rigidbody for velocity data.
    /// </summary>
    public Vector3 PredictTargetPosition(Transform target, Vector3 firePos, float projectileSpeed)
    {
        Vector3 targetPos = target.position;
        Vector3 targetVelocity = Vector3.zero;

        // Check ORCAAgent first (primary movement system for all entities)
        var orcaAgent = target.GetComponent<Navigation.ORCA.ORCAAgent>();
        if (orcaAgent != null)
        {
            Vector2 vel2D = orcaAgent.currentVelocity;
            targetVelocity = new Vector3(vel2D.x, 0f, vel2D.y);
        }

        // Fallback: check Rigidbody velocity
        if (targetVelocity.sqrMagnitude < 0.01f)
        {
            Rigidbody targetRb = target.GetComponent<Rigidbody>();
            if (targetRb != null)
                targetVelocity = targetRb.linearVelocity;
        }

        // If target is stationary, aim directly
        if (targetVelocity.sqrMagnitude < 0.01f)
            return targetPos;

        // Iterative prediction: refine the aim point over 2 passes for better accuracy
        Vector3 predictedPos = targetPos;
        for (int i = 0; i < 2; i++)
        {
            Vector3 toTarget = predictedPos - firePos;
            float distance = new Vector3(toTarget.x, 0f, toTarget.z).magnitude;
            float timeToTarget = distance / Mathf.Max(projectileSpeed, 0.1f);

            predictedPos = targetPos + targetVelocity * timeToTarget;
        }

        return predictedPos;
    }

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

            case AttackType.Projectile:
                Gizmos.color = new Color(0f, 1f, 1f, 0.5f);
                Gizmos.DrawRay(attackPoint.position, attackPoint.forward * debugRange);
                Gizmos.DrawWireSphere(attackPoint.position, 0.3f);
                break;
        }
    }
}
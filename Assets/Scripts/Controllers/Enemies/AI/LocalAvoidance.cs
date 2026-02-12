using UnityEngine;

public class LocalAvoidance : MonoBehaviour
{
    [Header("Detection")]
    [Tooltip("Layer(s) that contain other entities to avoid.")]
    public LayerMask entityLayer;

    [Tooltip("Radius to scan for nearby entities.")]
    public float detectionRadius = 2.0f;

    [Header("Avoidance")]
    [Tooltip("Maximum strength of the avoidance push.")]
    public float avoidanceStrength = 5.0f;

    [Tooltip("Entities closer than this will be pushed apart at full strength.")]
    public float hardRadius = 0.8f;

    public Vector3 AvoidanceVector { get; private set; }

    private Collider[] neighbourBuffer = new Collider[16];

    void FixedUpdate()
    {
        ComputeAvoidance();
    }

    private void ComputeAvoidance()
    {
        Vector3 avoidance = Vector3.zero;

        int count = Physics.OverlapSphereNonAlloc(
            transform.position,
            detectionRadius,
            neighbourBuffer,
            entityLayer
        );

        int neighbourCount = 0;

        for (int i = 0; i < count; i++)
        {
            // skip ourselves
            if (neighbourBuffer[i].gameObject == gameObject) continue;

            Vector3 toSelf = transform.position - neighbourBuffer[i].transform.position;
            toSelf.y = 0f; // keep avoidance horizontal

            float dist = toSelf.magnitude;

            if (dist < 0.001f)
            {
                // nearly on top of each other – push in a random horizontal direction
                float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
                avoidance += new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
                neighbourCount++;
                continue;
            }

            // linear fallof of push strength between detection radius and the hard radius
            float t = 1f - Mathf.Clamp01((dist - hardRadius) / (detectionRadius - hardRadius));
            avoidance += toSelf.normalized * t;
            neighbourCount++;
        }

        if (neighbourCount > 0)
        {
            avoidance /= neighbourCount;

            // clamp to 0-1 range so EnemyMovement can scale by its own speed
            if (avoidance.magnitude > 1f)
                avoidance = avoidance.normalized;
        }

        AvoidanceVector = avoidance;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 1f, 0f, 0.25f);
        Gizmos.DrawWireSphere(transform.position, detectionRadius);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, hardRadius);

        if (Application.isPlaying)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(transform.position, transform.position + AvoidanceVector * 2f);
        }
    }
}

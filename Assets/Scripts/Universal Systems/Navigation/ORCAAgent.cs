using UnityEngine;
using System.Collections.Generic;

namespace Navigation.ORCA
{
    public class ORCAAgent : MonoBehaviour
    {
        [Header("Overrides (Set to 0 or Nothing to use Simulator Defaults)")]
        [Tooltip("If set to 0, uses Simulator's Default Radius.")]
        public float radiusOverride = 0f;
        [Tooltip("Usually set at runtime by movement script, but uses Simulator's Default Max Speed if 0.")]
        public float maxSpeedOverride = 0f;
        [Tooltip("If set to 'Nothing', uses Simulator's Default Avoidance Mask.")]
        public LayerMask avoidanceMaskOverride;

        [Header("Logic Overrides")]
        public float neighborDistOverride = 0f;
        public int maxNeighborsOverride = 0;
        public float timeHorizonOverride = 0f;

        [Header("Runtime Data (Internal)")]
        public Vector2 preferredVelocity;
        public Vector2 currentVelocity;
        private Vector2 nextVelocity;

        public float Radius => (radiusOverride > 0.001f) ? radiusOverride : (ORCASimulator.Instance != null ? ORCASimulator.Instance.defaultRadius : 0.5f);
        public float MaxSpeed => (maxSpeedOverride > 0.001f) ? maxSpeedOverride : (ORCASimulator.Instance != null ? ORCASimulator.Instance.defaultMaxSpeed : 5f);
        public LayerMask AvoidanceMask => (avoidanceMaskOverride.value != 0) ? avoidanceMaskOverride : (ORCASimulator.Instance != null ? ORCASimulator.Instance.defaultAvoidanceMask : (LayerMask)0);

        public Vector2 Position2D => new Vector2(transform.position.x, transform.position.z);
        
        private List<Line> orcaLines = new List<Line>();
        private List<ORCAAgent> neighbors = new List<ORCAAgent>();
        private bool isRegistered = false;

        void Update()
        {
            if (!isRegistered && ORCASimulator.Instance != null)
            {
                ORCASimulator.Instance.RegisterAgent(this);
                isRegistered = true;
            }
        }

        void OnEnable()
        {
            if (ORCASimulator.Instance != null)
            {
                ORCASimulator.Instance.RegisterAgent(this);
                isRegistered = true;
            }
        }

        void OnDisable()
        {
            if (isRegistered && ORCASimulator.Instance != null)
                ORCASimulator.Instance.UnregisterAgent(this);
            isRegistered = false;
        }

        public void ComputeNewVelocity()
        {
            orcaLines.Clear();
            
            if (ORCASimulator.Instance == null)
            {
                nextVelocity = preferredVelocity;
                return;
            }

            // Get Logic Parameters from Global or Override
            float neighborDist = neighborDistOverride > 0.001f ? neighborDistOverride : ORCASimulator.Instance.defaultNeighborDist;
            int maxNeighbors = maxNeighborsOverride > 0 ? maxNeighborsOverride : ORCASimulator.Instance.defaultMaxNeighbors;
            float timeHorizon = timeHorizonOverride > 0.001f ? timeHorizonOverride : ORCASimulator.Instance.defaultTimeHorizon;
            float squeeze = ORCASimulator.Instance.radiusSqueezeFactor;

            // Get Physical Parameters from our properties (which already handle defaults)
            float myRadius = Radius;
            float myMaxSpeed = MaxSpeed;
            LayerMask myMask = AvoidanceMask;

            ORCASimulator.Instance.GetNeighbors(this, neighborDist, myMask, neighbors);

            float invTimeHorizon = 1.0f / timeHorizon;

            for (int i = 0; i < neighbors.Count && i < maxNeighbors; i++)
            {
                ORCAAgent other = neighbors[i];
                Vector2 relativePosition = other.Position2D - Position2D;
                Vector2 relativeVelocity = currentVelocity - other.currentVelocity;
                float distSq = ORCAMath.sqrMagnitude(relativePosition);
                
                // Apply Squeeze Factor here
                float combinedRadius = (myRadius + other.Radius) * squeeze;
                float combinedRadiusSq = ORCAMath.sqr(combinedRadius);

                Line line;
                Vector2 u;

                if (distSq > combinedRadiusSq)
                {
                    Vector2 w = relativeVelocity - invTimeHorizon * relativePosition;
                    float wLengthSq = ORCAMath.sqrMagnitude(w);
                    float dotProduct1 = Vector2.Dot(w, relativePosition);

                    if (dotProduct1 < 0.0f && ORCAMath.sqr(dotProduct1) > combinedRadiusSq * wLengthSq)
                    {
                        float wLength = Mathf.Sqrt(wLengthSq);
                        Vector2 unitW = w / wLength;

                        line.direction = new Vector2(unitW.y, -unitW.x);
                        u = (combinedRadius * invTimeHorizon - wLength) * unitW;
                    }
                    else
                    {
                        float legLength = Mathf.Sqrt(distSq - combinedRadiusSq);

                        if (ORCAMath.det(relativePosition, w) > 0.0f)
                        {
                            line.direction = new Vector2(relativePosition.x * legLength - relativePosition.y * combinedRadius, relativePosition.x * combinedRadius + relativePosition.y * legLength) / distSq;
                        }
                        else
                        {
                            line.direction = -new Vector2(relativePosition.x * legLength + relativePosition.y * combinedRadius, -relativePosition.x * combinedRadius + relativePosition.y * legLength) / distSq;
                        }

                        float dotProduct2 = Vector2.Dot(relativeVelocity, line.direction);
                        u = dotProduct2 * line.direction - relativeVelocity;
                    }
                }
                else
                {
                    float invTimeStep = 1.0f / Time.fixedDeltaTime;
                    Vector2 w = relativeVelocity - invTimeStep * relativePosition;
                    float wLength = w.magnitude;
                    if (wLength > 0.001f)
                    {
                        Vector2 unitW = w / wLength;
                        line.direction = new Vector2(unitW.y, -unitW.x);
                        u = (combinedRadius * invTimeStep - wLength) * unitW;
                    }
                    else
                    {
                        line.direction = Vector2.right;
                        u = Vector2.zero;
                    }
                }

                line.point = currentVelocity + 0.5f * u;
                orcaLines.Add(line);
            }

            int lineFail = ORCAMath.SolveLinearProgram2(orcaLines, myMaxSpeed, preferredVelocity, false, ref nextVelocity);

            if (lineFail < orcaLines.Count)
            {
                ORCAMath.SolveLinearProgram3(orcaLines, 0, lineFail, myMaxSpeed, ref nextVelocity);
            }
        }

        public void PostUpdate()
        {
            currentVelocity = nextVelocity;
        }

        void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(transform.position, Radius);

            if (Application.isPlaying)
            {
                Vector3 pos = transform.position;
                Gizmos.color = Color.blue;
                Gizmos.DrawLine(pos, pos + new Vector3(currentVelocity.x, 0, currentVelocity.y));
                
                Gizmos.color = Color.yellow;
                Gizmos.DrawLine(pos, pos + new Vector3(preferredVelocity.x, 0, preferredVelocity.y));
            }
        }
    }
}

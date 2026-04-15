using UnityEngine;
using System.Collections.Generic;

namespace Navigation.ORCA
{
    public class ORCASimulator : MonoBehaviour
    {
        public static ORCASimulator Instance { get; private set; }

        [Header("Global Agent Defaults")]
        public float defaultRadius = 0.5f;
        public float defaultMaxSpeed = 5.0f;
        public LayerMask defaultAvoidanceMask;

        [Header("Global Avoidance Logic")]
        [Tooltip("Multiplies the radius during avoidance math. Set to 0.8 or 0.9 to let agents 'squeeze' through tight spots.")]
        public float radiusSqueezeFactor = 0.9f;
        public float defaultNeighborDist = 5.0f;
        public int defaultMaxNeighbors = 10;
        public float defaultTimeHorizon = 2.0f;

        private List<ORCAAgent> agents = new List<ORCAAgent>();

        void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Destroy(gameObject);
            }
        }

        public void RegisterAgent(ORCAAgent agent)
        {
            if (!agents.Contains(agent))
            {
                agents.Add(agent);
            }
        }

        public void UnregisterAgent(ORCAAgent agent)
        {
            agents.Remove(agent);
        }

        void FixedUpdate()
        {
            for (int i = 0; i < agents.Count; i++)
            {
                if (agents[i].isActiveAndEnabled)
                {
                    agents[i].ComputeNewVelocity();
                }
            }

            for (int i = 0; i < agents.Count; i++)
            {
                if (agents[i].isActiveAndEnabled)
                {
                    agents[i].PostUpdate();
                }
            }
        }

        public void GetNeighbors(ORCAAgent agent, float range, LayerMask mask, List<ORCAAgent> neighbors)
        {
            neighbors.Clear();
            Vector2 pos = agent.Position2D;
            float rangeSq = range * range;

            for (int i = 0; i < agents.Count; i++)
            {
                ORCAAgent other = agents[i];
                if (other == agent || !other.isActiveAndEnabled) continue;

                if ((mask.value & (1 << other.gameObject.layer)) == 0) continue;

                float distSq = (other.Position2D - pos).sqrMagnitude;
                if (distSq < rangeSq)
                {
                    neighbors.Add(other);
                }
            }
        }
    }
}

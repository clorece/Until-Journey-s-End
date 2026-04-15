using UnityEngine;
using System.Collections.Generic;

namespace Navigation.ORCA
{
    /// <summary>
    /// Represents a directed line (half-plane) for ORCA constraints.
    /// Everything to the "left" of the direction is considered valid.
    /// </summary>
    public struct Line
    {
        public Vector2 direction;
        public Vector2 point;
    }

    public static class ORCAMath
    {
        private const float EPSILON = 0.00001f;

        /// <summary>
        /// Solves a 1D linear program along a line.
        /// </summary>
        public static bool SolveLinearProgram1(List<Line> lines, int lineNo, float radius, Vector2 optVelocity, bool directionOpt, ref Vector2 result)
        {
            float dotProduct = Vector2.Dot(lines[lineNo].point, lines[lineNo].direction);
            float discriminant = sqr(dotProduct) + sqr(radius) - sqrMagnitude(lines[lineNo].point);

            if (discriminant < 0.0f)
            {
                // Max speed circle fully invalidates this line.
                return false;
            }

            float sqrtDiscriminant = Mathf.Sqrt(discriminant);
            float tLeft = -dotProduct - sqrtDiscriminant;
            float tRight = -dotProduct + sqrtDiscriminant;

            for (int i = 0; i < lineNo; ++i)
            {
                float denominator = det(lines[lineNo].direction, lines[i].direction);
                float numerator = det(lines[i].direction, lines[lineNo].point - lines[i].point);

                if (Mathf.Abs(denominator) <= EPSILON)
                {
                    // Lines are parallel.
                    if (numerator < 0.0f)
                    {
                        return false;
                    }
                    continue;
                }

                float t = numerator / denominator;

                if (denominator >= 0.0f)
                {
                    // Line i bounds line lineNo on the right.
                    tRight = Mathf.Min(tRight, t);
                }
                else
                {
                    // Line i bounds line lineNo on the left.
                    tLeft = Mathf.Max(tLeft, t);
                }

                if (tLeft > tRight)
                {
                    return false;
                }
            }

            if (directionOpt)
            {
                // Optimize direction.
                if (Vector2.Dot(optVelocity, lines[lineNo].direction) > 0.0f)
                {
                    result = lines[lineNo].point + tRight * lines[lineNo].direction;
                }
                else
                {
                    result = lines[lineNo].point + tLeft * lines[lineNo].direction;
                }
            }
            else
            {
                // Optimize closest point.
                float t = Vector2.Dot(lines[lineNo].direction, optVelocity - lines[lineNo].point);

                if (t < tLeft)
                {
                    result = lines[lineNo].point + tLeft * lines[lineNo].direction;
                }
                else if (t > tRight)
                {
                    result = lines[lineNo].point + tRight * lines[lineNo].direction;
                }
                else
                {
                    result = lines[lineNo].point + t * lines[lineNo].direction;
                }
            }

            return true;
        }

        /// <summary>
        /// Solves a 2D linear program (intersections of half-planes).
        /// </summary>
        public static int SolveLinearProgram2(List<Line> lines, float radius, Vector2 optVelocity, bool directionOpt, ref Vector2 result)
        {
            if (directionOpt)
            {
                result = optVelocity * radius;
            }
            else if (sqrMagnitude(optVelocity) > sqr(radius))
            {
                result = optVelocity.normalized * radius;
            }
            else
            {
                result = optVelocity;
            }

            for (int i = 0; i < lines.Count; ++i)
            {
                if (det(lines[i].direction, lines[i].point - result) > 0.0f)
                {
                    // Result does not satisfy constraint i.
                    Vector2 tempResult = result;
                    if (!SolveLinearProgram1(lines, i, radius, optVelocity, directionOpt, ref result))
                    {
                        result = tempResult;
                        return i;
                    }
                }
            }

            return lines.Count;
        }

        /// <summary>
        /// Solves a 3D linear program (used when LP2 fails due to conflicting constraints).
        /// </summary>
        public static void SolveLinearProgram3(List<Line> lines, int numObstLines, int beginLine, float radius, ref Vector2 result)
        {
            float distance = 0.0f;

            for (int i = beginLine; i < lines.Count; ++i)
            {
                if (det(lines[i].direction, lines[i].point - result) > distance)
                {
                    // Result does not satisfy constraint i.
                    List<Line> projLines = new List<Line>();
                    for (int j = 0; j < numObstLines; ++j)
                    {
                        projLines.Add(lines[j]);
                    }

                    for (int j = numObstLines; j < i; ++j)
                    {
                        Line line;
                        float determinant = det(lines[i].direction, lines[j].direction);

                        if (Mathf.Abs(determinant) <= EPSILON)
                        {
                            // Parallel lines.
                            if (Vector2.Dot(lines[i].direction, lines[j].direction) > 0.0f)
                            {
                                // Point in same direction.
                                continue;
                            }
                            else
                            {
                                // Point in opposite direction.
                                line.point = 0.5f * (lines[i].point + lines[j].point);
                            }
                        }
                        else
                        {
                            line.point = lines[i].point + (det(lines[j].direction, lines[i].point - lines[j].point) / determinant) * lines[i].direction;
                        }

                        line.direction = (lines[j].direction - lines[i].direction).normalized;
                        projLines.Add(line);
                    }

                    Vector2 tempResult = result;
                    if (SolveLinearProgram2(projLines, radius, new Vector2(-lines[i].direction.y, lines[i].direction.x), true, ref result) < projLines.Count)
                    {
                        // This should in principle not happen.
                        result = tempResult;
                    }

                    distance = det(lines[i].direction, lines[i].point - result);
                }
            }
        }

        public static float sqr(float a) => a * a;
        public static float sqrMagnitude(Vector2 v) => v.x * v.x + v.y * v.y;
        public static float det(Vector2 a, Vector2 b) => a.x * b.y - a.y * b.x;
    }
}

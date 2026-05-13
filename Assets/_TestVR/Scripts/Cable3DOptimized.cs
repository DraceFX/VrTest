using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class Cable3DOptimized : MonoBehaviour
{
    [Header("Anchors")]
    public Transform startPoint;
    public Transform endPoint;

    [Header("Cable")]
    [Range(4, 128)]
    public int segmentCount = 32;

    public float segmentLength = 0.25f;
    public float cableRadius = 0.05f;

    [Header("Simulation")]
    public Vector3 gravity = new Vector3(0, -9.81f, 0);

    [Range(1, 20)]
    public int solverIterations = 8;

    [Range(0.9f, 1f)]
    public float damping = 0.995f;

    [Header("Collision")]
    public LayerMask collisionMask;

    [Range(1, 16)]
    public int collisionIterations = 2;

    private LineRenderer lineRenderer;

    private Vector3[] positions;
    private Vector3[] previousPositions;

    private Collider[] collisionBuffer = new Collider[8];

    void Awake()
    {
        lineRenderer = GetComponent<LineRenderer>();

        positions = new Vector3[segmentCount];
        previousPositions = new Vector3[segmentCount];

        GenerateCable();
    }

    void GenerateCable()
    {
        Vector3 start = startPoint.position;
        Vector3 end = endPoint.position;

        for (int i = 0; i < segmentCount; i++)
        {
            float t = i / (float)(segmentCount - 1);

            Vector3 pos = Vector3.Lerp(start, end, t);

            positions[i] = pos;
            previousPositions[i] = pos;
        }
    }

    void FixedUpdate()
    {
        Simulate();

        for (int i = 0; i < solverIterations; i++)
        {
            SolveConstraints();

            if (i < collisionIterations)
            {
                SolveCollisions();
            }
        }
    }

    void Simulate()
    {
        float dt = Time.fixedDeltaTime;
        Vector3 gravityStep = gravity * (dt * dt);

        for (int i = 1; i < segmentCount - 1; i++)
        {
            Vector3 current = positions[i];

            Vector3 velocity = (current - previousPositions[i]) * damping;

            previousPositions[i] = current;

            positions[i] += velocity;
            positions[i] += gravityStep;
        }

        positions[0] = startPoint.position;
        positions[segmentCount - 1] = endPoint.position;

        previousPositions[0] = startPoint.position;
        previousPositions[segmentCount - 1] = endPoint.position;
    }

    void SolveConstraints()
    {
        positions[0] = startPoint.position;
        positions[segmentCount - 1] = endPoint.position;

        for (int i = 0; i < segmentCount - 1; i++)
        {
            Vector3 a = positions[i];
            Vector3 b = positions[i + 1];

            Vector3 delta = b - a;

            float dist = delta.magnitude;

            if (dist <= 0.0001f)
                continue;

            float error = dist - segmentLength;

            Vector3 correction = delta / dist * error;

            if (i != 0)
                positions[i] += correction * 0.5f;

            if (i != segmentCount - 2)
                positions[i + 1] -= correction * 0.5f;
        }
    }

    void SolveCollisions()
    {
        float radiusSqr = cableRadius * cableRadius;

        for (int i = 1; i < segmentCount - 1; i++)
        {
            Vector3 point = positions[i];

            int hitCount = Physics.OverlapSphereNonAlloc(
                point,
                cableRadius,
                collisionBuffer,
                collisionMask,
                QueryTriggerInteraction.Ignore
            );

            for (int h = 0; h < hitCount; h++)
            {
                Collider col = collisionBuffer[h];

                Vector3 closest = col.ClosestPoint(point);

                Vector3 offset = point - closest;

                float sqrDist = offset.sqrMagnitude;

                if (sqrDist < radiusSqr)
                {
                    float dist = Mathf.Sqrt(sqrDist);

                    Vector3 normal;

                    if (dist > 0.0001f)
                    {
                        normal = offset / dist;
                    }
                    else
                    {
                        normal = Vector3.up;
                        dist = 0f;
                    }

                    float penetration = cableRadius - dist;

                    // SOFT CORRECTION
                    positions[i] += normal * (penetration * 0.8f);

                    // REMOVE NORMAL VELOCITY
                    Vector3 velocity =
                        positions[i] - previousPositions[i];

                    float normalVelocity =
                        Vector3.Dot(velocity, normal);

                    if (normalVelocity < 0f)
                    {
                        velocity -= normal * normalVelocity;
                    }

                    // SURFACE FRICTION
                    velocity *= 0.9f;

                    previousPositions[i] =
                        positions[i] - velocity;
                }
            }
        }
    }

    void LateUpdate()
    {
        RenderCable();
    }

    void RenderCable()
    {
        lineRenderer.positionCount = segmentCount;

        for (int i = 0; i < segmentCount; i++)
        {
            lineRenderer.SetPosition(i, positions[i]);
        }
    }

    void OnDrawGizmosSelected()
    {
        if (positions == null)
            return;

        Gizmos.color = Color.yellow;

        for (int i = 0; i < positions.Length; i++)
        {
            Gizmos.DrawSphere(positions[i], cableRadius);
        }
    }
}
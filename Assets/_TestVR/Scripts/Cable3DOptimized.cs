using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class Cable3DOptimized : MonoBehaviour
{
    [Header("Anchors")]
    [SerializeField] private Transform _startPoint;
    [SerializeField] private Transform _endPoint;

    [Header("Cable")]
    [Range(4, 128)]
    [SerializeField] private int _segmentCount = 32;

    [SerializeField] private float _segmentLength = 0.25f;
    [SerializeField] private float _cableRadius = 0.05f;

    [Header("Simulation")]
    [SerializeField] private Vector3 _gravity = new Vector3(0, -9.81f, 0);

    [Range(1, 20)]
    [SerializeField] private int _solverIterations = 8;

    [Range(0.9f, 1f)]
    [SerializeField] private float _damping = 0.995f;

    [Header("Collision")]
    [SerializeField] private LayerMask _collisionMask;

    [Range(1, 16)]
    [SerializeField] private int _collisionIterations = 2;

    private LineRenderer _lineRenderer;

    private Vector3[] _positions;
    private Vector3[] _previousPositions;

    private Collider[] _collisionBuffer = new Collider[8];

    private void Awake()
    {
        _lineRenderer = GetComponent<LineRenderer>();

        _positions = new Vector3[_segmentCount];
        _previousPositions = new Vector3[_segmentCount];

        GenerateCable();
    }

    private void GenerateCable()
    {
        Vector3 start = _startPoint.position;
        Vector3 end = _endPoint.position;

        for (int i = 0; i < _segmentCount; i++)
        {
            float t = i / (float)(_segmentCount - 1);

            Vector3 pos = Vector3.Lerp(start, end, t);

            _positions[i] = pos;
            _previousPositions[i] = pos;
        }
    }

    private void FixedUpdate()
    {
        Simulate();

        for (int i = 0; i < _solverIterations; i++)
        {
            SolveConstraints();

            if (i < _collisionIterations)
            {
                SolveCollisions();
            }
        }
    }

    private void Simulate()
    {
        float dt = Time.fixedDeltaTime;
        Vector3 gravityStep = _gravity * (dt * dt);

        for (int i = 1; i < _segmentCount - 1; i++)
        {
            Vector3 current = _positions[i];

            Vector3 velocity = (current - _previousPositions[i]) * _damping;

            _previousPositions[i] = current;

            _positions[i] += velocity;
            _positions[i] += gravityStep;
        }

        _positions[0] = _startPoint.position;
        _positions[_segmentCount - 1] = _endPoint.position;

        _previousPositions[0] = _startPoint.position;
        _previousPositions[_segmentCount - 1] = _endPoint.position;
    }

    private void SolveConstraints()
    {
        _positions[0] = _startPoint.position;
        _positions[_segmentCount - 1] = _endPoint.position;

        for (int i = 0; i < _segmentCount - 1; i++)
        {
            Vector3 a = _positions[i];
            Vector3 b = _positions[i + 1];

            Vector3 delta = b - a;

            float dist = delta.magnitude;

            if (dist <= 0.0001f) continue;

            float error = dist - _segmentLength;

            Vector3 correction = delta / dist * error;

            if (i != 0)
                _positions[i] += correction * 0.5f;

            if (i != _segmentCount - 2)
                _positions[i + 1] -= correction * 0.5f;
        }
    }

    private void SolveCollisions()
    {
        float radiusSqr = _cableRadius * _cableRadius;

        for (int i = 1; i < _segmentCount - 1; i++)
        {
            Vector3 point = _positions[i];

            int hitCount = Physics.OverlapSphereNonAlloc(point, _cableRadius, _collisionBuffer, _collisionMask, QueryTriggerInteraction.Ignore);

            for (int h = 0; h < hitCount; h++)
            {
                Collider col = _collisionBuffer[h];

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

                    float penetration = _cableRadius - dist;

                    _positions[i] += normal * (penetration * 0.8f);

                    Vector3 velocity = _positions[i] - _previousPositions[i];

                    float normalVelocity = Vector3.Dot(velocity, normal);

                    if (normalVelocity < 0f)
                    {
                        velocity -= normal * normalVelocity;
                    }

                    velocity *= 0.9f;

                    _previousPositions[i] = _positions[i] - velocity;
                }
            }
        }
    }

    private void LateUpdate()
    {
        RenderCable();
    }

    private void RenderCable()
    {
        _lineRenderer.positionCount = _segmentCount;

        for (int i = 0; i < _segmentCount; i++)
        {
            _lineRenderer.SetPosition(i, _positions[i]);
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (_positions == null) return;

        Gizmos.color = Color.yellow;

        for (int i = 0; i < _positions.Length; i++)
        {
            Gizmos.DrawSphere(_positions[i], _cableRadius);
        }
    }
}
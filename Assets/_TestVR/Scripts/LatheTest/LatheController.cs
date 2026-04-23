using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class LatheControllerVR : MonoBehaviour
{
    [Header("Shape Settings")]
    public int lengthSegments = 120;
    public int radialSegments = 32;
    public float length = 2f;
    public float initialRadius = 0.5f;
    public float minRadius = 0.05f;

    [Header("Tool")]
    public Transform tool;

    public enum LatheToolShape
    {
        Turning,
        Drill,
        Ball,
        Cone
    }

    public LatheToolShape toolShape = LatheToolShape.Turning;

    [Range(1f, 89f)]
    public float coneAngle = 45f;

    public float toolWidth = 0.05f;

    [Header("Performance")]
    public float updateRate = 0.05f;
    public bool updateNormals = true;

    private Mesh mesh;

    private float[] radii;
    private Vector3[] vertices;
    private int[] triangles;

    private float halfLen;

    private float timer;
    private bool dirty;

    // =========================
    // INIT
    // =========================
    private void Start()
    {
        mesh = new Mesh();
        mesh.name = "Lathe Mesh";
        GetComponent<MeshFilter>().mesh = mesh;

        halfLen = length * 0.5f;

        InitRadii();
        BuildTopology();
        GenerateVertices();
        ApplyMesh();
    }

    private void InitRadii()
    {
        radii = new float[lengthSegments];

        for (int i = 0; i < lengthSegments; i++)
            radii[i] = initialRadius;
    }

    // =========================
    // UPDATE
    // =========================
    private void Update()
    {
        if (tool != null)
            HandleCut();

        timer += Time.deltaTime;

        if (dirty && timer >= updateRate)
        {
            timer = 0f;

            SmoothRadii();
            GenerateVertices();
            ApplyMesh();

            dirty = false;
        }
    }

    // =========================
    // CUT LOGIC
    // =========================
    private void HandleCut()
    {
        Vector3 local = transform.InverseTransformPoint(tool.position);

        float toolX = local.x;

        for (int i = 0; i < lengthSegments; i++)
        {
            float x = Mathf.Lerp(-halfLen, halfLen, (float)i / (lengthSegments - 1));

            float dx = Mathf.Abs(x - toolX);

            if (dx > toolWidth) continue;

            float targetRadius = GetToolRadiusAt(i, x, local);

            if (targetRadius < radii[i])
            {
                radii[i] = Mathf.Max(targetRadius, minRadius);
                dirty = true;
            }
        }
    }

    // =========================
    // TOOL SHAPES
    // =========================
    private float GetToolRadiusAt(int index, float x, Vector3 toolLocal)
    {
        switch (toolShape)
        {
            case LatheToolShape.Turning:
                return TurningTool(toolLocal);

            case LatheToolShape.Drill:
                return DrillTool(index, x, toolLocal);

            case LatheToolShape.Ball:
                return BallTool(x, toolLocal);

            case LatheToolShape.Cone:
                return ConeTool(x, toolLocal);

            default:
                return initialRadius;
        }
    }

    private float TurningTool(Vector3 toolLocal)
    {
        return new Vector2(toolLocal.y, toolLocal.z).magnitude;
    }

    private float DrillTool(int index, float x, Vector3 toolLocal)
    {
        float toolRadius = new Vector2(toolLocal.y, toolLocal.z).magnitude;

        float currentRadius = radii[index];

        // 1. Проверка: инструмент вообще касается детали?
        if (toolRadius > currentRadius)
            return currentRadius; // не режем

        // 2. Проверка: дошли ли по X
        if (x > toolLocal.x)
            return toolRadius; // сверло прошло — оставляем отверстие

        // 3. Перед сверлом — ничего не делаем
        return currentRadius;
    }

    private float BallTool(float x, Vector3 toolLocal)
    {
        float r = toolWidth;

        float dx = x - toolLocal.x;

        if (Mathf.Abs(dx) > r)
            return initialRadius;

        float radialOffset = Mathf.Sqrt(r * r - dx * dx);

        float centerRadius = new Vector2(toolLocal.y, toolLocal.z).magnitude;

        return centerRadius - radialOffset;
    }

    private float ConeTool(float x, Vector3 toolLocal)
    {
        float dx = Mathf.Abs(x - toolLocal.x);

        float slope = Mathf.Tan(coneAngle * Mathf.Deg2Rad);

        float centerRadius = new Vector2(toolLocal.y, toolLocal.z).magnitude;

        return centerRadius + dx * slope;
    }

    // =========================
    // SMOOTHING
    // =========================
    private void SmoothRadii(int iterations = 1, float factor = 0.25f)
    {
        for (int it = 0; it < iterations; it++)
        {
            float[] temp = (float[])radii.Clone();

            for (int i = 1; i < radii.Length - 1; i++)
            {
                float avg = (radii[i - 1] + radii[i] + radii[i + 1]) / 3f;
                temp[i] = Mathf.Lerp(radii[i], avg, factor);
            }

            radii = temp;
        }
    }

    // =========================
    // MESH
    // =========================
    private void BuildTopology()
    {
        int baseCount = lengthSegments * radialSegments;

        vertices = new Vector3[baseCount + 2];
        triangles = new int[(lengthSegments - 1) * radialSegments * 6 + radialSegments * 6];

        int leftCenter = baseCount;
        int rightCenter = baseCount + 1;

        int tri = 0;

        for (int i = 0; i < lengthSegments - 1; i++)
        {
            for (int j = 0; j < radialSegments; j++)
            {
                int current = i * radialSegments + j;
                int next = current + radialSegments;

                int nextJ = (j + 1) % radialSegments;

                int currentNext = i * radialSegments + nextJ;
                int nextNext = (i + 1) * radialSegments + nextJ;

                triangles[tri++] = current;
                triangles[tri++] = currentNext;
                triangles[tri++] = next;

                triangles[tri++] = currentNext;
                triangles[tri++] = nextNext;
                triangles[tri++] = next;
            }
        }

        // left cap
        for (int j = 0; j < radialSegments; j++)
        {
            int a = j;
            int b = (j + 1) % radialSegments;

            triangles[tri++] = leftCenter;
            triangles[tri++] = b;
            triangles[tri++] = a;
        }

        // right cap
        int offset = (lengthSegments - 1) * radialSegments;

        for (int j = 0; j < radialSegments; j++)
        {
            int a = offset + j;
            int b = offset + (j + 1) % radialSegments;

            triangles[tri++] = rightCenter;
            triangles[tri++] = a;
            triangles[tri++] = b;
        }
    }

    private void GenerateVertices()
    {
        int baseCount = lengthSegments * radialSegments;

        for (int i = 0; i < lengthSegments; i++)
        {
            float x = Mathf.Lerp(-halfLen, halfLen, (float)i / (lengthSegments - 1));
            float radius = radii[i];

            for (int j = 0; j < radialSegments; j++)
            {
                float angle = j * Mathf.PI * 2f / radialSegments;

                float y = Mathf.Cos(angle) * radius;
                float z = Mathf.Sin(angle) * radius;

                vertices[i * radialSegments + j] = new Vector3(x, y, z);
            }
        }

        vertices[baseCount] = new Vector3(-halfLen, 0, 0);
        vertices[baseCount + 1] = new Vector3(halfLen, 0, 0);
    }

    private void ApplyMesh()
    {
        mesh.Clear();
        mesh.vertices = vertices;
        mesh.triangles = triangles;

        if (updateNormals)
            mesh.RecalculateNormals();

        mesh.RecalculateBounds();
    }

    // =========================
    // DEBUG
    // =========================
    private void OnDrawGizmosSelected()
    {
        if (tool != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawSphere(tool.position, 0.02f);
        }
        Gizmos.color = Color.cyan;

        float halfLen = length * 0.5f;
        float radius = initialRadius;

        Vector3 center = transform.position;

        DrawWireCylinder(center, halfLen, radius, 32);
    }

    private void DrawWireCylinder(Vector3 center, float halfLength, float radius, int segments)
    {
        Vector3 leftCenter = center + Vector3.left * halfLength;
        Vector3 rightCenter = center + Vector3.right * halfLength;

        Vector3 prevLeft = Vector3.zero;
        Vector3 prevRight = Vector3.zero;

        for (int i = 0; i <= segments; i++)
        {
            float t = (float)i / segments;
            float angle = t * Mathf.PI * 2f;

            float y = Mathf.Cos(angle) * radius;
            float z = Mathf.Sin(angle) * radius;

            Vector3 offset = new Vector3(0, y, z);

            Vector3 pLeft = leftCenter + offset;
            Vector3 pRight = rightCenter + offset;

            if (i > 0)
            {
                Gizmos.DrawLine(prevLeft, pLeft);
                Gizmos.DrawLine(prevRight, pRight);
                Gizmos.DrawLine(prevLeft, prevRight);
            }

            prevLeft = pLeft;
            prevRight = pRight;
        }
    }
}
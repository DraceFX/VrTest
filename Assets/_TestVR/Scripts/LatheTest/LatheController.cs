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

    [Header("Cut Settings")]
    public float cutStrength = 0.01f;
    public Transform tool;

    [Header("Performance (VR)")]
    public float updateRate = 0.05f; // 20 Hz
    public bool updateNormals = true;

    public Mesh sourceMesh;

    private Mesh mesh;

    private float[] radii;
    private Vector3[] vertices;
    private int[] triangles;

    private bool useProvidedMesh;
    private bool dirty;
    private float timer;

    private float halfLen;

    // =========================
    // INIT
    // =========================
    private void Start()
    {
        mesh = GetComponent<MeshFilter>().mesh;
        halfLen = length / 2f;

        useProvidedMesh = sourceMesh != null;

        if (useProvidedMesh)
        {
            UseProvidedMesh();
        }
        else
        {
            InitRadii();
            BuildTopologyOnce();
            GenerateVertices();
            ApplyMesh();
        }
    }

    // =========================
    // UPDATE (VR OPTIMIZED)
    // =========================
    private void Update()
    {
        if (useProvidedMesh)
        {
            HandleCutOnMesh();

            timer += Time.deltaTime;

            if (dirty && timer >= updateRate)
            {
                timer = 0f;
                ApplyMesh();
                dirty = false;
            }

        }
        else
        {
            HandleCutOnLathe();

            timer += Time.deltaTime;

            if (dirty && timer >= updateRate)
            {
                timer = 0f;
                GenerateVertices();
                ApplyMesh();
                dirty = false;
            }
        }
    }

    // =========================
    // INIT RADII
    // =========================
    private void InitRadii()
    {
        radii = new float[lengthSegments];

        for (int i = 0; i < lengthSegments; i++)
            radii[i] = initialRadius;
    }

    // =========================
    // OPTIONAL MESH MODE
    // =========================
    private void UseProvidedMesh()
    {
        mesh = Instantiate(sourceMesh); // создаём ПОЛНУЮ копию
        GetComponent<MeshFilter>().mesh = mesh;

        vertices = mesh.vertices;
        triangles = mesh.triangles;

        mesh.RecalculateNormals();  // обязательно
        mesh.RecalculateBounds();   // обязательн
    }

    // =========================
    // CUT (LATHE MODE)
    // =========================
    private void HandleCutOnLathe()
    {
        if (tool == null) return;

        Vector3 localPos = transform.InverseTransformPoint(tool.position);

        float t = Mathf.InverseLerp(-halfLen, halfLen, localPos.x);
        int index = Mathf.Clamp(Mathf.RoundToInt(t * (lengthSegments - 1)), 0, lengthSegments - 1);

        float dist = new Vector2(localPos.y, localPos.z).magnitude;

        if (dist < radii[index])
        {
            radii[index] -= cutStrength;
            radii[index] = Mathf.Max(radii[index], minRadius);

            dirty = true;
        }
    }

    // =========================
    // CUT (MESH MODE)
    // =========================
    private void HandleCutOnMesh()
    {
        if (tool == null || vertices == null) return;

        Vector3 local = transform.InverseTransformPoint(tool.position);

        bool changed = false;

        for (int i = 0; i < vertices.Length; i++)
        {
            Vector3 v = vertices[i];

            // расстояние вдоль оси X (длина детали)
            float dx = Mathf.Abs(v.x - local.x);

            if (dx > 0.05f) continue; // ширина резца

            // радиус в плоскости YZ
            float radius = new Vector2(v.y, v.z).magnitude;

            float toolRadius = new Vector2(local.y, local.z).magnitude;

            if (radius > toolRadius)
            {
                float newRadius = Mathf.Max(radius - cutStrength, minRadius);

                if (radius > 0.0001f)
                {
                    float factor = newRadius / radius;

                    v.y *= factor;
                    v.z *= factor;

                    vertices[i] = v;
                    changed = true;
                }
            }
        }

        if (changed)
        {
            dirty = true;
        }
    }

    // =========================
    // BUILD TOPOLOGY ONCE (IMPORTANT FOR VR)
    // =========================
    private void BuildTopologyOnce()
    {
        int baseCount = lengthSegments * radialSegments;

        Vector3[] baseVertices = new Vector3[baseCount];
        vertices = new Vector3[baseCount + 2];


        triangles = new int[(lengthSegments - 1) * radialSegments * 6 + radialSegments * 6];

        // caps indices
        int leftCenterIndex = baseCount;
        int rightCenterIndex = baseCount + 1;

        vertices = new Vector3[baseCount + 2];

        int triIndex = 0;

        // SIDE + CAPS STRUCTURE (fixed topology)
        for (int i = 0; i < lengthSegments - 1; i++)
        {
            for (int j = 0; j < radialSegments; j++)
            {
                int current = i * radialSegments + j;
                int next = current + radialSegments;

                int nextJ = (j + 1) % radialSegments;

                int currentNext = i * radialSegments + nextJ;
                int nextRowNext = (i + 1) * radialSegments + nextJ;

                triangles[triIndex++] = current;
                triangles[triIndex++] = currentNext;
                triangles[triIndex++] = next;

                triangles[triIndex++] = currentNext;
                triangles[triIndex++] = nextRowNext;
                triangles[triIndex++] = next;
            }
        }

        for (int j = 0; j < radialSegments; j++)
        {
            int a = j;
            int b = (j + 1) % radialSegments;

            triangles[triIndex++] = leftCenterIndex;
            triangles[triIndex++] = b;
            triangles[triIndex++] = a;
        }

        int offset = (lengthSegments - 1) * radialSegments;

        for (int j = 0; j < radialSegments; j++)
        {
            int a = offset + j;
            int b = offset + (j + 1) % radialSegments;

            triangles[triIndex++] = rightCenterIndex;
            triangles[triIndex++] = a;
            triangles[triIndex++] = b;
        }

        // caps stored implicitly, geometry generated later
    }

    // =========================
    // VERTEX UPDATE ONLY
    // =========================
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

        // caps
        vertices[baseCount] = new Vector3(-halfLen, 0, 0);
        vertices[baseCount + 1] = new Vector3(halfLen, 0, 0);
    }

    // =========================
    // APPLY MESH (VR SAFE)
    // =========================
    private void ApplyMesh()
    {
        mesh.vertices = vertices;
        mesh.triangles = triangles;

        if (updateNormals)
            mesh.RecalculateNormals();

        mesh.RecalculateBounds();
    }
}
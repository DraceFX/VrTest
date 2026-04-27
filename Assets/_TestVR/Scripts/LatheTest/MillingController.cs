using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class MillingСontrollerVR : MonoBehaviour
{
    [Header("Resolution")]
    public int widthSegments = 120;
    public int depthSegments = 120;

    [Header("Dimensions")]
    public Vector3 dimensions = new Vector3(2f, 1f, 2f);
    public float minThickness = 0.05f;

    [Header("Cut Settings")]
    public Transform tool;
    public ToolTypeMilling MillingType;

    [Header("Performance")]
    public float updateRate = 0.05f;
    public bool updateNormals = true;

    private Mesh mesh;

    private float[,] heightMap;

    private Vector3[] vertices;
    private int[] triangles;
    private Vector2[] uvs;

    private int vertCountX;
    private int vertCountZ;

    private float timer;
    private bool dirty;

    private float halfX;
    private float halfY;
    private float halfZ;

    // =========================
    // INIT
    // =========================
    private void Start()
    {
        mesh = new Mesh();
        mesh.name = "Milling HeightMap Mesh";
        GetComponent<MeshFilter>().mesh = mesh;

        vertCountX = widthSegments;
        vertCountZ = depthSegments;

        halfX = dimensions.x * 0.5f;
        halfY = dimensions.y * 0.5f;
        halfZ = dimensions.z * 0.5f;

        InitializeHeightMap();
        BuildMesh();
        ApplyMesh();
    }

    private void InitializeHeightMap()
    {
        heightMap = new float[vertCountX, vertCountZ];

        for (int x = 0; x < vertCountX; x++)
        {
            for (int z = 0; z < vertCountZ; z++)
            {
                heightMap[x, z] = halfY;
            }
        }
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
            UpdateVertices();
            ApplyMesh();
            dirty = false;
        }
    }

    // =========================
    // CUT LOGIC
    // =========================
    private void HandleCut()
    {
        Vector3 toolLocal = transform.InverseTransformPoint(tool.position);

        float radiusSq = MillingType.toolRadius * MillingType.toolRadius;

        for (int x = 0; x < vertCountX; x++)
        {
            float worldX = Mathf.Lerp(-halfX, halfX, (float)x / (vertCountX - 1));

            for (int z = 0; z < vertCountZ; z++)
            {
                float worldZ = Mathf.Lerp(-halfZ, halfZ, (float)z / (vertCountZ - 1));

                float dx = worldX - toolLocal.x;
                float dz = worldZ - toolLocal.z;

                float distSq = dx * dx + dz * dz;

                if (distSq > radiusSq) continue;

                float dist = Mathf.Sqrt(distSq);

                float targetY = MillingType.GetToolHeight(toolLocal.y, dist);

                // снимаем материал только если инструмент ниже поверхности
                if (heightMap[x, z] > targetY)
                {
                    heightMap[x, z] = Mathf.Max(
                        targetY,
                        -halfY + minThickness
                    );
                }
            }
        }

        dirty = true;
    }

    // =========================
    // MESH BUILD
    // =========================
    private void BuildMesh()
    {
        int topVertCount = vertCountX * vertCountZ;
        int bottomVertCount = topVertCount;

        vertices = new Vector3[topVertCount * 2];
        uvs = new Vector2[vertices.Length];

        // Верх + низ
        for (int x = 0; x < vertCountX; x++)
        {
            float px = Mathf.Lerp(-halfX, halfX, (float)x / (vertCountX - 1));

            for (int z = 0; z < vertCountZ; z++)
            {
                float pz = Mathf.Lerp(-halfZ, halfZ, (float)z / (vertCountZ - 1));

                int i = x * vertCountZ + z;

                // TOP
                vertices[i] = new Vector3(px, heightMap[x, z], pz);

                // BOTTOM
                vertices[i + topVertCount] = new Vector3(px, -halfY, pz);

                uvs[i] = new Vector2((float)x / vertCountX, (float)z / vertCountZ);
                uvs[i + topVertCount] = uvs[i];
            }
        }

        triangles = BuildTriangles(topVertCount);
    }

    private int[] BuildTriangles(int topVertCount)
    {
        System.Collections.Generic.List<int> tris = new System.Collections.Generic.List<int>();

        // TOP
        for (int x = 0; x < vertCountX - 1; x++)
        {
            for (int z = 0; z < vertCountZ - 1; z++)
            {
                int i = x * vertCountZ + z;

                int a = i;
                int b = i + vertCountZ;
                int c = i + 1;
                int d = i + vertCountZ + 1;

                tris.Add(a); tris.Add(d); tris.Add(b);
                tris.Add(a); tris.Add(c); tris.Add(d);
            }
        }

        // BOTTOM (перевернутые)
        for (int x = 0; x < vertCountX - 1; x++)
        {
            for (int z = 0; z < vertCountZ - 1; z++)
            {
                int i = x * vertCountZ + z + topVertCount;

                int a = i;
                int b = i + vertCountZ;
                int c = i + 1;
                int d = i + vertCountZ + 1;

                tris.Add(a); tris.Add(b); tris.Add(d);
                tris.Add(a); tris.Add(d); tris.Add(c);
            }
        }

        // СТЕНКИ
        BuildWalls(tris, topVertCount);

        return tris.ToArray();
    }

    private void BuildWalls(System.Collections.Generic.List<int> tris, int offset)
    {
        // FRONT (z = 0)
        for (int x = 0; x < vertCountX - 1; x++)
            AddWall(tris, x, 0, x + 1, 0, offset, true);

        // BACK (z = max) — инверсия
        for (int x = 0; x < vertCountX - 1; x++)
            AddWall(tris, x + 1, vertCountZ - 1, x, vertCountZ - 1, offset, true);

        // LEFT (x = 0) — инверсия
        for (int z = 0; z < vertCountZ - 1; z++)
            AddWall(tris, 0, z + 1, 0, z, offset, true);

        // RIGHT (x = max)
        for (int z = 0; z < vertCountZ - 1; z++)
            AddWall(tris, vertCountX - 1, z, vertCountX - 1, z + 1, offset, true);
    }

    private void AddWall(System.Collections.Generic.List<int> tris,
    int x1, int z1, int x2, int z2, int offset, bool dummy)
    {
        int i1 = x1 * vertCountZ + z1;
        int i2 = x2 * vertCountZ + z2;

        int b1 = i1 + offset;
        int b2 = i2 + offset;

        tris.Add(i1); tris.Add(i2); tris.Add(b1);
        tris.Add(i2); tris.Add(b2); tris.Add(b1);
    }

    // =========================
    // UPDATE VERTICES
    // =========================
    private void UpdateVertices()
    {
        int topCount = vertCountX * vertCountZ;

        for (int x = 0; x < vertCountX; x++)
        {
            for (int z = 0; z < vertCountZ; z++)
            {
                int i = x * vertCountZ + z;

                vertices[i].y = heightMap[x, z];
            }
        }
    }

    // =========================
    // APPLY
    // =========================
    private void ApplyMesh()
    {
        mesh.Clear();
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.uv = uvs;

        if (updateNormals)
            mesh.RecalculateNormals();

        mesh.RecalculateBounds();
    }

    // =========================
    // RESET
    // =========================
    public void ResetWorkpiece()
    {
        InitializeHeightMap();
        UpdateVertices();
        ApplyMesh();
    }

    // =========================
    // GIZMOS
    // =========================
    private void OnDrawGizmosSelected()
    {
        if (tool != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(tool.position, MillingType.toolRadius);
        }

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireCube(transform.position, dimensions);
    }
}
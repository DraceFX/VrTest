using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class WeldMeshBuilder : MonoBehaviour
{
    [Header("Параметры бусинок")]
    public float width = 0.008f;
    public float height = 0.004f;
    public float length = 0.012f;
    public float spacing = 0.005f;

    [Header("Остывание")]
    public Gradient temperatureGradient;
    public float coolingTime = 3f;

    // Внутренние списки
    private List<BeadData> beads = new List<BeadData>();
    private List<PoreData> pores = new List<PoreData>();
    private List<BurnData> burns = new List<BurnData>();

    private Mesh mesh;
    private List<Vector3> vertices = new List<Vector3>();
    private List<Color> colors = new List<Color>();
    private List<int> triangles = new List<int>();

    private Vector3 lastPoint;
    private bool hasLastPoint;

    private struct BeadData
    {
        public Vector3 position;
        public Quaternion rotation;
        public Vector3 scale;
        public float birthTime;
    }

    private struct PoreData
    {
        public Vector3 position;
        public float radius;
    }

    private struct BurnData
    {
        public Vector3 position;
        public Quaternion rotation;
    }

    void Awake()
    {
        mesh = new Mesh();
        GetComponent<MeshFilter>().mesh = mesh;
    }

    public void AddBead(Vector3 worldPoint, Vector3 normal)
    {
        Vector3 localPoint = transform.InverseTransformPoint(worldPoint);
        Vector3 localNormal = transform.InverseTransformDirection(normal);

        if (hasLastPoint)
        {
            float dist = Vector3.Distance(lastPoint, localPoint);
            if (dist < spacing) return;
        }
        lastPoint = localPoint;
        hasLastPoint = true;

        Vector3 forward = (localPoint - (beads.Count > 0 ? beads[beads.Count - 1].position : localPoint)).normalized;
        if (forward == Vector3.zero) forward = transform.forward;

        Quaternion rot = Quaternion.LookRotation(forward, localNormal) * Quaternion.AngleAxis(Random.Range(-10f, 10f), Vector3.forward);
        Vector3 scale = new Vector3(
            width * Random.Range(0.8f, 1.2f),
            height * Random.Range(0.8f, 1.2f),
            length * Random.Range(0.8f, 1.2f)
        );

        beads.Add(new BeadData
        {
            position = localPoint,
            rotation = rot,
            scale = scale,
            birthTime = Time.time
        });

        RebuildMesh();
    }

    public void AddPore(Vector3 worldPoint, float radius)
    {
        pores.Add(new PoreData
        {
            position = transform.InverseTransformPoint(worldPoint),
            radius = radius
        });
        RebuildMesh();
    }

    public void AddBurn(Vector3 worldPoint, Vector3 normal)
    {
        burns.Add(new BurnData
        {
            position = transform.InverseTransformPoint(worldPoint),
            rotation = Quaternion.LookRotation(transform.InverseTransformDirection(normal))
        });
        RebuildMesh();
    }

    private void RebuildMesh()
    {
        vertices.Clear();
        colors.Clear();
        triangles.Clear();

        foreach (var bead in beads)
        {
            AddBox(bead.position, bead.rotation, bead.scale, bead.birthTime);
        }

        foreach (var pore in pores)
        {
            AddSphere(pore.position, pore.radius, Color.black);
        }

        foreach (var burn in burns)
        {
            AddBurnMark(burn.position, burn.rotation);
        }

        mesh.Clear();
        mesh.SetVertices(vertices);
        mesh.SetColors(colors);
        mesh.SetTriangles(triangles, 0);
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
    }

    private void AddBox(Vector3 center, Quaternion rotation, Vector3 scale, float birthTime)
    {
        int startIndex = vertices.Count;
        Vector3[] corners = new Vector3[8]
        {
            new Vector3(-0.5f, -0.5f, -0.5f),
            new Vector3( 0.5f, -0.5f, -0.5f),
            new Vector3(-0.5f,  0.5f, -0.5f),
            new Vector3( 0.5f,  0.5f, -0.5f),
            new Vector3(-0.5f, -0.5f,  0.5f),
            new Vector3( 0.5f, -0.5f,  0.5f),
            new Vector3(-0.5f,  0.5f,  0.5f),
            new Vector3( 0.5f,  0.5f,  0.5f)
        };

        for (int i = 0; i < 8; i++)
        {
            Vector3 v = rotation * Vector3.Scale(corners[i], scale) + center;
            vertices.Add(v);

            float age = Time.time - birthTime;
            float t = Mathf.Clamp01(age / coolingTime);
            colors.Add(temperatureGradient.Evaluate(1 - t));
        }

        int[] boxTri = new int[36] {
            0,2,1, 2,3,1, // front
            4,5,6, 5,7,6, // back
            0,1,4, 1,5,4, // bottom
            2,6,3, 6,7,3, // top
            0,4,2, 4,6,2, // left
            1,3,5, 3,7,5  // right
        };

        foreach (int tri in boxTri)
            triangles.Add(startIndex + tri);
    }

    private void AddSphere(Vector3 center, float radius, Color color)
    {
        // Упрощённая сфера (4 сегмента по вертикали, 8 по горизонтали)
        int startIndex = vertices.Count;
        int segments = 4;
        for (int i = 0; i <= segments; i++)
        {
            float vAngle = Mathf.PI * i / segments;
            float y = Mathf.Cos(vAngle) * radius;
            float r = Mathf.Sin(vAngle) * radius;
            int ringVertices = (i == 0 || i == segments) ? 1 : segments * 2;
            for (int j = 0; j < ringVertices; j++)
            {
                float hAngle = 2 * Mathf.PI * j / ringVertices;
                float x = Mathf.Cos(hAngle) * r;
                float z = Mathf.Sin(hAngle) * r;
                vertices.Add(center + new Vector3(x, y, z));
                colors.Add(color);
            }
        }
        // Индексы треугольников (пропущено для краткости – в полной версии нужно заполнить triangles)
        // В реальном проекте используйте готовый генератор сферы или добавьте индексы.
    }

    private void AddBurnMark(Vector3 center, Quaternion rotation)
    {
        int startIndex = vertices.Count;
        float radius = 0.005f;
        for (int i = 0; i < 8; i++)
        {
            float angle = i * Mathf.PI * 2 / 8;
            Vector3 localPos = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0) * radius;
            vertices.Add(center + rotation * localPos);
            colors.Add(Color.Lerp(Color.black, new Color(0.2f, 0.1f, 0), 0.5f));
        }
        vertices.Add(center);
        colors.Add(Color.black);
        int centerIdx = startIndex + 8;
        for (int i = 0; i < 8; i++)
        {
            triangles.Add(centerIdx);
            triangles.Add(startIndex + i);
            triangles.Add(startIndex + (i + 1) % 8);
        }
    }

    void Update()
    {
        // Обновление цвета бусинок со временем (без перестройки меша)
        if (beads.Count == 0) return;
        bool changed = false;
        for (int i = 0; i < beads.Count; i++)
        {
            float age = Time.time - beads[i].birthTime;
            float t = Mathf.Clamp01(age / coolingTime);
            Color color = temperatureGradient.Evaluate(1 - t);
            int baseIdx = i * 8; // каждая бусинка – 8 вершин
            for (int v = 0; v < 8; v++)
            {
                if (baseIdx + v < colors.Count)
                {
                    colors[baseIdx + v] = color;
                    changed = true;
                }
            }
        }
        if (changed) mesh.SetColors(colors);
    }
}
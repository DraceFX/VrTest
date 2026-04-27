using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class LatheControllerVR : MonoBehaviour
{
    [Header("Shape Settings")]
    public int lengthSegments = 120;   // число сегментов по длине
    public int radialSegments = 32;    // число сегментов по углу (теперь важно!)
    public float length = 2f;
    public float initialRadius = 0.5f;
    public float minRadius = 0.05f;

    [Header("Tool")]
    public Transform tool;

    public ToolTypeLathe LatheTool;

    [Header("Performance")]
    public float updateRate = 0.05f;
    public bool updateNormals = true;

    // Двумерный массив радиусов: [длина, угол]
    private float[,] radii;
    private Vector3[] vertices;
    private int[] triangles;

    private Mesh mesh;
    private float halfLen;

    private float timer;
    private bool dirty;
    private bool canBeSplit = true;
    private bool isCutt = false;

    private void Start()
    {
        InitMesh();
        halfLen = length * 0.5f;
        InitRadii();
        BuildTopology();
        GenerateVertices();
        ApplyMesh();
    }

    private void InitRadii()
    {
        radii = new float[lengthSegments, radialSegments];
        for (int i = 0; i < lengthSegments; i++)
            for (int j = 0; j < radialSegments; j++)
                radii[i, j] = initialRadius;
    }

    private void InitMesh()
    {
        mesh = new Mesh { name = "Lathe Mesh" };
        GetComponent<MeshFilter>().mesh = mesh;
    }

    private void Update()
    {
        if (isCutt == false) return;
        
            if (tool != null)
                HandleCut();

            timer += Time.deltaTime;

            if (dirty && timer >= updateRate)
            {
                timer = 0f;
                SmoothRadii();
                GenerateVertices();
                ApplyMesh();
                CheckForCut();
                dirty = false;
            }
        
    }

    public void StartCutting() => isCutt = true;
    public void FinishCutting() => isCutt = false;

    public void CanCutting(bool isCutting)
    {
        if (isCutting)
        {
            StartCutting();
        }
        else
        {
            FinishCutting();
        }
    }

    // --------------- Обработка контакта с инструментом ---------------
    private void HandleCut()
    {
        // Позиция инструмента в локальной системе заготовки
        Vector3 localTool = transform.InverseTransformPoint(tool.position);
        float toolX = localTool.x;

        // Текущий угол инструмента вокруг оси X (в радианах)
        float toolAngle = Mathf.Atan2(localTool.z, localTool.y);

        // Половина угловой ширины в радианах
        float halfAngularRad = LatheTool.contactAngularWidth * 0.5f * Mathf.Deg2Rad;

        for (int i = 0; i < lengthSegments; i++)
        {
            float x = Mathf.Lerp(-halfLen, halfLen, (float)i / (lengthSegments - 1));

            // Быстрая проверка попадания по длине
            if (Mathf.Abs(x - toolX) > LatheTool.toolWidth)
                continue;

            for (int j = 0; j < radialSegments; j++)
            {
                // Угол текущей вершины
                float vertAngle = j * 2f * Mathf.PI / radialSegments;

                // Разница углов (с учётом цикличности)
                float diff = Mathf.Abs(Mathf.DeltaAngle(vertAngle * Mathf.Rad2Deg,
                                                        toolAngle * Mathf.Rad2Deg));
                if (diff > halfAngularRad * Mathf.Rad2Deg)
                    continue;

                // Целевой радиус согласно форме инструмента
                float targetRadius = GetToolRadiusAt(i, x, localTool);

                if (targetRadius < radii[i, j])
                {
                    radii[i, j] = Mathf.Max(targetRadius, minRadius);
                    dirty = true;
                }
            }
        }
    }

    // --------------- Расчёт целевого радиуса (без изменений) ---------------
    private float GetToolRadiusAt(int index, float x, Vector3 toolLocal)
    {
        switch (LatheTool.toolShape)
        {
            case LatheToolShape.Turning:
                return TurningTool(toolLocal);
            case LatheToolShape.CuttOff:
                return CutOff(index, x, toolLocal);
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

    private float CutOff(int index, float x, Vector3 toolLocal)
    {
        float currentRadius = radii[index, 0]; // для проверки берём первый угол (все примерно равны)
        float dx = Mathf.Abs(x - toolLocal.x);
        if (dx > LatheTool.toolWidth)
            return currentRadius;

        float toolDistance = new Vector2(toolLocal.y, toolLocal.z).magnitude;
        if (toolDistance >= currentRadius)
            return currentRadius;

        return Mathf.Max(currentRadius - 0.02f, minRadius);
    }

    private float BallTool(float x, Vector3 toolLocal)
    {
        float r = LatheTool.toolWidth;
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
        float slope = Mathf.Tan(LatheTool.coneAngle * Mathf.Deg2Rad);
        float centerRadius = new Vector2(toolLocal.y, toolLocal.z).magnitude;
        return centerRadius + dx * slope;
    }

    // --------------- Сглаживание двумерного массива ---------------
    private void SmoothRadii(int iterations = 1, float factor = 0.25f)
    {
        for (int it = 0; it < iterations; it++)
        {
            float[,] temp = (float[,])radii.Clone();

            // Сглаживание по длине для каждого угла
            // for (int j = 0; j < radialSegments; j++)
            // {
            //     for (int i = 1; i < lengthSegments - 1; i++)
            //     {
            //         float avg = (radii[i - 1, j] + radii[i, j] + radii[i + 1, j]) / 3f;
            //         // Важно: не даём радиусу увеличиться!
            //         temp[i, j] = Mathf.Min(radii[i, j], Mathf.Lerp(radii[i, j], avg, factor));
            //     }
            // }

            // Сглаживание по углу (мягкое), тоже с ограничением
            // float angularFactor = factor * 0.5f;
            // for (int i = 0; i < lengthSegments; i++)
            // {
            //     for (int j = 0; j < radialSegments; j++)
            //     {
            //         int prevJ = (j - 1 + radialSegments) % radialSegments;
            //         int nextJ = (j + 1) % radialSegments;
            //         float avg = (temp[i, prevJ] + temp[i, j] + temp[i, nextJ]) / 3f;
            //         temp[i, j] = Mathf.Min(temp[i, j], Mathf.Lerp(temp[i, j], avg, angularFactor));
            //     }
            // }

            radii = temp;
        }
    }

    // --------------- Построение сетки ---------------
    private void BuildTopology()
    {
        int baseCount = lengthSegments * radialSegments;
        vertices = new Vector3[baseCount + 2];
        triangles = new int[(lengthSegments - 1) * radialSegments * 6 + radialSegments * 6];

        int leftCenter = baseCount;
        int rightCenter = baseCount + 1;
        int tri = 0;

        // Боковая поверхность
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

        // Левая крышка
        for (int j = 0; j < radialSegments; j++)
        {
            int a = j;
            int b = (j + 1) % radialSegments;
            triangles[tri++] = leftCenter;
            triangles[tri++] = b;
            triangles[tri++] = a;
        }

        // Правая крышка
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

            for (int j = 0; j < radialSegments; j++)
            {
                float angle = j * 2f * Mathf.PI / radialSegments;
                float r = radii[i, j];
                float y = Mathf.Cos(angle) * r;
                float z = Mathf.Sin(angle) * r;
                vertices[i * radialSegments + j] = new Vector3(x, y, z);
            }
        }

        // Центры крышек
        vertices[baseCount] = new Vector3(-halfLen, 0, 0);
        vertices[baseCount + 1] = new Vector3(halfLen, 0, 0);
    }

    private void ApplyMesh()
    {
        if (mesh == null) InitMesh();
        mesh.Clear();
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        if (updateNormals) mesh.RecalculateNormals();
        mesh.RecalculateBounds();
    }

    // --------------- Проверка на отрезку ---------------
    private void CheckForCut()
    {
        for (int i = 2; i < lengthSegments - 2; i++)
        {
            // Проверяем, что ВСЕ радиусы в этом сегменте меньше порога
            bool allThin = true;
            for (int j = 0; j < radialSegments; j++)
            {
                if (radii[i, j] > minRadius * 1.01f)
                {
                    allThin = false;
                    break;
                }
            }
            if (!allThin) continue;

            // Соседи должны быть толще
            bool leftThick = radii[i - 1, 0] > minRadius * 1.5f;
            bool rightThick = radii[i + 1, 0] > minRadius * 1.5f;

            if (leftThick || rightThick)
            {
                SplitMesh(i);
                break;
            }
        }
    }

    private void SplitMesh(int cutIndex)
    {
        if (!canBeSplit) return;

        float cutX = Mathf.Lerp(-halfLen, halfLen, (float)cutIndex / (lengthSegments - 1));

        // Левая часть
        int leftCount = cutIndex + 1;
        float[,] leftRadii = new float[leftCount, radialSegments];
        for (int i = 0; i < leftCount; i++)
            for (int j = 0; j < radialSegments; j++)
                leftRadii[i, j] = radii[i, j];

        // Правая часть
        int rightCount = lengthSegments - cutIndex;
        float[,] rightRadii = new float[rightCount, radialSegments];
        for (int i = 0; i < rightCount; i++)
            for (int j = 0; j < radialSegments; j++)
                rightRadii[i, j] = radii[cutIndex + i, j];

        // Создаём правую деталь
        GameObject rightObj = Instantiate(gameObject, transform.parent);
        LatheControllerVR rightLathe = rightObj.GetComponent<LatheControllerVR>();
        Vector3 cutWorld = transform.TransformPoint(new Vector3(cutX, 0, 0));

        rightLathe.SetupAfterSplit(rightRadii, halfLen - cutX, cutWorld);
        rightLathe.canBeSplit = false;
        rightLathe.enabled = false;
        rightLathe.transform.position += new Vector3(1, 0, 0); // сдвиг для наглядности

        // Обновляем левую часть (этот объект)
        Vector3 leftWorld = transform.TransformPoint(new Vector3((cutX - halfLen) * 0.5f, 0, 0));
        SetupAfterSplit(leftRadii, cutX + halfLen, leftWorld);
    }

    private void SetupAfterSplit(float[,] newRadii, float newLength, Vector3 worldPos)
    {
        transform.position = worldPos;
        InitMesh();

        radii = newRadii;
        lengthSegments = radii.GetLength(0);
        radialSegments = radii.GetLength(1);
        length = newLength;
        halfLen = length * 0.5f;

        BuildTopology();
        GenerateVertices();
        ApplyMesh();
    }

    // --------------- Отладка ---------------
    private void OnDrawGizmosSelected()
    {
        if (tool != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawSphere(tool.position, 0.02f);
        }
        Gizmos.color = Color.cyan;
        DrawWireCylinder(transform.position, length * 0.5f, initialRadius, 32);
    }

    private void DrawWireCylinder(Vector3 center, float halfLength, float radius, int segments)
    {
        Vector3 leftCenter = center + Vector3.left * halfLength;
        Vector3 rightCenter = center + Vector3.right * halfLength;
        Vector3 prevLeft = Vector3.zero, prevRight = Vector3.zero;

        for (int i = 0; i <= segments; i++)
        {
            float angle = (float)i / segments * Mathf.PI * 2f;
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
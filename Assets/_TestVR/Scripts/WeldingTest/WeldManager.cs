using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WeldManager : MonoBehaviour
{
    [Header("Refs")]
    public WeldTorch torch;

    [Header("Tick")]
    [Range(0.01f, 0.05f)]
    public float tickRate = 0.02f; // ~50 Гц

    [Header("Points")]
    public float minPointDistance = 0.003f; // 3 мм
    public int maxPoints = 800;

    [Header("Validation")]
    public float maxAngleDeg = 60f; // угол между нормалью и лучом
    public float minSpeed = 0.0f;   // при желании задать диапазон скорости
    public float maxSpeed = 2.0f;

    [Header("Completion")]
    public int pointsToComplete = 120;

    [Header("Visual: Line")]
    public LineRenderer line;

    [Header("Visual: Beads (optional)")]
    public bool useBeads = false;
    public SimplePool beadPool;
    public float beadScale = 0.004f; // ~4 мм

    private Coroutine routine;

    private readonly List<Vector3> points = new List<Vector3>();
    private readonly List<GameObject> spawnedBeads = new List<GameObject>();

    private Vector3 lastPoint;
    private bool hasLastPoint;
    private Vector3 prevPointForSpeed;
    private bool hasPrevForSpeed;

    private void OnEnable()
    {
        if (torch != null)
            torch.OnWeldStateChanged += HandleWeldState;
    }

    private void OnDisable()
    {
        if (torch != null)
            torch.OnWeldStateChanged -= HandleWeldState;
    }

    private void HandleWeldState(bool active)
    {
        if (active && routine == null)
            routine = StartCoroutine(WeldTick());

        if (!active && routine != null)
        {
            StopCoroutine(routine);
            routine = null;
        }
    }

    private IEnumerator WeldTick()
    {
        var wait = new WaitForSeconds(tickRate);

        while (true)
        {
            ProcessWeld();
            yield return wait;
        }
    }

    private void ProcessWeld()
    {
        var zone = torch.CurrentZone;
        if (zone == null) return;

        if (!torch.TryGetHit(out var hit)) return;

        // Проверка: попали в один из соединяемых объектов
        var rb = hit.rigidbody;
        if (rb == null || (rb != zone.bodyA && rb != zone.bodyB)) return;

        // Проверка угла (горелка должна смотреть примерно на поверхность)
        float angle = Vector3.Angle(-torch.tip.forward, hit.normal);
        if (angle > maxAngleDeg) return;

        // Оценка скорости движения (по точкам)
        Vector3 rawPoint = hit.point;

        // Сглаживание (уменьшает дрожание VR)
        Vector3 point = hasLastPoint ? Vector3.Lerp(lastPoint, rawPoint, 0.3f) : rawPoint;

        if (hasPrevForSpeed)
        {
            float speed = (point - prevPointForSpeed).magnitude / Mathf.Max(tickRate, 1e-5f);
            if (speed < minSpeed || speed > maxSpeed)
            {
                prevPointForSpeed = point;
                return;
            }
        }
        prevPointForSpeed = point;
        hasPrevForSpeed = true;

        // Фильтр по минимальной дистанции
        if (hasLastPoint && Vector3.Distance(point, lastPoint) < minPointDistance)
            return;

        AddPoint(point, hit.normal);

        lastPoint = point;
        hasLastPoint = true;

        UpdateLine();

        TryComplete(zone);
    }

    private void AddPoint(Vector3 pos, Vector3 normal)
    {
        if (points.Count >= maxPoints)
        {
            // Удаляем старые точки (кольцевой буфер)
            points.RemoveAt(0);
            if (useBeads && spawnedBeads.Count > 0)
            {
                var old = spawnedBeads[0];
                spawnedBeads.RemoveAt(0);
                beadPool.Release(old);
            }
        }

        points.Add(pos);

        if (useBeads && beadPool != null)
        {
            var bead = beadPool.Get();
            bead.transform.position = pos;
            bead.transform.rotation = Quaternion.LookRotation(normal);
            bead.transform.localScale = Vector3.one * beadScale;
            spawnedBeads.Add(bead);
        }
    }

    private void UpdateLine()
    {
        if (line == null) return;

        line.positionCount = points.Count;
        line.SetPositions(points.ToArray());
    }

    private void TryComplete(WeldZone zone)
    {
        if (points.Count < pointsToComplete) return;

        // Соединяем тела
        var joint = zone.bodyA.gameObject.AddComponent<FixedJoint>();
        joint.connectedBody = zone.bodyB;

        StopWelding();
    }

    private void StopWelding()
    {
        if (routine != null)
        {
            StopCoroutine(routine);
            routine = null;
        }
    }

    // Опционально: сброс визуала/состояния
    public void ResetWeld()
    {
        points.Clear();
        hasLastPoint = false;
        hasPrevForSpeed = false;

        if (line != null)
        {
            line.positionCount = 0;
        }

        if (useBeads && beadPool != null)
        {
            foreach (var b in spawnedBeads)
                beadPool.Release(b);
            spawnedBeads.Clear();
        }
    }
}
using UnityEngine;

public class BeadWeldGenerator : MonoBehaviour
{
    [Header("Bead settings")]
    public GameObject beadPrefab;

    public float spacing = 0.004f;     // расстояние между "чешуйками"
    public float width = 0.006f;
    public float height = 0.003f;
    public float length = 0.008f;

    [Header("Randomness")]
    public float rotationJitter = 15f;
    public float scaleJitter = 0.2f;

    private Vector3 lastPoint;
    private Vector3 lastForward;
    private bool hasLastPoint = false;

    public void AddPoint(Vector3 worldPoint, Vector3 normal)
    {
        if (!hasLastPoint)
        {
            lastPoint = worldPoint;
            hasLastPoint = true;
            return;
        }

        float dist = Vector3.Distance(lastPoint, worldPoint);
        if (dist < spacing)
            return;

        Vector3 forward = (worldPoint - lastPoint).normalized;

        CreateBead(worldPoint, forward, normal);

        lastPoint = worldPoint;
        lastForward = forward;
    }

    private void CreateBead(Vector3 point, Vector3 forward, Vector3 normal)
    {
        GameObject bead = Instantiate(beadPrefab, transform);

        // Позиция
        bead.transform.position = point;

        // Ориентация вдоль шва
        Quaternion rot = Quaternion.LookRotation(forward, normal);

        // Случайный поворот (эффект сварки)
        rot *= Quaternion.AngleAxis(Random.Range(-rotationJitter, rotationJitter), forward);

        bead.transform.rotation = rot;

        // Рандомный масштаб
        float scaleNoise = 1f + Random.Range(-scaleJitter, scaleJitter);

        bead.transform.localScale = new Vector3(
            width * scaleNoise,
            height * scaleNoise,
            length * scaleNoise
        );
    }

    public void ResetWeld()
    {
        hasLastPoint = false;

        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            Destroy(transform.GetChild(i).gameObject);
        }
    }
}
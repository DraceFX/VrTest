using System.Collections.Generic;
using UnityEngine;

public class WeldMeshBuilder : MonoBehaviour
{
    [Header("Prefabs")]
    [SerializeField] private GameObject _weldBeadPrefab;
    [SerializeField] private GameObject _porePrefab;
    [SerializeField] private GameObject _burnPrefab;
    [SerializeField] private GameObject _spatterPrefab;

    [Header("Weld Settings")]
    public float Spacing = 0.004f;

    [Header("Combine")]
    [SerializeField] private bool _combineOnFinish = true;

    private readonly List<MeshFilter> _spawnedMeshes = new();

    private Vector3 _previousPoint;
    private bool _hasPreviousPoint;

    // =====================================================
    // ОСНОВНОЙ ШОВ
    // =====================================================

    public void AddBead(Vector3 worldPoint, Vector3 normal)
    {
        if (_hasPreviousPoint)
        {
            float dist = Vector3.Distance(_previousPoint, worldPoint);

            if (dist < Spacing) return;
        }

        Vector3 direction = _hasPreviousPoint ? (worldPoint - _previousPoint).normalized : transform.forward;

        if (direction == Vector3.zero)
            direction = transform.forward;

        Quaternion rotation = Quaternion.LookRotation(direction, normal);

        if (!TryProjectToSurface(worldPoint, normal, out RaycastHit hit))
        {
            return;
        }

        GameObject obj = Instantiate(_weldBeadPrefab, hit.point + hit.normal * 0.0003f, rotation, transform);

        float randomScale =
            Random.Range(0.95f, 1.05f);

        obj.transform.localScale *= randomScale;

        _previousPoint = hit.point;
        _hasPreviousPoint = true;

        RegisterMesh(obj);
    }

    // =====================================================
    // ПРОЖОГ
    // =====================================================

    public void AddBurn(Vector3 point, Vector3 normal)
    {
        if (!TryProjectToSurface(point, normal, out RaycastHit hit))
        {
            return;
        }

        Quaternion rot = Quaternion.FromToRotation(-Vector3.forward, hit.normal);

        GameObject obj = Instantiate(_burnPrefab, hit.point + hit.normal * 0.0005f, rot, transform);

        obj.transform.localScale *= Random.Range(0.8f, 1.2f);

        RegisterMesh(obj);
    }

    // =====================================================
    // ПОРЫ
    // =====================================================

    public void AddPore(Vector3 point, Vector3 normal)
    {
        if (!TryProjectToSurface(point, normal, out RaycastHit hit))
        {
            return;
        }

        Quaternion rot = Quaternion.LookRotation(hit.normal);

        GameObject obj = Instantiate(_porePrefab, hit.point + hit.normal * 0.0002f, rot, transform);

        obj.transform.localScale *= Random.Range(0.7f, 1.2f);

        RegisterMesh(obj);
    }

    // =====================================================
    // БРЫЗГИ / БУСИНКИ
    // =====================================================

    public void AddSpatter(Vector3 point, Vector3 normal)
    {
        Vector3 randomDir = Random.insideUnitSphere;

        Vector3 tangent = Vector3.ProjectOnPlane(randomDir, normal).normalized;

        if (tangent == Vector3.zero)
            tangent = Vector3.right;

        Vector3 offset = tangent * Random.Range(0.003f, 0.02f);

        Vector3 candidate = point + offset + normal * 0.01f;

        if (!Physics.Raycast(candidate, -normal, out RaycastHit hit, 0.03f))
        {
            return;
        }

        Quaternion rot = Quaternion.LookRotation(tangent, hit.normal);

        GameObject obj = Instantiate(_spatterPrefab, hit.point + hit.normal * 0.0005f, rot, transform);

        obj.transform.localScale *= Random.Range(0.5f, 1.3f);

        RegisterMesh(obj);
    }

    // =====================================================
    // SURFACE PROJECTION
    // =====================================================

    private bool TryProjectToSurface(Vector3 point, Vector3 normal, out RaycastHit hit)
    {
        Vector3 start = point + normal * 0.02f;

        return Physics.Raycast(start, -normal, out hit, 0.05f);
    }

    // =====================================================
    // REGISTRATION
    // =====================================================

    private void RegisterMesh(GameObject obj)
    {
        MeshFilter mf = obj.GetComponent<MeshFilter>();

        if (mf != null)
        {
            _spawnedMeshes.Add(mf);
        }
    }

    // =====================================================
    // COMBINE
    // =====================================================

    public void CombineAll()
    {
        MeshFilter[] meshFilters = GetComponentsInChildren<MeshFilter>();

        List<CombineInstance> combine = new List<CombineInstance>();

        Material sharedMaterial = null;

        foreach (MeshFilter mf in meshFilters)
        {
            if (mf.transform == transform)
                continue;

            if (mf.sharedMesh == null)
                continue;

            MeshRenderer mr = mf.GetComponent<MeshRenderer>();

            if (sharedMaterial == null && mr != null)
            {
                sharedMaterial = mr.sharedMaterial;
            }

            CombineInstance ci = new CombineInstance();

            ci.mesh = mf.sharedMesh;

            ci.transform = transform.worldToLocalMatrix * mf.transform.localToWorldMatrix;

            combine.Add(ci);
        }

        if (combine.Count == 0)
            return;

        Mesh combinedMesh = new Mesh();

        combinedMesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;

        combinedMesh.CombineMeshes(combine.ToArray(), true, true);

        MeshFilter rootMF = GetComponent<MeshFilter>();

        if (rootMF == null)
            rootMF = gameObject.AddComponent<MeshFilter>();

        MeshRenderer rootMR = GetComponent<MeshRenderer>();

        if (rootMR == null)
            rootMR = gameObject.AddComponent<MeshRenderer>();

        rootMF.sharedMesh = combinedMesh;

        if (sharedMaterial != null)
        {
            rootMR.sharedMaterial = sharedMaterial;
        }

        // Удаляем только дочерние объекты
        List<GameObject> toDestroy = new List<GameObject>();

        foreach (Transform child in transform)
        {
            toDestroy.Add(child.gameObject);
        }

        foreach (GameObject obj in toDestroy)
        {
            Destroy(obj);
        }
    }

    // =====================================================
    // RESET
    // =====================================================

    public void ResetPath()
    {
        _hasPreviousPoint = false;
    }
}
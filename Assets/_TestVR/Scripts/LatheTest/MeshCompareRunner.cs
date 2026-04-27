using TMPro;
using UnityEngine;

public class MeshCompareRunner : MonoBehaviour
{
    public MeshFilter referenceMF;
    public MeshFilter currentMF;

    public MeshCollider referenceCol;
    public MeshCollider currentCol;

    [Header("Settings")]
    public float tolerance = 0.005f;
    public int step = 2;
    public float overcutPenalty = 2f;

    [Header("Debug")]
    public TMP_Text _text;

    private void Start()
    {
        PrepareCollider(referenceMF, referenceCol);
        PrepareCollider(currentMF, currentCol);
    }

    public void PressedButton()
    {
        referenceCol.sharedMesh = null;
        referenceCol.sharedMesh = referenceMF.sharedMesh;

        currentCol.sharedMesh = null;
        currentCol.sharedMesh = currentMF.sharedMesh;

        float percent = CompareNow();
        _text.text = $"Совпадение: {percent:F2}%";
        //Debug.Log($"Совпадение: {percent:F2}%");
    }

    private void PrepareCollider(MeshFilter mf, MeshCollider col)
    {
        col.convex = false;
        col.sharedMesh = null;
        col.sharedMesh = mf.sharedMesh;
        col.enabled = false;
    }

    public float CompareNow()
    {
        float errorA = ComputeOneSide(currentMF, referenceCol);
        float errorB = ComputeOneSide(referenceMF, currentCol);

        float avgError = (errorA + errorB) * 0.5f;
        return (1f - avgError) * 100f;
    }
    float ComputeOneSide(MeshFilter source, MeshCollider targetCol)
    {
        var mesh = source.sharedMesh;
        var verts = mesh.vertices;
        var normals = mesh.normals;
        var tr = source.transform;

        float total = 0f;
        int count = 0;

        for (int i = 0; i < verts.Length; i += step)
        {
            Vector3 world = tr.TransformPoint(verts[i]);
            Vector3 closest = targetCol.ClosestPoint(world);

            float dist = Vector3.Distance(world, closest);

            float norm = Mathf.Clamp01(dist / tolerance);

            // штраф за перерезание
            if (normals != null && normals.Length == verts.Length)
            {
                Vector3 worldNormal = tr.TransformDirection(normals[i]);
                Vector3 dir = (closest - world).normalized;

                if (Vector3.Dot(worldNormal, dir) < 0f)
                {
                    norm *= overcutPenalty;
                    norm = Mathf.Clamp01(norm);
                }
            }

            total += norm;
            count++;
        }

        return count > 0 ? total / count : 1f;
    }
}

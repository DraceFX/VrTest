using System.Threading.Tasks;
using TMPro;
using UnityEngine;

public class MeshCompareUniversal : MonoBehaviour
{
    public GameObject objA;
    public GameObject objB;

    public float tolerance = 0.0001f;

    public TMP_Text text;

    public async void Compare()
    {
        float similarity = await CompareAsync(objA, objB, tolerance);
        text.text = "Mesh similarity: " + similarity.ToString("F2") + "%";
        Debug.Log("Mesh similarity: " + similarity.ToString("F2") + "%");
    }

    public async Task<float> CompareAsync(GameObject a, GameObject b, float tolerance)
    {
        // --- 1. Забираем данные на главном потоке ---
        MeshFilter mfA = a.GetComponent<MeshFilter>();
        MeshFilter mfB = b.GetComponent<MeshFilter>();

        if (mfA == null || mfB == null)
            return 0f;

        Mesh meshA = mfA.sharedMesh;
        Mesh meshB = mfB.sharedMesh;

        if (meshA == null || meshB == null)
            return 0f;

        var vA = meshA.vertices;
        var vB = meshB.vertices;
        var tA = meshA.triangles;
        var tB = meshB.triangles;
        var nA = meshA.normals;
        var nB = meshB.normals;
        var uvA = meshA.uv;
        var uvB = meshB.uv;

        // --- 2. Считаем в фоне ---
        return await Task.Run(() =>
        {
            float sqrTol = tolerance * tolerance;

            int total = 0;
            int matched = 0;

            int vCount = Mathf.Min(vA.Length, vB.Length);
            for (int i = 0; i < vCount; i++)
            {
                total++;
                if ((vA[i] - vB[i]).sqrMagnitude <= sqrTol)
                    matched++;
            }

            total += Mathf.Abs(vA.Length - vB.Length);

            int tCount = Mathf.Min(tA.Length, tB.Length);
            for (int i = 0; i < tCount; i++)
            {
                total++;
                if (tA[i] == tB[i])
                    matched++;
            }

            total += Mathf.Abs(tA.Length - tB.Length);

            if (total == 0) return 0f;
            return (matched / (float)total) * 100f;
        });
    }

    // public void Compare()
    // {
    //     float similarity = CalculateSimilarity(objA, objB, tolerance);
    //     text.text = "Mesh similarity: " + similarity.ToString("F2") + "%";
    //     Debug.Log("Mesh similarity: " + similarity.ToString("F2") + "%");
    // }

    // public static float CalculateSimilarity(GameObject a, GameObject b, float tolerance)
    // {
    //     MeshFilter mfA = a.GetComponent<MeshFilter>();
    //     MeshFilter mfB = b.GetComponent<MeshFilter>();

    //     if (mfA == null || mfB == null)
    //         return 0f;

    //     Mesh meshA = mfA.sharedMesh;
    //     Mesh meshB = mfB.sharedMesh;

    //     if (meshA == null || meshB == null)
    //         return 0f;

    //     float sqrTol = tolerance * tolerance;

    //     int totalChecks = 0;
    //     int matched = 0;

    //     // --- ВЕРШИНЫ ---
    //     var vA = meshA.vertices;
    //     var vB = meshB.vertices;

    //     int vCount = Mathf.Min(vA.Length, vB.Length);

    //     for (int i = 0; i < vCount; i++)
    //     {
    //         totalChecks++;
    //         if ((vA[i] - vB[i]).sqrMagnitude <= sqrTol)
    //             matched++;
    //     }

    //     // штраф за разное количество вершин
    //     totalChecks += Mathf.Abs(vA.Length - vB.Length);

    //     // --- ТРЕУГОЛЬНИКИ ---
    //     var tA = meshA.triangles;
    //     var tB = meshB.triangles;

    //     int tCount = Mathf.Min(tA.Length, tB.Length);

    //     for (int i = 0; i < tCount; i++)
    //     {
    //         totalChecks++;
    //         if (tA[i] == tB[i])
    //             matched++;
    //     }

    //     totalChecks += Mathf.Abs(tA.Length - tB.Length);

    //     // --- НОРМАЛИ ---
    //     var nA = meshA.normals;
    //     var nB = meshB.normals;

    //     int nCount = Mathf.Min(nA.Length, nB.Length);

    //     for (int i = 0; i < nCount; i++)
    //     {
    //         totalChecks++;
    //         if ((nA[i] - nB[i]).sqrMagnitude <= sqrTol)
    //             matched++;
    //     }

    //     totalChecks += Mathf.Abs(nA.Length - nB.Length);

    //     // --- UV ---
    //     var uvA = meshA.uv;
    //     var uvB = meshB.uv;

    //     int uvCount = Mathf.Min(uvA.Length, uvB.Length);

    //     for (int i = 0; i < uvCount; i++)
    //     {
    //         totalChecks++;
    //         if ((uvA[i] - uvB[i]).sqrMagnitude <= sqrTol)
    //             matched++;
    //     }

    //     totalChecks += Mathf.Abs(uvA.Length - uvB.Length);

    //     if (totalChecks == 0)
    //         return 0f;

    //     return (matched / (float)totalChecks) * 100f;
    // }
}
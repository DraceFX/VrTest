using UnityEngine;

public static class MeshComparer
{
    public static float Compare(
        MeshFilter current,
        MeshFilter reference,
        MeshCollider referenceCollider,
        MeshCollider currentCollider,
        float tolerance = 0.01f,
        int step = 1,
        float overcutPenalty = 2f
    )
    {
        float errorA = ComputeOneSide(
            current, referenceCollider, tolerance, step, overcutPenalty);

        float errorB = ComputeOneSide(
            reference, currentCollider, tolerance, step, overcutPenalty);

        float avgError = (errorA + errorB) * 0.5f;
        return (1f - avgError) * 100f;
    }

    private static float ComputeOneSide(
        MeshFilter source,
        MeshCollider targetCollider,
        float tolerance,
        int step,
        float overcutPenalty
    )
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
            Vector3 closest = targetCollider.ClosestPoint(world);

            float dist = Vector3.Distance(world, closest);

            // Нормализованная ошибка
            float norm = Mathf.Clamp01(dist / tolerance);

            // --- попытка определить "перерезание"
            if (normals != null && normals.Length == verts.Length)
            {
                Vector3 worldNormal = tr.TransformDirection(normals[i]);
                Vector3 dir = (closest - world).normalized;

                float dot = Vector3.Dot(worldNormal, dir);

                // если точка "внутри" эталона → перерезание
                if (dot < 0f)
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
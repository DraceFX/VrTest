using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
public class MeshCombiner : MonoBehaviour
{
    private void Awake()
    {
        CombineMeshes();
    }

    private void CombineMeshes()
    {
        // 1. Собираем дочерние MeshFilter и MeshRenderer
        MeshFilter[] childFilters = GetComponentsInChildren<MeshFilter>(true);
        // Исключаем самого себя, чтобы не скомбинировать собственный (пустой/старый) меш
        List<MeshFilter> validFilters = new List<MeshFilter>();
        foreach (var mf in childFilters)
        {
            if (mf.gameObject == gameObject) continue;
            if (mf.sharedMesh == null) continue;
            validFilters.Add(mf);
        }

        if (validFilters.Count == 0)
        {
            Debug.LogWarning("MeshCombiner: не найдено дочерних MeshFilter с мешами.");
            return;
        }

        // 2. Группируем подмеши по материалу
        // Ключ – Material, значение – список CombineInstance для этого материала
        Dictionary<Material, List<CombineInstance>> materialGroups = new Dictionary<Material, List<CombineInstance>>();

        foreach (var mf in validFilters)
        {
            MeshRenderer mr = mf.GetComponent<MeshRenderer>();
            if (mr == null || mr.sharedMaterials.Length == 0)
                continue; // пропускаем объекты без материалов (можно назначить дефолтный, но здесь пропускаем)

            Mesh mesh = mf.sharedMesh;
            Matrix4x4 localToParent = transform.worldToLocalMatrix * mf.transform.localToWorldMatrix;

            for (int subMeshIndex = 0; subMeshIndex < mesh.subMeshCount; subMeshIndex++)
            {
                // Берём материал для этого подмеша (если в массиве меньше элементов – null)
                Material mat = subMeshIndex < mr.sharedMaterials.Length ? mr.sharedMaterials[subMeshIndex] : null;
                if (mat == null) continue; // игнорируем подмеши без материала

                if (!materialGroups.ContainsKey(mat))
                    materialGroups[mat] = new List<CombineInstance>();

                materialGroups[mat].Add(new CombineInstance
                {
                    mesh = mesh,
                    subMeshIndex = subMeshIndex,
                    transform = localToParent
                });
            }

            // Отключаем дочерний объект, он больше не нужен
            mf.gameObject.SetActive(false);
        }

        // 3. Для каждой группы материала создаём временный меш (один подмеш)
        List<Mesh> tempMeshes = new List<Mesh>();
        List<Material> finalMaterials = new List<Material>();

        foreach (var kvp in materialGroups)
        {
            Mesh tempMesh = new Mesh();
            // true – объединяем все подмеши группы в один
            tempMesh.CombineMeshes(kvp.Value.ToArray(), true, true);
            tempMeshes.Add(tempMesh);
            finalMaterials.Add(kvp.Key);
        }

        // 4. Собираем итоговый меш из временных: каждый временный меш станет отдельным подмешем
        Mesh finalMesh = new Mesh();
        CombineInstance[] finalCombines = new CombineInstance[tempMeshes.Count];
        for (int i = 0; i < tempMeshes.Count; i++)
        {
            finalCombines[i] = new CombineInstance
            {
                mesh = tempMeshes[i],
                subMeshIndex = 0,
                transform = Matrix4x4.identity
            };
        }
        // false – не сливаем подмеши, каждый CombineInstance = отдельный подмеш
        finalMesh.CombineMeshes(finalCombines, false, false);

        // 5. Назначаем результат на родительский объект
        MeshFilter parentFilter = GetComponent<MeshFilter>();
        parentFilter.mesh = finalMesh;

        MeshRenderer parentRenderer = GetComponent<MeshRenderer>();
        parentRenderer.sharedMaterials = finalMaterials.ToArray();

        // 6. Удаляем временные меши (освобождаем память)
        foreach (var tempMesh in tempMeshes)
        {
            Destroy(tempMesh);
        }

        // Родительский объект уже был активен, дополнительно включать не нужно
    }
}

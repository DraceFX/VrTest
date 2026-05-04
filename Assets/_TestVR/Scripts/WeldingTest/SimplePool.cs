using System.Collections.Generic;
using UnityEngine;

public class SimplePool : MonoBehaviour
{
    public GameObject prefab;
    public int prewarm = 64;

    private readonly Queue<GameObject> pool = new Queue<GameObject>();

    private void Awake()
    {
        for (int i = 0; i < prewarm; i++)
            Create();
    }

    private GameObject Create()
    {
        var go = Instantiate(prefab, transform);
        go.SetActive(false);
        pool.Enqueue(go);
        return go;
    }

    public GameObject Get()
    {
        if (pool.Count == 0)
            Create();

        var go = pool.Dequeue();
        go.SetActive(true);
        return go;
    }

    public void Release(GameObject go)
    {
        go.SetActive(false);
        pool.Enqueue(go);
    }
}
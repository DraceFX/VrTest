using System.Collections.Generic;
using UnityEngine;

public class RandomActivateObjects : MonoBehaviour
{
    [SerializeField] private int _countEnableObj = 10;
    public List<GameObject> _objects;

    private void Awake()
    {
        int count = Mathf.Min(_countEnableObj, _objects.Count);

        foreach (var obj in _objects)
        {
            obj.SetActive(false);
        }

        List<GameObject> pool = new List<GameObject>(_objects);

        for (int i = 0; i < count; i++)
        {
            int index = Random.Range(0, pool.Count);

            pool[index].SetActive(true);
            pool.RemoveAt(index);
        }
    }
}

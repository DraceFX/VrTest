using System.Collections.Generic;
using UnityEngine;

public class TargetRegistry : MonoBehaviour
{
    private Dictionary<string, GameObject> _targets = new Dictionary<string, GameObject>();

    public static TargetRegistry Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        BuildRegistry();
    }

    private void BuildRegistry()
    {
        _targets.Clear();
        var identifiers = FindObjectsByType<StepTargetIdentifier>(FindObjectsSortMode.None);

        foreach (var id in identifiers)
        {
            if (_targets.ContainsKey(id.TargetId))
            {
                Debug.LogWarning($"Дубликат TargetId '{id.TargetId}' на объектах {_targets[id.TargetId].name} и {id.gameObject.name}. Будет использоваться первый.");
                continue;
            }
            _targets.Add(id.TargetId, id.gameObject);
        }
    }

    public GameObject GetTarget(string id)
    {
        if (string.IsNullOrEmpty(id))
            return null;

        _targets.TryGetValue(id, out var go);
        return go;
    }
}

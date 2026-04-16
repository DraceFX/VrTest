using System;
using System.Collections.Generic;
using UnityEngine;

public class ObjectTriggerManager : MonoBehaviour
{
    public List<ObjectTrigger> ObjectsAndTriggers = new List<ObjectTrigger>();

    private static HashSet<string> _generatedIds = new HashSet<string>();

    private void Awake()
    {
        GenerateId();
    }

    private void GenerateId()
    {
        foreach (var entry in ObjectsAndTriggers)
        {
            if (entry == null) continue;

            if (!string.IsNullOrEmpty(entry.Id))
            {
                string newId = RegisterId(entry.Id);
                SetId(entry, newId);
            }
            else
            {
                string newId = GenerateUniqueId();
                entry.Id = newId;

                SetId(entry, newId);
            }
        }
    }

    private string GenerateUniqueId()
    {
        string newId;
        do
        {
            newId = Guid.NewGuid().ToString("N");
        } while (_generatedIds.Contains(newId));

        _generatedIds.Add(newId);
        return newId;
    }

    private string RegisterId(string id)
    {
        if (!string.IsNullOrEmpty(id) && !_generatedIds.Contains(id))
        {
            _generatedIds.Add(id);
        }
        return id;
    }

    private void SetId(ObjectTrigger obj, string id)
    {
        if (obj.Object is ITriggerEnter objInterface)
        {
            objInterface.Id = id;
        }

        if (obj.Trigger is IObjectEnter triggerInterface)
        {
            triggerInterface.Id = id;
        }
    }
}

[Serializable]
public class ObjectTrigger
{
    public InteractableObject Object;
    public InteractableTrigger Trigger;
    public string Id;
}

[Flags]
public enum UseCondition
{
    None = 0,
    ReleaseGrab = 1 << 0,
    TriggerPress = 1 << 1,
    AutoSnap = 1 << 2
}

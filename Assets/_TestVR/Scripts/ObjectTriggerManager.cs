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
                if (entry.Object is ITriggerEnter objInterface)
                {
                    objInterface.Id = newId;
                }

                if (entry.Trigger is IObjectEnter triggerInterface)
                {
                    triggerInterface.Id = newId;
                }
            }
            else
            {
                string newId = GenerateUniqueId();
                entry.Id = newId;

                if (entry.Object is ITriggerEnter objInterface)
                {
                    objInterface.Id = newId;
                }

                if (entry.Trigger is IObjectEnter triggerInterface)
                {
                    triggerInterface.Id = newId;
                }
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
}

[Serializable]
public class ObjectTrigger
{
    public InteractableObject Object;
    public InteractableTrigger Trigger;
    public string Id;
}

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class WeldingUI : MonoBehaviour
{
    [SerializeField] private List<PPEToggle> _toggles;

    private void Start()
    {
        InteractionManager.Instance.OnObjectUsed += HandleObjectUsed;
    }

    private void HandleObjectUsed(InteractableTrigger trigger)
    {
        if (trigger == null) return;

        foreach (var toggle in _toggles)
        {
            if (toggle.Id == trigger.Id)
            {
                toggle.TogglePPE.isOn = true;
            }
        }
    }
}

[Serializable]
public class PPEToggle
{
    public string Id;
    public Toggle TogglePPE;
}
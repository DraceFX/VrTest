using UnityEngine;

public class OutlineServiceAdapter : MonoBehaviour, IOutlineService
{
    public void HideOutline(GameObject target)
    {
        var outline = target.GetComponent<Outline>();
        if (outline != null)
            outline.enabled = false;
    }

    public void ShowOutLine(GameObject target)
    {
        var outline = target.GetComponent<Outline>();
        if (outline != null)
            outline.enabled = true;
    }
}

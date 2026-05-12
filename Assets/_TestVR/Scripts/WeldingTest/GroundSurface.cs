using UnityEngine;

public class GroundSurface : MonoBehaviour
{
    public bool IsActive { get; private set; }

    public void Activate()
    {
        if (!IsActive)
        {
            IsActive = true;
            GroundManager.NotifyGroundingChanged();
        }
    }

    public void Deactivate()
    {
        if (IsActive)
        {
            IsActive = false;
            GroundManager.NotifyGroundingChanged();
        }
    }
}
using UnityEngine;

public interface IWeldSessionManager
{
    public void StartNewWeld(Weldable a, Weldable b, Vector3 startPoint, Vector3 normal, Vector3 forward);
    public void SetSecondTarget(Weldable b);
    public void FinishWeld();
    public WeldMeshBuilder ActiveBuilder { get; }
    public Weldable ActiveTargetB { get; }
}

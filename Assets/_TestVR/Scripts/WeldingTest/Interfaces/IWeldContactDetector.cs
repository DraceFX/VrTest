using UnityEngine;

public interface IWeldContactDetector
{
    public bool Evaluate(IWeldingTool tool, out RaycastHit hit);
    public WeldProcessModel ProcessModel { get; }
    public Weldable TargetA { get; }
    public Weldable TargetB { get; }
    public void ResetTargets();
}

using UnityEngine;

public interface IWeldTrajectoryEvaluator
{
    public void Initialize(Vector3 startPoint, Vector3 normal, Vector3 forward);
    public void UpdateTracking(Vector3 tipPosition);
    public void Reset();
    public float TrajectoryQuality { get; }
    public float WeaveAmplitude { get; }
    public string CurrentPattern { get; }
    public float WeaveFrequency { get; }
    public float ComputeIdealOffset(float x);
}

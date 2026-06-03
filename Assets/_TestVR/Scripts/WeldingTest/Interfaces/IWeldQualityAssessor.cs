using UnityEngine;

public interface IWeldQualityAssessor
{
    public void StartAssessment();
    public void StopAssessment();
    public void RegisterDefect();
    public float OverallQuality { get; }
    public void UpdateAssessment(float currentPower, float optimalPower, Vector3 electrodeTipPos, float arcDistance, float idealArcDistance, float currentWidth);
}

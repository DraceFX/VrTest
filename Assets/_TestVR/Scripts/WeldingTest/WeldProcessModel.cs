using UnityEngine;

[System.Serializable]
public class WeldProcessModel
{
    [Header("Tuning")]
    public float optimalPower = 2500f;

    public float meltSpeed = 0.002f;     // скорость сгорания электрода
    public float depositRate = 0.003f;   // скорость наплавки
    public float burnThreshold = 4000f;  // прожог

    public float EvaluateMelt(float power)
    {
        return meltSpeed * (power / optimalPower);
    }

    public float EvaluateDeposit(float power)
    {
        float efficiency = Mathf.Clamp01(power / optimalPower);
        return depositRate * efficiency;
    }

    public bool IsBurning(float power)
    {
        return power > burnThreshold;
    }
}
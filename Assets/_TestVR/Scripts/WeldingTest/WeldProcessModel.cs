using UnityEngine;

[System.Serializable]
public class WeldProcessModel
{
    [Header("Tuning")]
    public float OptimalPower = 2500f;

    public float MeltSpeed = 0.002f;     // скорость сгорания электрода
    public float DepositRate = 0.003f;   // скорость наплавки
    public float BurnThreshold = 4000f;  // прожог

    public float EvaluateMelt(float power)
    {
        return MeltSpeed * (power / OptimalPower);
    }

    public float EvaluateDeposit(float power)
    {
        float efficiency = Mathf.Clamp01(power / OptimalPower);
        return DepositRate * efficiency;
    }

    public float EvaluateQuality(float power)
    {
        float delta = Mathf.Abs(power - OptimalPower) / OptimalPower;

        return 1f - Mathf.Clamp01(delta);
    }

    public bool IsBurning(float power)
    {
        return power > BurnThreshold;
    }
}
using UnityEngine;

public class WeldingSettings : MonoBehaviour, IPowerSource
{
    [Header("Electrical")]
    [Range(10f, 300f)][SerializeField] private float current = 120f;

    [Range(10f, 40f)][SerializeField] private float voltage = 24f;

    public float Power => current * voltage;

    public float Current => current;
    public float Voltage => voltage;

    public void SetCurrent(float value)
    {
        current = value;
    }

    public void SetVoltage(float value)
    {
        voltage = value;
    }
}
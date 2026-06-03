using UnityEngine;

public interface IPowerSource
{
    public float Current { get; }
    public float Voltage { get; }
    public float Power { get; }
    public void SetCurrent(float value);
    public void SetVoltage(float value);
}

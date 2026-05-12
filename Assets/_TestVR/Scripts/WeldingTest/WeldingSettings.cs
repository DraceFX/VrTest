using UnityEngine;

public class WeldingSettings : MonoBehaviour
{
    [Header("Electrical")]
    [Range(10f, 300f)]
    public float current = 120f;   // Амперы

    [Range(10f, 40f)]
    public float voltage = 24f;    // Вольты

    [Header("Derived")]
    public float Power => current * voltage; // Ватты

    public void OnCurrentChanged(float current)
    {
        Debug.Log("Вольт: " + current);
        this.current = current;
    }

    public void OnVoltageChanged(float voltage)
    {
        Debug.Log("Вольт: " + voltage);
        this.voltage = voltage;
    }
}
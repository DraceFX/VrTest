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
}
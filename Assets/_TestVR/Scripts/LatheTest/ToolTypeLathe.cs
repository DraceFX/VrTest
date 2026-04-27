using UnityEngine;

[CreateAssetMenu(fileName = "ToolType", menuName = "Lathe/LatheTool", order = 1)]
public class ToolTypeLathe : ScriptableObject
{
    public LatheToolShape toolShape = LatheToolShape.Turning;

    [Range(1f, 89f)]
    public float coneAngle = 45f;
    public float toolWidth = 0.05f;

    [Header("Contact (non-axisymmetric)")]
    [Tooltip("Угловая ширина зоны контакта инструмента (градусы). " +
             "При значении 360 поведение похоже на оригинал, но всё равно идёт через всю окружность.")]
    public float contactAngularWidth = 10f;
}
public enum LatheToolShape
{
    Turning,
    Ball,
    Cone,
    CuttOff
}

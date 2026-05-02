using UnityEngine;

[CreateAssetMenu(fileName = "ToolType", menuName = "Lathe/LatheTool", order = 1)]
public class ToolTypeLathe : ScriptableObject
{
    public LatheToolShape toolShape = LatheToolShape.Turning;

    [Range(1f, 89f)]
    public float coneAngle = 45f;
    public float toolWidth = 0.05f;

    [Header("Contact (non-axisymmetric)")]
    [Tooltip("������� ������ ���� �������� ����������� (�������). " +
             "��� �������� 360 ��������� ������ �� ��������, �� �� ����� ��� ����� ��� ����������.")]
    public float contactAngularWidth = 10f;

    public float GetToolRadiusAt(int index, float x, Vector3 toolLocal, float currentRadius, float initialRadius, float minRadius)
    {
        switch (toolShape)
        {
            case LatheToolShape.Turning:
                return TurningTool(toolLocal);

            case LatheToolShape.CuttOff:
                return CutOff(index, x, toolLocal, currentRadius, minRadius);

            case LatheToolShape.Ball:
                return BallTool(x, toolLocal, initialRadius);

            case LatheToolShape.Cone:
                return ConeTool(x, toolLocal);

            default:
                return initialRadius;
        }
    }

    private float TurningTool(Vector3 toolLocal)
    {
        return new Vector2(toolLocal.y, toolLocal.z).magnitude;
    }

    private float CutOff(int index, float x, Vector3 toolLocal, float currentRadius, float minRadius)
    {
        float dx = Mathf.Abs(x - toolLocal.x);

        if (dx > toolWidth)
            return currentRadius;

        float toolDistance = new Vector2(toolLocal.y, toolLocal.z).magnitude;

        if (toolDistance >= currentRadius)
            return currentRadius;

        return Mathf.Max(currentRadius - 0.02f, minRadius);
    }

    private float BallTool(float x, Vector3 toolLocal, float initialRadius)
    {
        float r = toolWidth;
        float dx = x - toolLocal.x;

        if (Mathf.Abs(dx) > r)
            return initialRadius;

        float radialOffset = Mathf.Sqrt(r * r - dx * dx);
        float centerRadius = new Vector2(toolLocal.y, toolLocal.z).magnitude;

        return centerRadius - radialOffset;
    }

    private float ConeTool(float x, Vector3 toolLocal)
    {
        float dx = Mathf.Abs(x - toolLocal.x);
        float slope = Mathf.Tan(coneAngle * Mathf.Deg2Rad);
        float centerRadius = new Vector2(toolLocal.y, toolLocal.z).magnitude;

        return centerRadius + dx * slope;
    }
}
public enum LatheToolShape
{
    Turning,
    Ball,
    Cone,
    CuttOff
}

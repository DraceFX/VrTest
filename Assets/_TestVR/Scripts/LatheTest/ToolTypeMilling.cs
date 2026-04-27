using UnityEngine;

[CreateAssetMenu(fileName = "ToolType", menuName = "Milling/MillingTool", order = 1)]
public class ToolTypeMilling : ScriptableObject
{
    public ToolShape toolShape = ToolShape.Flat;

    [Range(1f, 89f)]
    public float coneAngle = 45f; // для конуса

    [Header("Cut Settings")]
    public float cutStrength = 0.01f;
    public float toolRadius = 0.05f;

    public float GetToolHeight(float toolY, float distance)
    {
        switch (toolShape)
        {
            case ToolShape.Flat:
                return FlatTool(toolY);

            case ToolShape.Ball:
                return BallTool(toolY, distance);

            case ToolShape.Cone:
                return ConeTool(toolY, distance);

            default:
                return toolY;
        }
    }

    private float FlatTool(float toolY)
    {
        return toolY;
    }

    private float BallTool(float toolY, float distance)
    {
        float r = toolRadius;

        // уравнение сферы
        float h = Mathf.Sqrt(r * r - distance * distance);

        return toolY + (r - h);
    }

    private float ConeTool(float toolY, float distance)
    {
        float angleRad = coneAngle * Mathf.Deg2Rad;

        float slope = Mathf.Tan(angleRad);

        return toolY + distance * slope;
    }
}

public enum ToolShape
{
    Flat,
    Ball,
    Cone
}

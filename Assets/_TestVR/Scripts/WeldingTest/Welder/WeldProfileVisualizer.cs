using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class WeldProfileVisualizer : MonoBehaviour
{
    [SerializeField] private RawImage targetRawImage;
    [SerializeField] private Color realProfileColor = Color.blue;
    [SerializeField] private Color idealProfileColor = Color.green;
    [SerializeField] private int textureWidth = 512;
    [SerializeField] private int textureHeight = 256;
    [SerializeField] private float marginPixels = 20f;

    private Texture2D graphTexture;

    private void Awake()
    {
        graphTexture = new Texture2D(textureWidth, textureHeight);
        graphTexture.filterMode = FilterMode.Point;
        if (targetRawImage != null)
            targetRawImage.texture = graphTexture;
    }

    /// <summary>
    /// Отображает график реального профиля и идеального эталона.
    /// totalLength – длина шва по оси s, weaveAmplitude – амплитуда узора.
    /// </summary>
    public void DrawProfile(List<Vector2> realProfile, float totalLength, float weaveAmplitude, WeldPattern pattern, float frequency)
    {
        // Очищаем белым
        Color[] clearColors = new Color[textureWidth * textureHeight];
        for (int i = 0; i < clearColors.Length; i++)
            clearColors[i] = Color.white;
        graphTexture.SetPixels(clearColors);

        // Границы по осям
        float xMin = 0f;
        float xMax = totalLength;
        float yMin = -weaveAmplitude * 1.5f;
        float yMax = weaveAmplitude * 1.5f;

        // Идеальный профиль
        List<Vector2> idealProfile = GenerateIdealProfile(totalLength, pattern, frequency, weaveAmplitude, 200);
        DrawLine(idealProfile, idealProfileColor, xMin, xMax, yMin, yMax);

        // Реальный профиль
        DrawLine(realProfile, realProfileColor, xMin, xMax, yMin, yMax);

        graphTexture.Apply();
    }

    private void DrawLine(List<Vector2> points, Color color, float xMin, float xMax, float yMin, float yMax)
    {
        if (points.Count < 2) return;

        float xScale = (textureWidth - 2 * marginPixels) / (xMax - xMin);
        float yScale = (textureHeight - 2 * marginPixels) / (yMax - yMin);
        float xOffset = marginPixels;
        float yOffset = marginPixels;

        for (int i = 1; i < points.Count; i++)
        {
            Vector2 p0 = points[i - 1];
            Vector2 p1 = points[i];

            int x0 = Mathf.RoundToInt((p0.x - xMin) * xScale + xOffset);
            int y0 = Mathf.RoundToInt((p0.y - yMin) * yScale + yOffset);
            int x1 = Mathf.RoundToInt((p1.x - xMin) * xScale + xOffset);
            int y1 = Mathf.RoundToInt((p1.y - yMin) * yScale + yOffset);

            DrawLineOnTexture(x0, y0, x1, y1, color);
        }
    }

    // Алгоритм Брезенхэма
    private void DrawLineOnTexture(int x0, int y0, int x1, int y1, Color color)
    {
        int dx = Mathf.Abs(x1 - x0), sx = x0 < x1 ? 1 : -1;
        int dy = -Mathf.Abs(y1 - y0), sy = y0 < y1 ? 1 : -1;
        int err = dx + dy, e2;

        while (true)
        {
            if (x0 >= 0 && x0 < textureWidth && y0 >= 0 && y0 < textureHeight)
                graphTexture.SetPixel(x0, y0, color);

            if (x0 == x1 && y0 == y1) break;
            e2 = 2 * err;
            if (e2 >= dy) { err += dy; x0 += sx; }
            if (e2 <= dx) { err += dx; y0 += sy; }
        }
    }

    private List<Vector2> GenerateIdealProfile(float totalLength, WeldPattern pattern, float frequency, float amplitude, int steps)
    {
        List<Vector2> profile = new List<Vector2>();

        for (int i = 0; i <= steps; i++)
        {
            float s = (i / (float)steps) * totalLength;
            float phase = s * frequency;
            float omega = phase * Mathf.PI * 2f;

            Vector2 p = Vector2.zero;

            switch (pattern)
            {
                case WeldPattern.Straight:
                    {
                        p = new Vector2(s, 0f);
                        break;
                    }

                case WeldPattern.Zigzag:
                    {
                        float t = Mathf.PingPong(phase, 1f);
                        float y = Mathf.Lerp(-amplitude, amplitude, t);

                        p = new Vector2(s, y);
                        break;
                    }

                case WeldPattern.Circle:
                    {
                        p = new Vector2(s + Mathf.Cos(omega) * amplitude, Mathf.Sin(omega) * amplitude);
                        break;
                    }

                case WeldPattern.Figure8:
                    {
                        p = new Vector2(s + Mathf.Sin(omega) * amplitude, Mathf.Sin(2f * omega) * amplitude);
                        break;
                    }

                case WeldPattern.Spiral:
                    {
                        float r = amplitude * (0.3f + 0.7f * (i / (float)steps));

                        p = new Vector2(s + Mathf.Cos(omega) * r, Mathf.Sin(omega) * r);
                        break;
                    }

                case WeldPattern.CShape:
                    {
                        float a = Mathf.Lerp(-Mathf.PI * 0.75f, Mathf.PI * 0.75f, (Mathf.Sin(omega) + 1f) * 0.5f);

                        p = new Vector2(s + Mathf.Cos(a) * amplitude, Mathf.Sin(a) * amplitude);
                        break;
                    }

                case WeldPattern.Herringbone:
                    {
                        float t = phase % 1f;
                        float y;

                        if (t < 0.5f)
                            y = amplitude * (4f * t - 1f);
                        else
                            y = amplitude * (3f - 4f * t);

                        p = new Vector2(s, y);
                        break;
                    }

                default:
                    {
                        p = new Vector2(s, amplitude * Mathf.Sin(omega));
                        break;
                    }
            }

            profile.Add(p);
        }

        return profile;
    }
}
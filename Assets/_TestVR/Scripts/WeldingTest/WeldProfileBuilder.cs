using System.Collections.Generic;
using UnityEngine;

public static class WeldProfileBuilder
{
    /// <summary>
    /// Строит развёртку траектории: (продольный путь s, поперечное отклонение) 
    /// с динамической системой координат, использующей нормаль в каждой точке.
    /// </summary>
    /// <param name="points">Точки шва с нормалями (из WeldMeshBuilder).</param>
    /// <returns>Список Vector2, где x = s (накопленный путь), y = боковое отклонение.</returns>
    public static List<Vector2> BuildProfile(List<BeadPoint> points)
    {
        List<Vector2> profile = new List<Vector2>();
        if (points.Count == 0) return profile;

        // Первая точка: s=0, отклонение 0
        profile.Add(new Vector2(0f, 0f));
        float accumulatedS = 0f;

        for (int i = 1; i < points.Count; i++)
        {
            Vector3 prevPos = points[i - 1].position;
            Vector3 currPos = points[i].position;
            Vector3 segment = currPos - prevPos;
            float segmentLength = segment.magnitude;
            if (segmentLength < 0.0001f) continue;

            // Направление шва в этом сегменте
            Vector3 seamDir = segment.normalized;

            // Нормаль в текущей точке (можно взять среднюю между соседними, но возьмём из текущей)
            Vector3 normal = points[i].normal;

            // Поперечное направление
            Vector3 lateral = Vector3.Cross(normal, seamDir).normalized;
            // На всякий случай, если коллинеарны
            if (lateral.magnitude < 0.1f)
            {
                // аварийный вектор
                lateral = Vector3.Cross(Vector3.up, seamDir).normalized;
                if (lateral.magnitude < 0.1f)
                    lateral = Vector3.Cross(Vector3.forward, seamDir).normalized;
            }

            // Смещение от начала шва (первая точка)
            Vector3 fromStart = currPos - points[0].position;
            float s = Vector3.Dot(fromStart, seamDir); // это приближённое продольное расстояние, но мы будем использовать накопленное
            // Лучше накопить точно
            accumulatedS += segmentLength;
            float sCoord = accumulatedS;
            Vector3 axisDirection = (points[points.Count - 1].position - points[0].position).normalized;
            // Проецируем вектор от начальной точки до текущей на ось и на перпендикуляр
            Vector3 toPoint = currPos - points[0].position;
            float projOnAxis = Vector3.Dot(toPoint, axisDirection);
            Vector3 projectionOnAxis = points[0].position + projOnAxis * axisDirection;
            float deviation = Vector3.Distance(currPos, projectionOnAxis);
            // Знак отклонения: через cross
            Vector3 cross = Vector3.Cross(axisDirection, toPoint);
            float sign = Mathf.Sign(Vector3.Dot(cross, points[0].normal));
            float signedDeviation = deviation * sign;

            profile.Add(new Vector2(sCoord, signedDeviation));
        }
        return profile;
    }
}
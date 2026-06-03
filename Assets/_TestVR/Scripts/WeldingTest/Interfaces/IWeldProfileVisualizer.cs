using System.Collections.Generic;
using UnityEngine;

public interface IWeldProfileVisualizer
{
    public void DrawProfile(List<Vector2> profile, float length, float amplitude, string pattern, float frequency);
}

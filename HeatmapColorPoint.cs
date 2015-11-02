using System;
using UnityEngine;

[Serializable]
public struct HeatmapColorPoint
{

    [Range(0.0f, 1.0f)]
    public float Progress;

    public Color Color;

    public HeatmapColorPoint(float progress, Color color)
    {
        Progress = progress;
        Color = color;
    }

}
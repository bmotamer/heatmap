using UnityEngine;

public static class HeatmapUtils
{

    private static bool TryLerp(HeatmapColorPoint from, HeatmapColorPoint to, float progress, out Color color)
    {
        // If the given progress is larger than the 'from' color point's progress, lerps the two colors
        if (progress > from.Progress)
        {
            float difference = to.Progress - from.Progress;

            color = Color.Lerp(
                from.Color,
                to.Color,
                (difference == 0.0f) ? 1.0f : (progress - from.Progress) / difference
            );
            return true;
        }

        color = default(Color);

        return false;
    }

    public static Color GetColor(HeatmapColorPoint[] colorPoints, float progress)
    {
        // If the color points array is null or empty, uses white
        if (colorPoints == null)
            return Color.white;

        int colorPointIndex = colorPoints.Length;

        if (colorPointIndex == 0)
            return Color.white;

        Color color;

        for (colorPointIndex -= 2; colorPointIndex >= 0; --colorPointIndex)
            if (TryLerp(colorPoints[colorPointIndex], colorPoints[colorPointIndex + 1], progress, out color))
                return color;

        // If there's only one color point in the array, uses it
        return colorPoints[0].Color;
    }

}
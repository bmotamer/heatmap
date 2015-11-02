using System;

[Flags]
public enum HeatmapNormalization
{

    None         = 0x0,
    Color        = 0x1,
    Size         = 0x2,
    ColorAndSize = Color | Size

}
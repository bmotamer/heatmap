using System;
using UnityEngine;

public class HeatmapData
{

	public int   Width;  // Amount of horizontal tiles
    public int   Height; // Amount of vertical tiles

    public float MaxPositionWeight;
    public float MaxPositionWeightLog10;

    public float MaxShootingWeight;
    public float MaxShootingWeightLog10;

    public float MaxRunningWeight;
    public float MaxRunningWeightLog10;

    public HeatmapTile[] Tiles = new HeatmapTile[0]; // Array what contains all the tiles within the grid

    public HeatmapData()
    {
    }

    public HeatmapData(int width, int height)
    {
        Width  = width;
        Height = height;

        // Instead of using a bidimensional array, grabs enough space to fit
        Tiles = new HeatmapTile[width * height];
    }

    public HeatmapTile this[int x, int y]
    {
        get { return Tiles[x + y * Width]; }
    }

    // Adds position, shooting or running weight to a tile
    // If the new weight is over the maximum, replaces the maximum for this new one

    #region AddPositionWeightTo

    public void AddPositionWeightTo(ref HeatmapTile tile, float weight)
    {
        tile.PositionWeight     += weight;
        tile.PositionWeightLog10 = Mathf.Log10(1.0f + tile.PositionWeight);

        if (tile.PositionWeight > MaxPositionWeight)
        {
            MaxPositionWeight      = tile.PositionWeight;
            MaxPositionWeightLog10 = tile.PositionWeightLog10;
        }
    }

    public void AddPositionWeightTo(int x, int y, float weight)
    {
        AddPositionWeightTo(ref Tiles[x + y * Width], weight);
    }

    #endregion

    #region AddShootingWeightTo

    public void AddShootingWeightTo(ref HeatmapTile tile, float weight)
    {
        tile.ShootingWeight += weight;
        tile.ShootingWeightLog10 = Mathf.Log10(1.0f + tile.ShootingWeight);

        if (tile.ShootingWeight > MaxShootingWeight)
        {
            MaxShootingWeight = tile.ShootingWeight;
            MaxShootingWeightLog10 = tile.ShootingWeightLog10;
        }
    }

    public void AddShootingWeightTo(int x, int y, float weight)
    {
        AddShootingWeightTo(ref Tiles[x + y * Width], weight);
    }

    #endregion

    #region AddRunningWeightTo

    public void AddRunningWeightTo(ref HeatmapTile tile, float weight)
    {
        tile.RunningWeight     += weight;
        tile.RunningWeightLog10 = Mathf.Log10(1.0f + tile.RunningWeight);

        if (tile.RunningWeight > MaxRunningWeight)
        {
            MaxRunningWeight      = tile.RunningWeight;
            MaxRunningWeightLog10 = tile.RunningWeightLog10;
        }
    }

    public void AddRunningWeightTo(int x, int y, float weight)
    {
        AddRunningWeightTo(ref Tiles[x + y * Width], weight);
    }

    #endregion

    public static HeatmapData FromJSONFile(TextAsset jsonFile)
    {
        return (HeatmapData)Json.Deserialize(typeof(HeatmapData), jsonFile.text);
    }

    public void ForEachTile(Action<int, int, HeatmapTile> action)
    {
        if (action == null)
            return;

        // Iterates through every tile and calls the given function passing x, y and the tile data as
        // the parameters of the function
        for (int y = 0; y < Height; y++)
            for (int x = 0; x < Width; x++)
                action(x, y, Tiles[x + y * Width]);
    }

}
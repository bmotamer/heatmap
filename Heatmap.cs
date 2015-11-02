using System;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

public sealed class Heatmap : MonoBehaviour
{

    //
	// Variables visible in the editor
	//
	public Vector2              Pivot                  = new Vector2(0.5f, 0.5f); // Center point of the grid
	public float                Interval               = 1.0f;                    // Interval to store data
	public Transform            Player                 = null;
    public float                Weight                 = 1.0f;
    public HeatmapScale         Scale                  = HeatmapScale.Linear;
    public HeatmapNormalization Normalization          = HeatmapNormalization.ColorAndSize;
    public HeatmapVisualization Visualization          = HeatmapVisualization.PositionShootingAndRunning;
	public float                CubeHeightMultiplier   = 3.0f;                    // Maximum height Gizmo cubes have when they own the highest weight
    public float                SphereRadiusMultiplier = 1.0f;                    // Maximum height Gizmo cubes have when they own the highest weight
    public float                CircleRadiusMultiplier = 1.0f;
	public float                ColorHighlight         = 0.125f;                  // Amount of white added to the color when the player is on a certain tile
	[Range(0.0f, 1.0f)]
	public float                Cleanliness            = 0.0625f;
	public Color                GridColor              = new Color(1.0f, 1.0f, 1.0f, 0.25f);
	public HeatmapColorPoint[]  ColorPoints            = new HeatmapColorPoint[5]
	{
		new HeatmapColorPoint(0.00f, new Color(0.0f, 0.0f, 1.0f, 0.12f)), // Blue   @   0%
		new HeatmapColorPoint(0.25f, new Color(0.0f, 1.0f, 1.0f, 0.24f)), // Cyan   @  25%
		new HeatmapColorPoint(0.50f, new Color(0.0f, 1.0f, 0.0f, 0.36f)), // Green  @  50%
		new HeatmapColorPoint(0.75f, new Color(1.0f, 1.0f, 0.0f, 0.48f)), // Yellow @  75%
		new HeatmapColorPoint(1.00f, new Color(1.0f, 0.0f, 0.0f, 0.60f))  // Red    @ 100%
	};

	//
	// Variables hidden in the editor
	//
    [HideInInspector] public TextAsset   File;                               // JSON file to be loaded
    [HideInInspector] public Vector2     Size     = new Vector2(3.0f, 3.0f); // Dimensions of the grid, in units
    [HideInInspector] public Vector2     Division = new Vector2(1.0f, 1.0f); // Grid division size
    [HideInInspector] public HeatmapData Data;
    [HideInInspector] public bool        IsPlayerShooting;
    [HideInInspector] public bool        IsPlayerRunning;
    [HideInInspector] public Action      AutoSave;                           // Function used by the custom inspector to save automatically that gets ran when the game is over

	private Vector3 _Size;        // Grid bounds world size
	private Vector3 _Min;         // Grid bounds world minimum position
	private Vector3 _Max;         // Grid bounds world maximum position
	private float   _Timer;       // Timer used for the update interval
	private int     _PlayerGridX;  
	private int     _PlayerGridY;
	
	public void Start()
	{
		RefreshData();

		if (Interval < 0.0f)
			Interval = 0.0f;
	}
	
	public void RefreshData()
	{
        // Calculates the dimensions of the grid, in units (but rounded to fit divisions correctly)
        if (File == null)
            Data = new HeatmapData(
                Mathf.RoundToInt(Size.x / Division.x),
                Mathf.RoundToInt(Size.y / Division.y)
            );
        else
            // Or loads a file if there's one selected
            Data = HeatmapData.FromJSONFile(File);
        
        _Size = new Vector3(Data.Width * Division.x, 0.0f, Data.Height * Division.y);

        RefreshPosition();
	}

    public void RefreshPosition()
    {
        // Then calculates the minimum and maximum position according to the given pivot
        _Min = new Vector3(
            transform.position.x - Pivot.x * _Size.x,
            transform.position.y,
            transform.position.z - Pivot.y * _Size.z
        );

        _Max = _Min + _Size;
    }
	
	public void Update()
	{
		_Timer -= Time.deltaTime;
		
		// If the player is within the grid and it's time to store the data, calculates
		// their position on the grid and add weight to the tile they're standing on
		if (
			(_Timer <= 0.0f) &&
			(Player != null) &&
			(Player.position.x >= _Min.x) &&
			(Player.position.z >= _Min.z) &&
			(Player.position.x <= _Max.x) &&
			(Player.position.z <= _Max.z)
		)
		{
			_PlayerGridX = Mathf.FloorToInt((Player.position.x - _Min.x) / Division.x);
			_PlayerGridY = Mathf.FloorToInt((Player.position.z - _Min.z) / Division.y);

            Data.AddPositionWeightTo(_PlayerGridX, _PlayerGridY, Weight);
			
            if (IsPlayerShooting)
                Data.AddShootingWeightTo(_PlayerGridX, _PlayerGridY, Weight);

            if (IsPlayerRunning)
                Data.AddRunningWeightTo(_PlayerGridX, _PlayerGridY, Weight);
		}
		
		// Then resets the timer
		if (Interval <= 0.0f)
			_Timer = 0.0f;
		else
			while (_Timer < 0.0f)
				_Timer += Interval;
	}
	
	public void OnDrawGizmos()
	{
		// If the game is being played, there's no need to setup since it has already been setup
		// on start when the scene was loaded
		if (!Application.isPlaying && (Data == null))
			RefreshData();
		
		// Draws the horizontal and vertical lines of the grid
		Gizmos.color = GridColor;
		
        if (Gizmos.color.a > 0.0f)
        {
            float tmp;

            for (int x = 0; x <= Data.Width; x++)
            {
                tmp = _Min.x + x * Division.x;

                Gizmos.DrawLine(
                    new Vector3(tmp, _Min.y, _Min.z),
                    new Vector3(tmp, _Min.y, _Min.z + _Size.z)
                );
            }

            for (int y = 0; y <= Data.Height; y++)
            {
                tmp = _Min.z + y * Division.y;

                Gizmos.DrawLine(
                    new Vector3(_Min.x, _Min.y, tmp),
                    new Vector3(_Min.x + _Size.x, _Min.y, tmp)
                );
            }
        }
		
		// Then draws each tile information
		Data.ForEachTile((x, y, tile) => DrawTile(x, y, tile));
	}
	
	public void DrawTile(int x, int y, HeatmapTile tile)
	{
		// Checks if this tile is the tile the playe is standing on, so it makes the color brighter
		bool    playerTile = (x == _PlayerGridX) && (y == _PlayerGridY);
		// Calculates the center world position of the tile
		Vector3 origin     = new Vector3(_Min.x + (x + 0.5f) * Division.x, _Min.y, _Min.z + (y + 0.5f) * Division.y);
		
		// Calculates the minimum weight for position, shooting and running based on the cleanliness level
        float minPositionWeight      = Cleanliness * Data.MaxPositionWeight;
        float minPositionWeightLog10 = Cleanliness * Data.MaxPositionWeightLog10;

        float minShootingWeight      = Cleanliness * Data.MaxShootingWeight;
        float minShootingWeightLog10 = Cleanliness * Data.MaxShootingWeightLog10;

        float minRunningWeight       = Cleanliness * Data.MaxRunningWeight;
        float minRunningWeightLog10  = Cleanliness * Data.MaxRunningWeightLog10;

        float positionWeight;
		float positionRatio;
        float positionCleanRatio;

        float shootingWeight;
        float shootingRatio;
        float shootingCleanRatio;

        float runningWeight;
        float runningRatio;
        float runningCleanRatio;

        // Calculates the average using the selected scale
        switch (Scale)
        {
            case HeatmapScale.Log10:
                positionWeight     = tile.PositionWeightLog10;
                positionRatio      = positionWeight / Data.MaxPositionWeightLog10;
                positionCleanRatio = (positionWeight - minPositionWeightLog10) / (Data.MaxPositionWeightLog10 - minPositionWeightLog10);

                shootingWeight     = tile.ShootingWeightLog10;
                shootingRatio      = shootingWeight / Data.MaxShootingWeightLog10;
                shootingCleanRatio = (shootingWeight - minShootingWeightLog10) / (Data.MaxShootingWeightLog10 - minShootingWeightLog10);

                runningWeight     = tile.RunningWeightLog10;
                runningRatio      = runningWeight / Data.MaxRunningWeightLog10;
                runningCleanRatio = (runningWeight - minRunningWeightLog10) / (Data.MaxRunningWeightLog10 - minRunningWeightLog10);

                break;
            default:
                positionWeight     = tile.PositionWeight;
                positionRatio      = positionWeight / Data.MaxPositionWeight;
                positionCleanRatio = (positionWeight - minPositionWeight) / (Data.MaxPositionWeight - minPositionWeight);

                shootingWeight     = tile.ShootingWeight;
                shootingRatio      = shootingWeight / Data.MaxShootingWeight;
                shootingCleanRatio = (shootingWeight - minShootingWeight) / (Data.MaxShootingWeight - minShootingWeight);

                runningWeight     = tile.RunningWeight;
                runningRatio      = runningWeight / Data.MaxRunningWeight;
                runningCleanRatio = (runningWeight - minRunningWeight) / (Data.MaxRunningWeight - minRunningWeight);

                break;
        }

        float size;

        // Checks if the user selected to visualize the position, shooting and running weights
        // If they did, then draws cubes for the position weights
		if (
            ((Visualization & HeatmapVisualization.Position) == HeatmapVisualization.Position) &&
            (positionRatio > 0.0f) &&
            (positionRatio >= Cleanliness)
        )
		{
            if ((Normalization & HeatmapNormalization.Size) == HeatmapNormalization.Size)
                size = positionCleanRatio * CubeHeightMultiplier;
            else
                size = positionWeight * CubeHeightMultiplier;

            if ((Normalization & HeatmapNormalization.Color) == HeatmapNormalization.Color)
                Gizmos.color = HeatmapUtils.GetColor(ColorPoints, positionCleanRatio);
            else
                Gizmos.color = HeatmapUtils.GetColor(ColorPoints, positionWeight);

			if (playerTile)
                Gizmos.color += new Color(ColorHighlight, ColorHighlight, ColorHighlight, 0.0f);

            Gizmos.DrawCube(
                origin + new Vector3(0.0f, 0.5f * size, 0.0f),
                new Vector3(Division.x, size, Division.y)
            );

            Gizmos.color = new Color(
                Gizmos.color.r,
                Gizmos.color.g,
                Gizmos.color.b,
                1.0f
            );

			Gizmos.DrawWireCube(
				origin + new Vector3(0.0f, 0.5f * size, 0.0f),
				new Vector3(Division.x, size, Division.y)
			);
		}

        // Spheres for the shooting weights
        if (
            ((Visualization & HeatmapVisualization.Shooting) == HeatmapVisualization.Shooting) &&
            (shootingRatio > 0.0f) &&
            (shootingRatio >= Cleanliness)
        )
        {
            float height = 0.5f * Mathf.Min(Division.x, Division.y);

            if ((Normalization & HeatmapNormalization.Size) == HeatmapNormalization.Size)
                size = shootingCleanRatio * height * SphereRadiusMultiplier;
            else
                size = shootingWeight * height * SphereRadiusMultiplier;

            if ((Normalization & HeatmapNormalization.Color) == HeatmapNormalization.Color)
                Gizmos.color = HeatmapUtils.GetColor(ColorPoints, shootingCleanRatio);
            else
                Gizmos.color = HeatmapUtils.GetColor(ColorPoints, shootingWeight);

            if (playerTile)
                Gizmos.color += new Color(ColorHighlight, ColorHighlight, ColorHighlight, 0.0f);

            Gizmos.DrawSphere(
                origin + new Vector3(0.0f, size, 0.0f),
                size
            );

            Gizmos.color = new Color(
                Gizmos.color.r,
                Gizmos.color.g,
                Gizmos.color.b,
                1.0f
            );

            Gizmos.DrawWireSphere(
                origin + new Vector3(0.0f, size, 0.0f),
                size
            );
        }

        // And circles for the running weights
        if (
            ((Visualization & HeatmapVisualization.Running) == HeatmapVisualization.Running) &&
            (runningRatio > 0.0f) &&
            (runningRatio >= Cleanliness)
        )
        {
            float height = 0.5f * Mathf.Min(Division.x, Division.y);

            if ((Normalization & HeatmapNormalization.Size) == HeatmapNormalization.Size)
                size = runningCleanRatio * height * CircleRadiusMultiplier;
            else
                size = runningWeight * height * CircleRadiusMultiplier;

            // The only way to draw circles on the scene view is by using the Handles class from the UnityEditor namespace
            // However, the UnityEditor namespace doesn't get compiled when the project is built, so we have to check for the UNITY_EDITOR symbol
#if UNITY_EDITOR

            if ((Normalization & HeatmapNormalization.Color) == HeatmapNormalization.Color)
                Handles.color = HeatmapUtils.GetColor(ColorPoints, runningCleanRatio);
            else
                Handles.color = HeatmapUtils.GetColor(ColorPoints, runningWeight);

            if (playerTile)
                Handles.color += new Color(ColorHighlight, ColorHighlight, ColorHighlight, 0.0f);

            Handles.DrawSolidArc(origin, Vector3.up, Vector3.forward, 360.0f, size);

            Handles.color = new Color(
                Handles.color.r,
                Handles.color.g,
                Handles.color.b,
                1.0f
            );

            Handles.DrawWireArc(origin, Vector3.up, Vector3.forward, 360.0f, size);

#endif
        }
	}

    public void OnDestroy()
    {
        // When the player leaves the game, autosaves
        if (AutoSave != null)
            AutoSave();
    }

}
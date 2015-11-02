using System;

[Flags]
public enum HeatmapVisualization
{

    None                       = 0x0,
    Position                   = 0x1,
    Shooting                   = 0x2,
    Running                    = 0x4,
    PositionAndShooting        = Position | Shooting,
    ShootingAndRunning         = Shooting | Running,
    PositionAndRunning         = Position | Running,
    PositionShootingAndRunning = Position | Shooting | Running

}
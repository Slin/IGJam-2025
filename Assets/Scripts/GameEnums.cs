using UnityEngine;

/// <summary>
/// Types of enemies in the game
/// </summary>
public enum EnemyType
{
    Regular,
    Fast,
    Armored,
    Boss,
    Attack,
    Teleporter,
    Exploder
}

/// <summary>
/// Types of buildings that can be constructed
/// </summary>
public enum BuildingType
{
    Base,
    RocketLauncher,
    LaserTower,
    BoostBuilding,
    DroneFactory,
    FreezeTower,
    RadarJammer
}

/// <summary>
/// Current phase of the game
/// </summary>
public enum GamePhase
{
    Building,
    Defense
}

/// <summary>
/// State of the game
/// </summary>
public enum GameState
{
    NotStarted,
    Playing,
    GameOver
}

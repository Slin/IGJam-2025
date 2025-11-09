using UnityEngine;

public class UIActions : MonoBehaviour
{
    void Reset()
    {
    }

    // Canvas Button: Start Round
    public void StartRound()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.RequestStartDefensePhase();
            return;
        }
    }

    // Canvas Buttons: Select Building for placement
    public void SelectBase()
    {
        var bm = BuildingManager.Instance;
        if (bm == null) return;
        if (!CanSelectBuilding()) { AudioManager.Instance?.PlaySFX("click"); return; }
        bm.StartBuildingPlacement(BuildingType.Base);
    }

    public void SelectRocketLauncher()
    {
        var bm = BuildingManager.Instance;
        if (bm == null) return;
        if (!CanSelectBuilding()) { AudioManager.Instance?.PlaySFX("click"); return; }
        bm.StartBuildingPlacement(BuildingType.RocketLauncher);
    }

    public void SelectLaserTower()
    {
        var bm = BuildingManager.Instance;
        if (bm == null) return;
        if (!CanSelectBuilding()) { AudioManager.Instance?.PlaySFX("click"); return; }
        bm.StartBuildingPlacement(BuildingType.LaserTower);
    }

    public void SelectBoostBuilding()
    {
        var bm = BuildingManager.Instance;
        if (bm == null) return;
        if (!CanSelectBuilding()) { AudioManager.Instance?.PlaySFX("click"); return; }
        bm.StartBuildingPlacement(BuildingType.BoostBuilding);
    }

    public void SelectDroneFactory()
    {
        var bm = BuildingManager.Instance;
        if (bm == null) return;
        if (!CanSelectBuilding()) { AudioManager.Instance?.PlaySFX("click"); return; }
        bm.StartBuildingPlacement(BuildingType.DroneFactory);
    }

    public void SelectFreezeTower()
    {
        var bm = BuildingManager.Instance;
        if (bm == null) return;
        if (!CanSelectBuilding()) { AudioManager.Instance?.PlaySFX("click"); return; }
        bm.StartBuildingPlacement(BuildingType.FreezeTower);
    }

    public void SelectRadarJammer()
    {
        var bm = BuildingManager.Instance;
        if (bm == null) return;
        if (!CanSelectBuilding()) { AudioManager.Instance?.PlaySFX("click"); return; }
        bm.StartBuildingPlacement(BuildingType.RadarJammer);
    }

    public void CancelPlacement()
    {
        BuildingManager.Instance?.CancelBuildingPlacement();
    }

    bool CanSelectBuilding()
    {
        var gm = GameManager.Instance;
        return gm == null || gm.CurrentPhase == GamePhase.Building;
    }
}



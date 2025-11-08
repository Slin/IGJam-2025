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
		BuildingManager.Instance?.StartBuildingPlacement(BuildingType.Base);
	}

	public void SelectRocketLauncher()
	{
		BuildingManager.Instance?.StartBuildingPlacement(BuildingType.RocketLauncher);
	}

	public void SelectLaserTower()
	{
		BuildingManager.Instance?.StartBuildingPlacement(BuildingType.LaserTower);
	}

	public void SelectBoostBuilding()
	{
		BuildingManager.Instance?.StartBuildingPlacement(BuildingType.BoostBuilding);
	}

	public void SelectDroneFactory()
	{
		BuildingManager.Instance?.StartBuildingPlacement(BuildingType.DroneFactory);
	}

	public void CancelPlacement()
	{
		BuildingManager.Instance?.CancelBuildingPlacement();
	}
}



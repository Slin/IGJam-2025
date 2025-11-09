using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Updates the cost display on building buttons based on purchase count
/// Attach this to each building button in the UI
/// </summary>
[DisallowMultipleComponent]
public class BuildingButtonUI : MonoBehaviour
{
    [Header("Settings")]
    public BuildingType buildingType;

    [Header("References (Auto-find if empty)")]
    public TMP_Text priceText;
    public Button button;

    bool _subscribed = false;

    void Awake()
    {
        // Auto-find components if not assigned
        if (button == null)
            button = GetComponent<Button>();

        if (priceText == null)
        {
            // Look for a child named "Price" with TMP_Text
            Transform priceTransform = transform.Find("Price");
            if (priceTransform != null)
                priceText = priceTransform.GetComponent<TMP_Text>();
        }
    }

    void OnEnable()
    {
        // Try to subscribe and update, but don't require success
        TrySubscribeToEvents();
        UpdateCostDisplay();
    }

    void OnDisable()
    {
        UnsubscribeFromEvents();
    }

    void TrySubscribeToEvents()
    {
        if (_subscribed) return;

        var buildingManager = BuildingManager.Instance;
        var playerStats = PlayerStatsManager.Instance;

        // Only mark as subscribed if we successfully subscribe to both
        bool canSubscribe = buildingManager != null && playerStats != null;

        if (!canSubscribe) return;

        buildingManager.onBuildingPlaced.AddListener(OnBuildingPlaced);
        buildingManager.onBuildingDestroyed.AddListener(OnBuildingDestroyed);
        playerStats.onTritiumChanged.AddListener(OnTritiumChanged);

        _subscribed = true;
    }

    void UnsubscribeFromEvents()
    {
        if (!_subscribed) return;

        var buildingManager = BuildingManager.Instance;
        var playerStats = PlayerStatsManager.Instance;

        if (buildingManager != null)
        {
            buildingManager.onBuildingPlaced.RemoveListener(OnBuildingPlaced);
            buildingManager.onBuildingDestroyed.RemoveListener(OnBuildingDestroyed);
        }

        if (playerStats != null)
        {
            playerStats.onTritiumChanged.RemoveListener(OnTritiumChanged);
        }

        _subscribed = false;
    }

    void OnBuildingPlaced(Building building)
    {
        // Update cost display after any building is placed (purchase counts change)
        UpdateCostDisplay();
    }

    void OnBuildingDestroyed(Building building)
    {
        // Update affordability when buildings are destroyed (might refund tritium in future)
        UpdateCostDisplay();
    }

    void OnTritiumChanged(int newAmount)
    {
        // Update button state based on affordability
        UpdateCostDisplay();
    }

    public void UpdateCostDisplay()
    {
        var buildingManager = BuildingManager.Instance;
        var playerStats = PlayerStatsManager.Instance;

        // If managers aren't ready, just return (will update when they are)
        if (buildingManager == null || playerStats == null)
            return;

        // Get the current cost for this building type
        int currentCost = buildingManager.GetCurrentBuildingCost(buildingType);

        // Update the price text
        if (priceText != null)
        {
            priceText.text = $"{currentCost} T";
        }

        // Update button interactability based on affordability
        if (button != null)
        {
            bool canAfford = playerStats.CanAfford(currentCost);
            button.interactable = canAfford;
        }
    }

    void Start()
    {
        // Start coroutine to ensure we subscribe once managers are ready
        StartCoroutine(WaitForManagersAndUpdate());
    }

    System.Collections.IEnumerator WaitForManagersAndUpdate()
    {
        // Wait for both managers to be ready
        while (BuildingManager.Instance == null || PlayerStatsManager.Instance == null)
        {
            yield return null;
        }

        // Ensure we're subscribed
        TrySubscribeToEvents();

        // Force an update after subscription
        UpdateCostDisplay();
    }
}

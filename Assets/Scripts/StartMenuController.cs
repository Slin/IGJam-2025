using UnityEngine;

public class StartMenuController : MonoBehaviour
{
    [SerializeField] private GameObject howToPlayPrefab;
    private HowToPlayController howToPlayController;

    void Start()
    {
        // Find or setup the HowToPlay controller
        if (howToPlayPrefab != null)
        {
            howToPlayController = howToPlayPrefab.GetComponent<HowToPlayController>();
            // Make sure it's hidden at start
            howToPlayPrefab.SetActive(false);
        }
    }

    public void ShowHowToPlay()
    {
        if (howToPlayController != null)
        {
            AudioManager.Instance?.PlaySFX("click");
            howToPlayController.Open();
        }
        else if (howToPlayPrefab != null)
        {
            AudioManager.Instance?.PlaySFX("click");
            howToPlayPrefab.SetActive(true);
        }
    }

    public void HideHowToPlay()
    {
        if (howToPlayPrefab != null)
        {
            AudioManager.Instance?.PlaySFX("click");
            howToPlayPrefab.SetActive(false);
        }
    }
}

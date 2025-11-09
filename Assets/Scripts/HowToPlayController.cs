using UnityEngine;
using UnityEngine.UI;

public class HowToPlayController : MonoBehaviour
{
    [Header("Panels")]
    [SerializeField] private GameObject panel1;
    [SerializeField] private GameObject panel2;
    [SerializeField] private GameObject panel3;

    [Header("Navigation Buttons")]
    [SerializeField] private Button nextButton1;
    [SerializeField] private Button nextButton2;
    [SerializeField] private Button previousButton2;
    [SerializeField] private Button previousButton3;
    [SerializeField] private Button closeButton;

    private int currentPanelIndex = 0;
    private GameObject[] panels;

    void Awake()
    {
        // Initialize panel array
        panels = new GameObject[] { panel1, panel2, panel3 };

        // Setup button listeners
        if (nextButton1 != null)
            nextButton1.onClick.AddListener(() => ShowPanel(1));
        
        if (nextButton2 != null)
            nextButton2.onClick.AddListener(() => ShowPanel(2));
        
        if (previousButton2 != null)
            previousButton2.onClick.AddListener(() => ShowPanel(0));
        
        if (previousButton3 != null)
            previousButton3.onClick.AddListener(() => ShowPanel(1));
        
        if (closeButton != null)
            closeButton.onClick.AddListener(Close);
    }

    void OnEnable()
    {
        // Show first panel when opened
        ShowPanel(0);
    }

    public void ShowPanel(int index)
    {
        if (index < 0 || index >= panels.Length)
            return;

        // Hide all panels
        foreach (var panel in panels)
        {
            if (panel != null)
                panel.SetActive(false);
        }

        // Show selected panel
        if (panels[index] != null)
        {
            panels[index].SetActive(true);
            currentPanelIndex = index;
        }

        // Play click sound
        AudioManager.Instance?.PlaySFX("click");
    }

    public void NextPanel()
    {
        ShowPanel(currentPanelIndex + 1);
    }

    public void PreviousPanel()
    {
        ShowPanel(currentPanelIndex - 1);
    }

    public void Close()
    {
        AudioManager.Instance?.PlaySFX("click");
        gameObject.SetActive(false);
    }

    public void Open()
    {
        gameObject.SetActive(true);
        ShowPanel(0);
    }
}

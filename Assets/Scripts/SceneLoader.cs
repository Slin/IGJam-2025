using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneLoader : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {

    }

    public void LoadGameplayScene()
    {
        // Stop any playing SFX (like game over melody) before loading gameplay scene
        AudioManager.Instance?.StopAllSFX();
        
        SceneManager.LoadScene("GameplayScene");
    }

    public void LoadGameOverScene()
    {
        SceneManager.LoadScene("GameOverScene");
    }
}

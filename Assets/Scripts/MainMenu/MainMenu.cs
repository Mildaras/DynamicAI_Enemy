using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenu : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Button continueButton;
    [SerializeField] private GameObject settingsPanel;

    void Start()
    {
        // Gray-out Continue if there is no save file
        if (continueButton != null)
            continueButton.interactable = GameSaveManager.HasSaveFile();

        // Make sure settings panel starts hidden
        if (settingsPanel != null)
            settingsPanel.SetActive(false);
    }

    /// <summary>
    /// Wipe any existing save, reset player state, and start fresh.
    /// </summary>
    public void NewGame()
    {
        GameSaveManager.DeleteSave();
        PlayerData.ResetToDefaults();
        PlayerPrefs.SetInt("GameDay", 1);
        SceneManager.LoadSceneAsync(1);
    }

    /// <summary>
    /// Load the saved progress and jump into the game.
    /// </summary>
    public void ContinueGame()
    {
        GameSaveManager.LoadGame();
        SceneManager.LoadSceneAsync(1);
    }

    public void OpenSettings()
    {
        if (settingsPanel != null)
            settingsPanel.SetActive(true);
    }

    public void CloseSettings()
    {
        if (settingsPanel != null)
            settingsPanel.SetActive(false);
    }

    public void QuitGame()
    {
        Application.Quit();
    }
}

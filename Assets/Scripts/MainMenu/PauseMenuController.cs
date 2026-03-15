using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// In-game pause menu toggled with ESC.
/// Attach to a Canvas in the GameScene that contains:
///   - A pause panel  (Resume / Settings / Quit to Menu buttons)
///   - A settings sub-panel (with SettingsUI component)
/// Both panels should start disabled in the editor.
/// </summary>
public class PauseMenuController : MonoBehaviour
{
    [Header("Panels")]
    [SerializeField] private GameObject pausePanel;
    [SerializeField] private GameObject settingsPanel;

    private bool _isPaused;

    void Start()
    {
        // Ensure panels are hidden on start
        if (pausePanel   != null) pausePanel.SetActive(false);
        if (settingsPanel != null) settingsPanel.SetActive(false);
    }

    void Update()
    {
        // Let NPC dialogue handle ESC first
        if (PlayerMovement.dialogueActive)
            return;

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (settingsPanel != null && settingsPanel.activeSelf)
            {
                CloseSettings();
            }
            else if (_isPaused)
            {
                Resume();
            }
            else
            {
                Pause();
            }
        }
    }

    public void Pause()
    {
        _isPaused = true;
        if (pausePanel != null) pausePanel.SetActive(true);
        Time.timeScale = 0f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        Debug.Log($"[Pause] Paused. Cursor locked={Cursor.lockState}, visible={Cursor.visible}");
    }

    public void Resume()
    {
        Debug.Log("[Pause] Resume clicked");
        _isPaused = false;
        if (pausePanel    != null) pausePanel.SetActive(false);
        if (settingsPanel != null) settingsPanel.SetActive(false);
        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    public void OpenSettings()
    {
        Debug.Log("[Pause] OpenSettings clicked");
        if (settingsPanel != null) settingsPanel.SetActive(true);
        if (pausePanel    != null) pausePanel.SetActive(false);
    }

    /// <summary>
    /// Called by the settings Back button to return to the pause panel.
    /// </summary>
    public void CloseSettings()
    {
        if (settingsPanel != null) settingsPanel.SetActive(false);
        if (pausePanel    != null) pausePanel.SetActive(true);
    }

    /// <summary>
    /// Save progress and return to the main menu.
    /// </summary>
    public void QuitToMainMenu()
    {
        Debug.Log("[Pause] QuitToMainMenu clicked");
        GameSaveManager.SaveGame();
        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        SceneManager.LoadScene(0);
    }
}

using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

public class SettingsUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Slider      volumeSlider;
    [SerializeField] private Slider      sensitivitySlider;
    [SerializeField] private TMP_Dropdown resolutionDropdown;
    [SerializeField] private Toggle      fullscreenToggle;

    [Header("Navigation")]
    [SerializeField] private Button      backButton;
    [SerializeField] private string      mainMenuSceneName = "MainMenu";

    void Start()
    {
        var mgr = SettingsManager.Instance;

        // populate resolution dropdown
        resolutionDropdown.ClearOptions();
        var options = new System.Collections.Generic.List<string>();
        for (int i = 0; i < mgr.Resolutions.Length; i++)
        {
            var r = mgr.Resolutions[i];
            options.Add($"{r.width} × {r.height} @ {r.refreshRate}Hz");
        }
        resolutionDropdown.AddOptions(options);

        // initialize UI with current settings
        volumeSlider.value       = mgr.MasterVolume;
        sensitivitySlider.value  = mgr.MouseSensitivity;
        resolutionDropdown.value = mgr.ResolutionIndex;
        fullscreenToggle.isOn    = mgr.IsFullscreen;

        // bind events
        volumeSlider.onValueChanged.AddListener(val =>
        {
            mgr.ApplyVolume(val);
            mgr.SaveSettings();
        });

        sensitivitySlider.onValueChanged.AddListener(val =>
        {
            mgr.ApplySensitivity(val);
            mgr.SaveSettings();
        });

        resolutionDropdown.onValueChanged.AddListener(idx =>
        {
            mgr.ApplyResolution(idx, mgr.IsFullscreen);
            mgr.SaveSettings();
        });

        fullscreenToggle.onValueChanged.AddListener(isFs =>
        {
            mgr.ApplyResolution(mgr.ResolutionIndex, isFs);
            mgr.SaveSettings();
        });

        backButton.onClick.AddListener(() =>
        {
            SceneManager.LoadScene(mainMenuSceneName);
        });
    }
}

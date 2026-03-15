using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Populates and binds the settings panel UI.
/// Works as an overlay — no scene change needed.
/// Attach to the root of the settings panel.
/// </summary>
public class SettingsUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Slider       volumeSlider;
    [SerializeField] private Slider       sensitivitySlider;
    [SerializeField] private TMP_Dropdown resolutionDropdown;
    [SerializeField] private Toggle       fullscreenToggle;

    [Header("Value Displays")]
    [SerializeField] private TextMeshProUGUI volumeValueText;
    [SerializeField] private TextMeshProUGUI sensitivityValueText;

    [Header("Navigation")]
    [SerializeField] private Button backButton;

    private bool _initialized;

    void OnEnable()
    {
        // Re-sync UI every time the panel is shown
        if (SettingsManager.Instance != null)
            Initialize();
    }

    private void Initialize()
    {
        var mgr = SettingsManager.Instance;

        if (!_initialized)
        {
            // populate resolution dropdown once
            resolutionDropdown.ClearOptions();
            var options = new System.Collections.Generic.List<string>();
            for (int i = 0; i < mgr.Resolutions.Length; i++)
            {
                var r = mgr.Resolutions[i];
                options.Add($"{r.width} × {r.height} @ {r.refreshRate}Hz");
            }
            resolutionDropdown.AddOptions(options);

            // bind events (only once)
            volumeSlider.onValueChanged.AddListener(val =>
            {
                mgr.ApplyVolume(val);
                mgr.SaveSettings();
                UpdateValueTexts();
            });

            sensitivitySlider.onValueChanged.AddListener(val =>
            {
                mgr.ApplySensitivity(val);
                mgr.SaveSettings();
                UpdateValueTexts();
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

            // Back button simply hides this panel
            if (backButton != null)
            {
                backButton.onClick.AddListener(() =>
                {
                    gameObject.SetActive(false);
                });
            }

            _initialized = true;
        }

        // Sync slider / toggle values with current settings
        volumeSlider.SetValueWithoutNotify(mgr.MasterVolume);
        sensitivitySlider.SetValueWithoutNotify(mgr.MouseSensitivity);
        resolutionDropdown.SetValueWithoutNotify(mgr.ResolutionIndex);
        fullscreenToggle.isOn = mgr.IsFullscreen;
        UpdateValueTexts();
    }

    private void UpdateValueTexts()
    {
        if (volumeValueText != null)
            volumeValueText.text = Mathf.RoundToInt(volumeSlider.value * 100f).ToString();
        if (sensitivityValueText != null)
            sensitivityValueText.text = Mathf.RoundToInt(sensitivitySlider.value).ToString();
    }
}

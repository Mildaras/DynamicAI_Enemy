using UnityEngine;

public class SettingsManager : MonoBehaviour
{
    public static SettingsManager Instance { get; private set; }

    // keys for persistence
    private const string kVolumeKey       = "Settings_Volume";
    private const string kSensKey         = "Settings_Sensitivity";
    private const string kResolutionKey   = "Settings_ResolutionIndex";
    private const string kFullscreenKey   = "Settings_Fullscreen";

    [Header("Current Settings")]
    public float MasterVolume    { get; private set; }
    public float MouseSensitivity{ get; private set; }
    public Resolution[] Resolutions { get; private set; }
    public int   ResolutionIndex { get; private set; }
    public bool  IsFullscreen    { get; private set; }

    void Awake()
    {
        // singleton pattern
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        // build resolution list
        Resolutions = Screen.resolutions;
        // load or set defaults
        LoadSettings();
        ApplyAll();
    }

    public void LoadSettings()
    {
        MasterVolume     = PlayerPrefs.GetFloat(kVolumeKey, 1f);
        MouseSensitivity = PlayerPrefs.GetFloat(kSensKey, 200f);
        ResolutionIndex  = PlayerPrefs.GetInt(kResolutionKey,   Resolutions.Length - 1);
        IsFullscreen     = PlayerPrefs.GetInt(kFullscreenKey,   1) == 1;
    }

    public void SaveSettings()
    {
        PlayerPrefs.SetFloat(kVolumeKey,     MasterVolume);
        PlayerPrefs.SetFloat(kSensKey,       MouseSensitivity);
        PlayerPrefs.SetInt(  kResolutionKey, ResolutionIndex);
        PlayerPrefs.SetInt(  kFullscreenKey, IsFullscreen ? 1 : 0);
        PlayerPrefs.Save();
    }

    public void ApplyAll()
    {
        ApplyVolume(MasterVolume);
        ApplySensitivity(MouseSensitivity);
        ApplyResolution(ResolutionIndex, IsFullscreen);
    }

    // Called from UI slider
    public void ApplyVolume(float vol)
    {
        MasterVolume = vol;
        AudioListener.volume = vol;
    }

    // Called from UI slider
    public void ApplySensitivity(float sens)
    {
        MouseSensitivity = sens;
        CameraMovement.baseSensX = sens;
        CameraMovement.baseSensY = sens;
    }

    // Called from UI dropdown or toggle
    public void ApplyResolution(int index, bool fullscreen)
    {
        ResolutionIndex = index;
        IsFullscreen = fullscreen;
        var res = Resolutions[index];
        Screen.SetResolution(res.width, res.height, fullscreen);
    }
}

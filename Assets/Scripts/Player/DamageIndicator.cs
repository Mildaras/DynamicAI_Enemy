using UnityEngine;
using UnityEngine.UI;
using System.Collections;

/// <summary>
/// Provides visual feedback for taking damage:
/// - Red vignette that pulses on damage
/// - Intensifies when health is low
/// - Screen shake on damage
/// - Optional damage direction indicator
/// </summary>
public class DamageIndicator : MonoBehaviour
{
    public static DamageIndicator Instance { get; private set; }
    
    [Header("UI References")]
    [Tooltip("Assign a fullscreen Image with red color (set alpha in code)")]
    public Image damageVignette;
    
    [Header("Damage Flash Settings")]
    [Tooltip("How opaque the flash is when taking damage")]
    public float damageFlashIntensity = 0.3f;
    [Tooltip("How long the damage flash lasts")]
    public float damageFlashDuration = 0.2f;
    
    [Header("Low Health Settings")]
    [Tooltip("Health percent below which warning appears")]
    [Range(0f, 1f)]
    public float lowHealthThreshold = 0.25f;
    [Tooltip("Max alpha of low health vignette")]
    public float lowHealthMaxAlpha = 0.4f;
    [Tooltip("How fast the low health vignette pulses")]
    public float lowHealthPulseSpeed = 2f;
    
    [Header("Screen Shake")]
    [Tooltip("Enable screen shake on damage")]
    public bool enableScreenShake = true;
    [Tooltip("Screen shake duration")]
    public float shakeDuration = 0.15f;
    [Tooltip("Screen shake intensity")]
    public float shakeIntensity = 0.1f;
    
    private float _currentAlpha = 0f;
    private float _flashTimer = 0f;
    private bool _isFlashing = false;
    
    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }
        
        if (damageVignette != null)
        {
            Color c = damageVignette.color;
            c.a = 0f;
            damageVignette.color = c;
        }
    }
    
    void Update()
    {
        if (damageVignette == null) return;
        
        // Calculate low health vignette
        float healthPercent = PlayerData.playerHealth / 100f;
        float lowHealthAlpha = 0f;
        
        if (healthPercent < lowHealthThreshold)
        {
            // Pulse effect for low health
            float normalizedHealth = healthPercent / lowHealthThreshold; // 0 at death, 1 at threshold
            float pulse = (Mathf.Sin(Time.time * lowHealthPulseSpeed) + 1f) * 0.5f; // 0 to 1
            lowHealthAlpha = Mathf.Lerp(lowHealthMaxAlpha * 0.5f, lowHealthMaxAlpha, pulse) * (1f - normalizedHealth);
        }
        
        // Combine low health vignette with damage flash
        float targetAlpha = Mathf.Max(_currentAlpha, lowHealthAlpha);
        
        // Smoothly fade out damage flash
        if (_isFlashing)
        {
            _flashTimer -= Time.deltaTime;
            if (_flashTimer <= 0f)
            {
                _isFlashing = false;
            }
        }
        else if (_currentAlpha > 0f)
        {
            _currentAlpha = Mathf.MoveTowards(_currentAlpha, 0f, Time.deltaTime * damageFlashIntensity / damageFlashDuration);
        }
        
        // Apply alpha
        Color c = damageVignette.color;
        c.a = targetAlpha;
        damageVignette.color = c;
    }
    
    /// <summary>
    /// Call this when player takes damage
    /// </summary>
    public void OnDamageTaken(float damageAmount)
    {
        if (_isFlashing) return; // Prevent multiple flashes overlapping
        
        TriggerFlash();
        
        // Screen shake
        if (enableScreenShake && CameraShake.Instance != null)
        {
            // Scale shake with damage amount (but cap it)
            float scaledIntensity = Mathf.Min(shakeIntensity * (damageAmount / 50f), shakeIntensity * 2f);
            CameraShake.Instance.Shake(shakeDuration, scaledIntensity);
        }
    }
    
    private void TriggerFlash()
    {
        _isFlashing = true;
        _currentAlpha = damageFlashIntensity;
        _flashTimer = damageFlashDuration;
    }
}

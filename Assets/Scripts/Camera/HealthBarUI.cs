using UnityEngine;
using UnityEngine.UI;

public class HealthBarUI : MonoBehaviour
{
    [Tooltip("The UI Image with Fill Method=Horizontal")]
    public Image healthFillImage;
    public Image shieldFillImage;
    
    [Tooltip("Your player's max health")]
    public float maxHealth = 100f;
    public float maxShield = 200f;

    void Update()
    {
        // 1. Read your player’s current health
        float curr = PlayerData.playerHealth;  // or however you expose it
        float currShield = PlayerData.playerShield;  // or however you expose it
        // 2. Normalize to [0,1]
        float t = Mathf.Clamp01(curr / maxHealth);
        float tShield = Mathf.Clamp01(currShield / maxShield);
        // 3. Drive the fill
        healthFillImage.fillAmount = t;
        shieldFillImage.fillAmount = tShield;
        // 4. Hide when empty
        healthFillImage.enabled = t > 0f;
        shieldFillImage.enabled = tShield > 0f;
    }
}

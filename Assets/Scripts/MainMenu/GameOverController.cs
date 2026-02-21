using UnityEngine;
using TMPro;
using System.Collections;

[RequireComponent(typeof(CanvasGroup))]
public class GameOverController : MonoBehaviour
{
    [Tooltip("How long to fade in (seconds)")]
    public float fadeDuration = 2f;

    [Tooltip("TextMeshProUGUI component for the days survived")]
    public TextMeshProUGUI daysText;

    private CanvasGroup _canvasGroup;

    void Awake()
    {
        _canvasGroup = GetComponent<CanvasGroup>();
        _canvasGroup.alpha = 0f;
        _canvasGroup.blocksRaycasts = false; // prevent clicks while fading
    }

    IEnumerator Start()
    {
        // 1) Figure out how many days the player survived
        int day = PlayerPrefs.GetInt("GameDay", 1);
        int survived = Mathf.Max(0, day - 1);
        if (daysText != null)
            daysText.text = $"Days Survived: {survived}";

        // 2) Fade in the panel (and its children) over fadeDuration seconds
        float t = 0f;
        _canvasGroup.blocksRaycasts = true; // enable blocking once fade starts
        while (t < fadeDuration)
        {
            t += Time.unscaledDeltaTime;  
            _canvasGroup.alpha = Mathf.Clamp01(t / fadeDuration);
            yield return null;
        }
        _canvasGroup.alpha = 1f;
    }
}

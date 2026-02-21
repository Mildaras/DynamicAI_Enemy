using UnityEngine;
using System.Collections;

/// <summary>
/// Attach this to your Main Camera. Call CameraShake.Instance.Shake(...) to trigger screen shake.
/// </summary>
[RequireComponent(typeof(Camera))]
public class CameraShake : MonoBehaviour
{
    public static CameraShake Instance { get; private set; }
    private Transform _camTransform;
    private Vector3 _originalPos;

    void Awake()
    {
        // Singleton pattern
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }
        _camTransform = transform;
        _originalPos = _camTransform.localPosition;
    }

    /// <summary>
    /// Call this to shake the camera.
    /// </summary>
    /// <param name="duration">Total duration of the shake in seconds.</param>
    /// <param name="magnitude">Maximum offset magnitude.</param>
    public void Shake(float duration, float magnitude)
    {
        StopAllCoroutines();
        StartCoroutine(DoShake(duration, magnitude));
    }

    private IEnumerator DoShake(float duration, float magnitude)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            float x = Random.Range(-1f, 1f) * magnitude;
            float y = Random.Range(-1f, 1f) * magnitude;
            _camTransform.localPosition = _originalPos + new Vector3(x, y, 0f);

            elapsed += Time.deltaTime;
            yield return null;
        }
        // restore
        _camTransform.localPosition = _originalPos;
    }
}

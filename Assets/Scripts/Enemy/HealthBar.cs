// HealthBar.cs
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Displays a world-space health bar above a target, automatically
/// calculating vertical offset based on renderer or collider height.
/// </summary>


public class HealthBar : MonoBehaviour
{
    [SerializeField] private Image fillImage;
    [Tooltip("Extra vertical margin above the target's top bounds")]
    [SerializeField] private float verticalMargin = 1f;
    [Tooltip("Optional horizontal/pivot offset (x,z). Vertical offset is calculated automatically.")]
    [SerializeField] private Vector2 horizontalOffset = Vector2.zero;

    private Transform target;
    private Camera mainCam;
    private Vector3 calculatedOffset;
    private Vector3 originalTargetScale;
    private Vector3 originalLocalScale;

    /// <summary>
    /// Call once right after Instantiate to hook up.
    /// normalizedHealth: 0…1
    /// </summary>
    public void Initialize(Transform targetTransform, float normalizedHealth)
    {

        target = targetTransform;
        mainCam = Camera.main;

        // set initial fill
        fillImage.fillAmount = normalizedHealth;

        // Calculate vertical offset based on renderer or collider bounds
        float halfHeight = 0f;
        var rend = targetTransform.GetComponentInChildren<Renderer>();
        if (rend != null)
        {
            halfHeight = rend.bounds.extents.y;
        }
        else
        {
            var col = targetTransform.GetComponent<Collider>();
            if (col != null)
                halfHeight = col.bounds.extents.y;
        }

        // Store full offset
        calculatedOffset = new Vector3(
            horizontalOffset.x,
            halfHeight + verticalMargin,
            horizontalOffset.y
        );

        originalTargetScale = targetTransform.localScale;
        originalLocalScale = transform.localScale;
    }

    void LateUpdate()
    {
        if (target == null)
        {
            Destroy(gameObject);
            return;
        }

        // Scale health bar with enemy size
        float scaleRatio = target.localScale.x / originalTargetScale.x;
        transform.position = target.position + calculatedOffset * scaleRatio;
        transform.localScale = originalLocalScale * scaleRatio;
        transform.rotation = Quaternion.LookRotation(transform.position - mainCam.transform.position);
    }

    /// <summary>
    /// Called by Enemy.OnHealthChanged(float): normalized (0…1).
    /// </summary>
    public void SetHealth(float normalized)
    {
        fillImage.fillAmount = normalized;
    }
}

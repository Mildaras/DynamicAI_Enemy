using UnityEngine;

[RequireComponent(typeof(Transform))]
public class BouncerLaunchProjectile : MonoBehaviour
{
    public Vector3 targetPosition;
    public GameObject bouncerPrefab;
    public float travelTime = 0.5f;
    public float startScale = 0.1f;

    private float _timer;
    private Vector3 _startPosition;
    private Vector3 _origScale;

    void Awake()
    {
        _startPosition = transform.position;
        _origScale = transform.localScale;
        transform.localScale = _origScale * startScale;
    }

    void Update()
    {
        _timer += Time.deltaTime;
        float t = Mathf.Clamp01(_timer / travelTime);
        // Move
        transform.position = Vector3.Lerp(_startPosition, targetPosition, t);
        // Scale up
        transform.localScale = Vector3.Lerp(_origScale * startScale, _origScale, t);

        if (t >= 1f)
        {
            // Spawn actual bouncer
            if (bouncerPrefab != null)
                Object.Instantiate(bouncerPrefab, targetPosition, Quaternion.identity);
            Destroy(gameObject);
        }
    }
}
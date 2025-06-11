using UnityEngine;

[RequireComponent(typeof(Transform))]
public class TankSpawnEffect : MonoBehaviour
{
    [Tooltip("How far below the final position they start.")]
    public float riseDistance = 4f;
    [Tooltip("How quickly (seconds) they rise.")]
    public float riseDuration = 0.8f;


    private Vector3 _startPos;
    private Vector3 _endPos;
    private float   _timer;

    void Awake()
    {
        // record the target (final) position
        _endPos = transform.position;
        // start below ground
        _startPos = _endPos - Vector3.up * riseDistance;
        transform.position = _startPos;
        _timer = 0f;
    }

    void Update()
    {
        if (_timer < riseDuration)
        {
            _timer += Time.deltaTime;
            float t = Mathf.Clamp01(_timer / riseDuration);
            // smoothstep gives a nicer ease-in/out
            t = t * t * (3f - 2f * t);
            transform.position = Vector3.Lerp(_startPos, _endPos, t);
        }
        else
        {
            // ensure you end exactly at the target
            transform.position = _endPos;
            // and then you can destroy this component if you like:
            Destroy(this);
        }
    }
}
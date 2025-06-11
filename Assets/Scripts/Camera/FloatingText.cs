using UnityEngine;
using TMPro;

[RequireComponent(typeof(TextMeshPro))]
public class FloatingText : MonoBehaviour
{
    [Tooltip("How fast the text floats up (world units/sec)")]
    public float floatSpeed = 1f;
    [Tooltip("Total lifetime before self‐destroy")]
    public float lifeTime   = 1f;
    private float _bornTime;
    private TextMeshPro _tmp;
    private Color       _baseColor;

    void Awake()
    {
        _tmp = GetComponent<TextMeshPro>();
        _baseColor = _tmp.color;
    }

    public void Initialize(string text, Color color)
    {
        _tmp.text = text;
        _tmp.color = color;
        _baseColor = color;
        _bornTime = Time.time;
    }

    void Update()
    {
        // float up
        transform.position += Vector3.up * floatSpeed * Time.deltaTime;

        // fade out over lifetime
        float t = (Time.time - _bornTime) / lifeTime;
        if (t >= 1f) { Destroy(gameObject); return; }
        Color c = _baseColor;
        c.a = Mathf.Lerp(1f, 0f, t);
        _tmp.color = c;
    }
}

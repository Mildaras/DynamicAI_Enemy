// WeaponSwingController.cs
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class WeaponSwingController : MonoBehaviour
{
    // stats (injected by WeaponManager)
    [HideInInspector] public float damage;
    [HideInInspector] public float minSpeed;
    [HideInInspector] public float maxSpeed;

    [Header("Hit Volume")]
    [SerializeField] private float attackReach  = 1.0f;
    [SerializeField] private float attackRadius = 0.5f;
    [SerializeField] private LayerMask enemyLayer;

    [Header("Cooldown")]
    [SerializeField] private float swingCooldown = 0.5f;
    private float _nextAllowedSwingTime = 0f;

    [Header("Visual Swing")]
    [Tooltip("Assign your sword mesh transform here.")]
    [SerializeField] private Transform weaponModel;
    [Tooltip("How sensitive the visual follows Mouse X/Y axes.")]
    [SerializeField] private float visualSensX = 120f;
    [SerializeField] private float visualSensY = 120f;
    [Tooltip("Max degrees the sword can swing from rest.")]
    [SerializeField] private float maxSwingAngle = 90f;
    [Tooltip("How fast it returns to idle pose.")]
    [SerializeField] private float visualSensYaw   = 4000f;
    [Tooltip("How fast the sword pitches (up/down) per mouse Y unit.")]
    [SerializeField] private float visualSensPitch = 100f;
    [Tooltip("How fast the sword rolls (leans) per mouse X unit.")]

    [Header("Swing Trail")]
    [SerializeField] private TrailRenderer swingTrail;

    [Tooltip("Raw mouse-speed that yields full red trail.")]
    [SerializeField] private float maxTrailHueSpeed = 1000f;
    [SerializeField] private float visualSensRoll  = 150f;
    [SerializeField] private float returnSpeed = 5f;

    // swing‐tracking (for damage)
    private bool swinging = false;
    private float elapsed;
    private float totalDist;

    // visual‐tracking
    private Quaternion _restRotation;
    private float _visAngleX;
    private float _visAngleY;
    private float      _visYaw, _visPitch, _visRoll;

    [SerializeField] private GameObject floatingTextPrefab;

    void Awake()
    {
        if (weaponModel == null)
            weaponModel = transform; 
        _restRotation = weaponModel.localRotation;

        if (swingTrail != null)
            swingTrail.emitting = false;
    }

    void Update()
    {
        // start swing
        if (Input.GetMouseButtonDown(0) 
            && !swinging 
            && Time.time >= _nextAllowedSwingTime)
        {
            BeginSwing();
        }

        // while swinging
        if (swinging)
            TrackSwing();

        // end swing
        if (Input.GetMouseButtonUp(0) && swinging)
            EndSwing();

        // return to rest pose
        if (!swinging)
        {
            weaponModel.localRotation = Quaternion.Slerp(
                weaponModel.localRotation,
                _restRotation,
                Time.deltaTime * returnSpeed
            );
        }
    }

    private void BeginSwing()
    {
        swinging = true;
        CameraMovement.isSwinging = true;
        elapsed   = 0f;
        totalDist = 0f;
        _visAngleX = 0f;
        _visAngleY = 0f;
        _restRotation = weaponModel.localRotation;

        if (swingTrail != null)
        {
            swingTrail.Clear();
            swingTrail.emitting = true;
        }
    }

    void TrackSwing()
    {
        // --- DAMAGE TRACKING (unchanged) ---
        float dx = Input.GetAxisRaw("Mouse X");
        float dy = Input.GetAxisRaw("Mouse Y");

        float rawSpeed = new Vector2(
            Input.GetAxisRaw("Mouse X"),
            Input.GetAxisRaw("Mouse Y")
        ).magnitude * visualSensYaw;

        // map to 0…1
        float t = Mathf.Clamp01(rawSpeed / maxTrailHueSpeed);

        // Lerp white→red
        Color c = Color.Lerp(Color.white, Color.red, t);

        // apply to both ends of the trail
        if (swingTrail != null)
        {
            swingTrail.startColor = c;
            swingTrail.endColor   = c;
        }

        float frameDist = Mathf.Sqrt(dx*dx + dy*dy);
        totalDist += frameDist;
        elapsed   += Time.deltaTime;

        // --- VISUAL TRACKING ---
        // accumulate yaw/pitch/roll
        _visYaw   = Mathf.Clamp(
            _visYaw   + dx * visualSensYaw   * Time.deltaTime,
            -maxSwingAngle, maxSwingAngle
        );

        _visPitch = Mathf.Clamp(
            _visPitch + dy * visualSensPitch * Time.deltaTime,
            -maxSwingAngle, maxSwingAngle
        );

        // roll is opposite dx so blade leans INTO the swing
        _visRoll  = Mathf.Clamp(
            _visRoll  - dx * visualSensRoll  * Time.deltaTime,
            -maxSwingAngle, maxSwingAngle
        );

        // combine into a single rotation
        Quaternion q = Quaternion.Euler(_visPitch, _visYaw, _visRoll);
        weaponModel.localRotation = _restRotation * q;
    }

    private void EndSwing()
    {
        
        _visYaw = _visPitch = _visRoll = 0f;
        swinging = false;
        CameraMovement.isSwinging = false;
        _nextAllowedSwingTime = Time.time + swingCooldown;

        if (swingTrail != null)
            swingTrail.emitting = false;

        float avgSpeed = (elapsed > 0f) ? totalDist / elapsed : 0f;
        Debug.Log($"Swing speed = {avgSpeed:F0}");

        if (avgSpeed < minSpeed * 0.5f)
        {
            Debug.Log("Missed (too slow).");
        }
        else if (avgSpeed < minSpeed)
        {
            float t = (avgSpeed - minSpeed * 0.5f) / (minSpeed * 0.5f);
            ApplyDamage(damage * t);
        }
        else if (avgSpeed > maxSpeed)
        {
            Debug.Log("Missed (too fast).");
        }
        else
        {
            ApplyDamage(damage);
        }
    }

    private void ApplyDamage(float dmg)
    {
        Vector3 origin = transform.position + transform.forward * attackReach;
        Collider[] hits = Physics.OverlapSphere(origin, attackRadius, enemyLayer);
        foreach (var col in hits)
        {
            var d = col.GetComponent<IDamageable>();
            if (d != null)
            {
                float dist = Vector3.Distance(transform.position, col.transform.position);
                // log *before* or *after* you call TakeDamage
                ActionLogger.Instance.LogAction(
                        actor:     "Player",
                        actionType:"Swing",   
                        target:     col.gameObject.name, 
                        isHit:      true,
                        damage:     dmg,
                        distance:   dist
                    );
                d.TakeDamage(dmg);
                SpawnFloatingText($"{(int)dmg}", Color.red, col.transform.position + Vector3.up * 1f);
            }
            else
            {
                // log a miss
                ActionLogger.Instance.LogAction(
                    actor:     "Player",
                    actionType:"Swing",   
                    target:     col.gameObject.name, 
                    isHit:      false,
                    damage:     0f,
                    distance:   Vector3.Distance(transform.position, col.transform.position)
                );
            }
        }
    }

    private void SpawnFloatingText(string text, Color color, Vector3 worldPos)
    {
        if (floatingTextPrefab == null) return;
        var go = Instantiate(floatingTextPrefab, worldPos, Quaternion.identity);
        var ft = go.GetComponent<FloatingText>();
        if (ft != null)
            ft.Initialize(text, color);
    }
}

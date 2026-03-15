using UnityEngine;

public class CameraMovement : MonoBehaviour
{
    [Header("Mouse Sensitivity")]
    public float sensX = 400f;
    public float sensY = 400f;
    public Transform player;

    [Header("Head Bob Settings")]
    public bool enableHeadBob = true;
    [Tooltip("Vertical bob height when walking")]
    public float walkBobAmount = 0.05f;
    [Tooltip("Vertical bob height when sprinting")]
    public float sprintBobAmount = 0.08f;
    [Tooltip("Speed of head bob oscillation when walking")]
    public float walkBobSpeed = 12f;
    [Tooltip("Speed of head bob oscillation when sprinting")]
    public float sprintBobSpeed = 16f;
    [Tooltip("How quickly bob returns to center when stopping")]
    public float bobResetSpeed = 5f;

    float rotationY;
    float rotationX;

    public static float baseSensX;
    public static float baseSensY;

    public static bool isSwinging = false; // Static flag set externally (e.g., from WeaponSwingController)

    // Head bob state
    private float bobTimer = 0f;
    private Vector3 originalLocalPosition;
    private Rigidbody playerRb;
    private PlayerMovement playerMovement;

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = true;

        baseSensX = sensX;
        baseSensY = sensY;

        // Store original local position for head bob
        originalLocalPosition = transform.localPosition;

        // Get player rigidbody for velocity checks
        if (player != null)
        {
            playerRb = player.GetComponent<Rigidbody>();
            playerMovement = player.GetComponent<PlayerMovement>();
        }
    }

    void Update()
    {
        if (!PlayerMovement.dialogueActive && Time.timeScale > 0f)
        {
            float sensitivityMultiplier = isSwinging ? 0.3f : 1.0f; // Reduce camera sensitivity while swinging

            float mouseX = Input.GetAxisRaw("Mouse X") * Time.deltaTime * baseSensX * sensitivityMultiplier;
            float mouseY = Input.GetAxisRaw("Mouse Y") * Time.deltaTime * baseSensY * sensitivityMultiplier;

            rotationY += mouseX;
            rotationX -= mouseY;
            rotationX = Mathf.Clamp(rotationX, -80f, 80f);

            transform.rotation = Quaternion.Euler(rotationX, rotationY, 0);
            player.rotation = Quaternion.Euler(0, rotationY, 0);

            // Apply head bob
            if (enableHeadBob)
            {
                ApplyHeadBob();
            }

            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
        else
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }

    void ApplyHeadBob()
    {
        if (playerRb == null || playerMovement == null) return;

        // Access the grounded state directly from PlayerMovement using reflection
        // (since grounded field is private)
        var groundedField = typeof(PlayerMovement).GetField("grounded", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        bool isGrounded = groundedField != null && (bool)groundedField.GetValue(playerMovement);

        // Get player horizontal velocity
        Vector3 flatVel = new Vector3(playerRb.linearVelocity.x, 0f, playerRb.linearVelocity.z);
        float speed = flatVel.magnitude;

        // Check if player is sprinting
        bool isSprinting = Input.GetKey(KeyCode.LeftShift);

        // Determine if player is moving AND grounded
        if (isGrounded && speed > 0.5f) // Movement threshold
        {
            // Choose bob parameters based on sprint state
            float bobAmount = isSprinting ? sprintBobAmount : walkBobAmount;
            float bobSpeed = isSprinting ? sprintBobSpeed : walkBobSpeed;

            // Increment bob timer
            bobTimer += Time.deltaTime * bobSpeed;

            // Calculate vertical offset using sine wave
            float bobOffset = Mathf.Sin(bobTimer) * bobAmount;

            // Apply bob to camera position
            Vector3 newPos = originalLocalPosition;
            newPos.y += bobOffset;
            transform.localPosition = newPos;
        }
        else
        {
            // Player is not moving - smoothly return to center
            bobTimer = 0f;
            transform.localPosition = Vector3.Lerp(
                transform.localPosition,
                originalLocalPosition,
                Time.deltaTime * bobResetSpeed
            );
        }
    }
}

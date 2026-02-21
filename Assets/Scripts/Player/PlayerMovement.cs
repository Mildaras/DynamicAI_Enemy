using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed;
    public float groundDrag;
    public float jumpForce;
    public float jumpCooldown;
    public float airMultiplier;
    [Tooltip("How quickly player stops when no input (higher = snappier)")]
    public float stopForce = 5f;
    public static bool dialogueActive = false;
    bool readyToJump;
    float jumps = 1;
    float extraJumps = 1;

    float walkSpeed = 7f;
    float sprintSpeed = 10f;

    

    [Header("Keybinds")]
    public KeyCode jumpKey   = KeyCode.Space;
    public KeyCode sprintKey = KeyCode.LeftShift;

    [Header("Ground Check")]
    public float    playerHeight;
    public LayerMask whatIsGround;
    bool grounded;

    [Header("References")]
    public Transform orientation;   // assign your camera's transform here
    [SerializeField] private Animator animator;  // assign your Player’s Animator here


    float horizontalInput;
    float verticalInput;
    Vector3 moveDirection;
    Rigidbody rb;

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.freezeRotation = true;
        readyToJump = true;

        if (animator == null)
            animator = GetComponentInChildren<Animator>();
    }

    private void Update()
    {
        // 1) Ground check + input + drag
        if (!dialogueActive)
        {
            grounded = Physics.Raycast(
                transform.position,
                Vector3.down,
                playerHeight * 0.5f + 0.3f,
                whatIsGround
            );
            MyInput();
            SpeedControl();
            RotateModelToCamera();

            // Apply ground drag (friction) when grounded
            if (grounded)
                rb.linearDamping = groundDrag;
            else
                rb.linearDamping = 0f;
        }
        else
        {
            horizontalInput = verticalInput = 0f;
            rb.linearVelocity = Vector3.zero;
        }

        // 2) Animate
        if (animator != null)
        {
            // normalize horizontal speed to [0,1] based on sprintSpeed
            Vector3 flatVel = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
            float speedNorm = Mathf.Clamp01(flatVel.magnitude / sprintSpeed);
            animator.SetFloat("Speed", speedNorm);

            // sprint flag
            animator.SetBool("isSprinting", Input.GetKey(sprintKey));

            // grounded flag
            animator.SetBool("isGrounded", grounded);
        }
    }

    private void FixedUpdate()
    {
        if (!dialogueActive)
            MovePlayer();
    }

    private void MyInput()
    {
        if (dialogueActive) return;

        horizontalInput = Input.GetAxisRaw("Horizontal");
        verticalInput   = Input.GetAxisRaw("Vertical");

        // default to walk
        moveSpeed = walkSpeed;

        // Jump
        if (Input.GetKey(jumpKey) && readyToJump && grounded && jumps > 0)
        {
            readyToJump = false;
            if (animator != null) animator.SetTrigger("JumpT");
            Jump();
            Invoke(nameof(ResetJump), jumpCooldown);
        }
        else if (Input.GetKey(jumpKey) && readyToJump && !grounded
                 && PlayerData.hasExtraJump && extraJumps > 0)
        {
            readyToJump = false;
            if (animator != null) animator.SetTrigger("JumpT");
            Jump();
            extraJumps--;
            Invoke(nameof(ResetJump),      jumpCooldown);
            Invoke(nameof(ResetExtraJump), 5f);
        }

        // Sprint
        if (Input.GetKey(sprintKey))
            moveSpeed = sprintSpeed;
    }

    private void MovePlayer()
    {
        moveDirection = orientation.forward * verticalInput
                      + orientation.right   * horizontalInput;

        // Check if player is actively moving
        bool isMoving = horizontalInput != 0f || verticalInput != 0f;
        
        if (isMoving)
        {
            // Apply movement force
            if (grounded)
                rb.AddForce(moveDirection.normalized * moveSpeed * 10f, ForceMode.Force);
            else
                rb.AddForce(moveDirection.normalized * moveSpeed * 10f * airMultiplier, ForceMode.Force);
        }
        else if (grounded)
        {
            // No input - apply stopping force for snappier feel
            Vector3 flatVel = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
            rb.AddForce(-flatVel * stopForce, ForceMode.Force);
        }
    }

    private void SpeedControl()
    {
        Vector3 flatVel = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
        if (flatVel.magnitude > moveSpeed)
        {
            Vector3 limited = flatVel.normalized * moveSpeed;
            rb.linearVelocity = new Vector3(limited.x, rb.linearVelocity.y, limited.z);
        }
    }

    private void RotateModelToCamera()
    {
        // rotate player Y to match camera yaw
        Vector3 camEuler = orientation.eulerAngles;
        transform.rotation = Quaternion.Euler(0f, camEuler.y, 0f);
    }

    private void Jump()
    {
        rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
        rb.AddForce(transform.up * jumpForce, ForceMode.Impulse);
        jumps--;
    }

    void ResetJump()
    {
        jumps++;
        readyToJump = true;
    }

    void ResetExtraJump()
    {
        extraJumps++;
    }
}

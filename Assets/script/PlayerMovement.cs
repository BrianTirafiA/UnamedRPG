using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float walkSpeed = 5f;
    [SerializeField] private float runSpeed = 8f;

    [Header("Jumping")]
    [SerializeField] private float jumpForce = 7f;
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private Transform groundCheck;
    [SerializeField] private float groundCheckRadius = 0.2f;

    // Private state variables
    private Rigidbody rb;
    private Vector2 moveInput;
    private bool isGrounded;
    private bool jumpRequested = false;
    private Animator animator;
    private SpriteRenderer spriteRenderer;
    private bool isRunning;

    private Vector2 lastMoveDirection;

    private void Awake()
    {
        // Cache the Rigidbody for performance.
        rb = GetComponent<Rigidbody>();
        animator = GetComponentInChildren<Animator>();
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();
    }

    // This method is called by the "Player Input" component when the "Move" action is triggered.
    public void OnRun(InputAction.CallbackContext context)
    {
        if (context.performed)
            isRunning = true;
        if (context.canceled)
            isRunning = false;
    }

    public void OnMove(InputAction.CallbackContext context)
    {
        moveInput = context.ReadValue<Vector2>();
    }

    // This method is called by the "Player Input" component when the "Jump" action is triggered.
    public void OnJump(InputAction.CallbackContext context)
    {
        if (context.performed && isGrounded)
        {
            jumpRequested = true;
        }
    }

    private void Update()
    {
        // Perform the ground check in Update. A sphere cast is reliable.
        CheckGroundStatus();
    }

    private void FixedUpdate()
    {
        // Apply movement and jump forces in FixedUpdate for physics consistency.
        HandleMovement();
        HandleJump();
        UpdateAnimation();
    }

    private void UpdateAnimation()
    {
        bool isCurrentlyMoving = moveInput.sqrMagnitude > 0.1f;
        animator.SetBool("IsMoving", isCurrentlyMoving);
        animator.SetBool("IsRunning", isRunning);

        if (isCurrentlyMoving)
        {
            // Store the current movement direction
            lastMoveDirection = moveInput.normalized;

            // --- Logic for 4-directional animation ---
            // We prioritize vertical movement for the animation direction
            if (Mathf.Abs(moveInput.y) > Mathf.Abs(moveInput.x))
            {
                animator.SetFloat("MoveX", 0);
                animator.SetFloat("MoveY", moveInput.y > 0 ? 1 : -1);
            }
            else
            {
                animator.SetFloat("MoveX", moveInput.x > 0 ? 1 : -1);
                animator.SetFloat("MoveY", 0);
            }

            // --- Sprite Flipping Logic ---
            if (moveInput.x < -0.1f)
            {
                spriteRenderer.flipX = true; // Facing Left
            }
            else if (moveInput.x > 0.1f)
            {
                spriteRenderer.flipX = false; // Facing Right
            }
        }

        animator.SetFloat("lastX", lastMoveDirection.x);
        animator.SetFloat("lastY", lastMoveDirection.y);
    }

    private void CheckGroundStatus()
    {
        // Cast a small sphere at the groundCheck position to see if it hits anything on the ground layer.
        isGrounded = Physics.CheckSphere(groundCheck.position, groundCheckRadius, groundLayer);
    }

    private void HandleMovement()
    {
        // Get the camera's forward and right vectors
        Vector3 cameraForward = Camera.main.transform.forward;
        Vector3 cameraRight = Camera.main.transform.right;

        // Project the vectors onto the horizontal plane (by zeroing out the Y component)
        cameraForward.y = 0;
        cameraRight.y = 0;

        // Normalize to ensure consistent speed regardless of camera angle
        cameraForward.Normalize();
        cameraRight.Normalize();

        // Calculate the desired move direction relative to the camera
        float currentSpeed = isRunning ? runSpeed : walkSpeed;
        // Calculate the desired move direction relative to the camera
        Vector3 moveDirection = (cameraForward * moveInput.y + cameraRight * moveInput.x);

        // Set the Rigidbody's velocity using the new currentSpeed variable.
        rb.linearVelocity = new Vector3(moveDirection.x * currentSpeed, rb.linearVelocity.y, moveDirection.z * currentSpeed);
    }

    private void HandleJump()
    {
        if (jumpRequested)
        {
            // Apply an instant upward force for the jump.
            rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
            jumpRequested = false;
        }
    }

    // Optional: Draw a gizmo in the editor to visualize the ground check area.
    private void OnDrawGizmosSelected()
    {
        if (groundCheck != null)
        {
            Gizmos.color = isGrounded ? Color.green : Color.red;
            Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
        }
    }
}


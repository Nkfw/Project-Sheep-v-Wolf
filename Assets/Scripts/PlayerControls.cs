using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerControls : MonoBehaviour
{
    // ============================================
    // MOVEMENT SETTINGS (editable in Inspector)
    // ============================================

    [Header("Movement")]
    [SerializeField] private float movementSpeed = 5f;
    [SerializeField] private float rotationSpeed = 10f;

    [Header("Dash")]
    [SerializeField] private float dashDistance = 5f;        // How far the dash travels
    [SerializeField] private float dashDuration = 0.2f;      // How long the dash lasts in seconds
    [SerializeField] private float dashCooldown = 1f;        // Time before you can dash again

    // ============================================
    // PRIVATE VARIABLES (internal state)
    // ============================================

    // Input from keyboard/gamepad
    private Vector2 moveInput;

    // Dash state tracking
    private bool isDashing;
    private float dashTimer;              // Counts down during dash
    private float dashCooldownTimer;      // Counts down after dash ends
    private Vector3 dashDirection;        // Direction wolf dashes toward

    // Component references
    private CharacterController characterController;
    private Transform cameraTransform;
    private PlayerInteractions playerInteractions;

    // ============================================
    // UNITY LIFECYCLE METHODS
    // ============================================

    private void Awake()
    {
        // Get the CharacterController component attached to this GameObject
        characterController = GetComponent<CharacterController>();

        // Get the main camera's transform for camera-relative movement
        cameraTransform = Camera.main.transform;

        // Get the PlayerInteractions component for action/spell abilities
        playerInteractions = GetComponent<PlayerInteractions>();
    }

    private void Update()
    {
        // Handle dash cooldown and duration timers
        HandleDashTimers();

        // Move the wolf based on input
        HandleMovement();
    }

    // ============================================
    // MOVEMENT LOGIC
    // ============================================

    private void HandleMovement()
    {
        // If we are currently dashing, move in the dash direction instead of normal movement
        if (isDashing == true)
        {
            // Calculate dash speed: distance / time = speed
            float dashSpeed = dashDistance / dashDuration;

            // Move the wolf in the dash direction
            Vector3 dashMovement = dashDirection * dashSpeed * Time.deltaTime;
            characterController.Move(dashMovement);

            // Skip normal movement while dashing
            return;
        }

        // Check if player is pressing any movement keys
        // sqrMagnitude is faster than magnitude for simple "is there input?" checks
        bool hasMovementInput = moveInput.sqrMagnitude > 0.01f;

        if (hasMovementInput == false)
        {
            // No input, don't move
            return;
        }

        // Convert 2D input to 3D world direction based on camera orientation
        // This makes W always move toward the top of the screen
        Vector3 moveDirection = GetCameraRelativeDirection(moveInput);

        // Move the wolf
        Vector3 movement = moveDirection * movementSpeed * Time.deltaTime;
        characterController.Move(movement);

        // Rotate the wolf to face the movement direction
        Quaternion targetRotation = Quaternion.LookRotation(moveDirection);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
    }

    /// <summary>
    /// Converts 2D input (WASD) into a 3D direction relative to where the camera is looking.
    /// This makes controls feel natural - pressing W always moves "forward" on screen.
    /// </summary>
    private Vector3 GetCameraRelativeDirection(Vector2 input)
    {
        // Get the camera's forward and right directions
        Vector3 cameraForward = cameraTransform.forward;
        Vector3 cameraRight = cameraTransform.right;

        // Flatten to horizontal plane (ignore camera tilt)
        // We set Y to 0 so the wolf doesn't try to move up/down
        cameraForward.y = 0f;
        cameraRight.y = 0f;

        // Normalize to get unit vectors (length of 1)
        cameraForward.Normalize();
        cameraRight.Normalize();

        // Combine camera directions with input to get final movement direction
        // input.y = forward/backward (W/S), input.x = left/right (A/D)
        Vector3 moveDirection = cameraForward * input.y + cameraRight * input.x;

        return moveDirection;
    }

    // ============================================
    // DASH LOGIC
    // ============================================

    private void HandleDashTimers()
    {
        // Count down the cooldown timer
        if (dashCooldownTimer > 0f)
        {
            dashCooldownTimer = dashCooldownTimer - Time.deltaTime;
        }

        // Count down the dash duration timer
        if (isDashing == true)
        {
            dashTimer = dashTimer - Time.deltaTime;

            // Check if dash is finished
            if (dashTimer <= 0f)
            {
                isDashing = false;
            }
        }
    }

    /// <summary>
    /// Starts a dash in the current movement direction (or forward if not moving).
    /// </summary>
    private void StartDash()
    {
        // Determine dash direction based on current input
        bool hasMovementInput = moveInput.sqrMagnitude > 0.01f;

        if (hasMovementInput == true)
        {
            // Dash in the direction we're moving
            dashDirection = GetCameraRelativeDirection(moveInput);
        }
        else
        {
            // No movement input, dash forward
            dashDirection = transform.forward;
        }

        // Make sure dash direction has length of 1
        dashDirection = dashDirection.normalized;

        // Start the dash
        isDashing = true;
        dashTimer = dashDuration;
        dashCooldownTimer = dashCooldown;
    }

    // ============================================
    // INPUT SYSTEM CALLBACKS
    // These methods are called automatically by Unity's Input System
    // ============================================

    /// <summary>
    /// Called when player presses WASD or moves gamepad stick.
    /// </summary>
    public void OnMove(InputAction.CallbackContext context)
    {
        // Read the 2D input value (x = left/right, y = forward/backward)
        moveInput = context.ReadValue<Vector2>();
    }

    /// <summary>
    /// Called when player presses the Dash button (Left Shift).
    /// </summary>
    public void OnDash(InputAction.CallbackContext context)
    {
        // Only trigger on button press, not release
        if (context.performed == false)
        {
            return;
        }

        // Don't dash if we're already dashing
        if (isDashing == true)
        {
            return;
        }

        // Don't dash if cooldown hasn't finished
        if (dashCooldownTimer > 0f)
        {
            return;
        }

        // All checks passed, start the dash!
        StartDash();
    }

    /// <summary>
    /// Called when player presses the Action button (E).
    /// Used for area-based abilities around the player.
    /// </summary>
    public void OnAction(InputAction.CallbackContext context)
    {
        // Only trigger on button press, not release
        if (context.performed == false)
        {
            return;
        }

        // Make sure we have the PlayerInteractions component
        if (playerInteractions == null)
        {
            Debug.LogWarning("PlayerInteractions component not found!");
            return;
        }

        // Perform the action ability
        playerInteractions.PerformAction();
    }

    /// <summary>
    /// Called when player presses the Spell button (Q).
    /// Used for directional abilities in front of the player.
    /// </summary>
    public void OnSpell(InputAction.CallbackContext context)
    {
        // Only trigger on button press, not release
        if (context.performed == false)
        {
            return;
        }

        // Make sure we have the PlayerInteractions component
        if (playerInteractions == null)
        {
            Debug.LogWarning("PlayerInteractions component not found!");
            return;
        }

        // Perform the spell ability
        playerInteractions.PerformSpell();
    }
}

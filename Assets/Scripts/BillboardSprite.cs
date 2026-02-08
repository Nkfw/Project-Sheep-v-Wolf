using UnityEngine;

/// <summary>
/// Makes a 2D sprite always face the camera in a 3D world.
/// Attach this to a child object of the player that holds the SpriteRenderer.
/// This creates the "Don't Starve" style look where 2D characters exist in a 3D world.
/// </summary>
public class BillboardSprite : MonoBehaviour
{
    // ============================================
    // SETTINGS (editable in Inspector)
    // ============================================

    [Header("Billboard Settings")]
    [Tooltip("If true, the sprite will face the camera. If false, billboarding is disabled.")]
    [SerializeField] private bool billboardEnabled = true;

    [Tooltip("If true, flips the sprite horizontally when moving left.")]
    [SerializeField] private bool flipBasedOnMovement = true;

    // ============================================
    // PRIVATE VARIABLES
    // ============================================

    // Reference to the main camera
    private Camera mainCamera;

    // Reference to the SpriteRenderer (for flipping)
    private SpriteRenderer spriteRenderer;

    // Reference to the parent's position (to track movement direction)
    private Vector3 lastPosition;

    // ============================================
    // UNITY LIFECYCLE METHODS
    // ============================================

    private void Start()
    {
        // Get the main camera
        mainCamera = Camera.main;

        // Get the SpriteRenderer component
        spriteRenderer = GetComponent<SpriteRenderer>();

        // Store initial position
        lastPosition = transform.parent.position;
    }

    private void LateUpdate()
    {
        // LateUpdate runs after all Update methods
        // This ensures the billboard faces camera after all movement is done

        if (billboardEnabled == true && mainCamera != null)
        {
            FaceCamera();
        }

        if (flipBasedOnMovement == true && spriteRenderer != null)
        {
            UpdateSpriteFlip();
        }
    }

    // ============================================
    // BILLBOARD LOGIC
    // ============================================

    /// <summary>
    /// Rotates the sprite to always face the camera.
    /// The sprite matches the camera's rotation so it stays upright and visible.
    /// </summary>
    private void FaceCamera()
    {
        // Get the camera's forward direction (where it's looking)
        Vector3 cameraForward = mainCamera.transform.forward;

        // We want the sprite to face OPPOSITE to where the camera is looking
        // so the sprite faces TOWARD the camera
        Vector3 lookDirection = -cameraForward;

        // Create a rotation that looks in that direction
        Quaternion targetRotation = Quaternion.LookRotation(lookDirection);

        // Apply the rotation to the sprite
        transform.rotation = targetRotation;
    }

    /// <summary>
    /// Flips the sprite horizontally based on movement direction.
    /// Moving right = normal, moving left = flipped.
    /// </summary>
    private void UpdateSpriteFlip()
    {
        // Calculate movement since last frame
        Vector3 currentPosition = transform.parent.position;
        Vector3 movement = currentPosition - lastPosition;

        // Only flip if there's significant horizontal movement
        if (Mathf.Abs(movement.x) > 0.01f)
        {
            // Moving right = not flipped, moving left = flipped
            spriteRenderer.flipX = movement.x < 0;
        }

        // Store position for next frame
        lastPosition = currentPosition;
    }
}

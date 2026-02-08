using UnityEngine;
using UnityEngine.AI;

public class AnimalMovement : MonoBehaviour
{
    // ============================================
    // MOVEMENT SETTINGS (editable in Inspector)
    // ============================================

    [Header("Target")]
    [Tooltip("The destination point the animal walks toward.")]
    [SerializeField] private GameObject targetObject;
    [SerializeField] private Vector3 targetPosition;

    [Tooltip("How close the animal needs to be to the target before being destroyed.")]
    [SerializeField] private float arrivalDistance = 1f;

    [Header("Speed")]
    [Tooltip("How fast the animal moves toward the target.")]
    [SerializeField] private float movementSpeed = 3f;

    [Header("Wandering (Natural Movement)")]
    [Tooltip("How far the animal can drift up/down (Z axis) from the direct path.")]
    [SerializeField] private float wanderStrength = 2f;

    [Tooltip("How often the animal changes its wander direction (in seconds).")]
    [SerializeField] private float wanderChangeInterval = 1.5f;

    [Header("Sprite Settings")]
    [Tooltip("Reference to the child object that holds the SpriteRenderer.")]
    [SerializeField] private Transform spriteTransform;

    [Tooltip("If true, the sprite will always face the camera (billboard effect).")]
    [SerializeField] private bool billboardEnabled = true;

    [Tooltip("If true, the sprite flips horizontally based on movement direction.")]
    [SerializeField] private bool flipBasedOnMovement = true;

    // ============================================
    // PRIVATE VARIABLES
    // ============================================

    // Reference to the NavMeshAgent component
    private NavMeshAgent navMeshAgent;

    // Reference to the main camera for billboard effect
    private Camera mainCamera;

    // Reference to the SpriteRenderer for flipping
    private SpriteRenderer spriteRenderer;

    // The current wander offset on the Z axis
    private float currentWanderOffset;

    // The target wander offset we're smoothly moving toward
    private float targetWanderOffset;

    // Timer for changing wander direction
    private float wanderTimer;

    // How smoothly the wander offset changes
    private float wanderSmoothSpeed = 2f;

    // The current destination with wander offset applied
    private Vector3 currentDestination;

    // Last position for tracking movement direction
    private Vector3 lastPosition;

    // ============================================
    // UNITY LIFECYCLE METHODS
    // ============================================

    private void Start()
    {
        // Get the NavMeshAgent component
        navMeshAgent = GetComponent<NavMeshAgent>();

        // Make sure we have a NavMeshAgent
        if (navMeshAgent == null)
        {
            Debug.LogError("AnimalMovement: No NavMeshAgent found! Please add one to the animal prefab.");
            return;
        }

        // Set up the NavMeshAgent speed
        navMeshAgent.speed = movementSpeed;

        // Get the main camera for billboard effect
        mainCamera = Camera.main;

        // Try to find the SpriteRenderer on the sprite child object
        if (spriteTransform != null)
        {
            spriteRenderer = spriteTransform.GetComponent<SpriteRenderer>();
        }

        // Set the target position from the target object if assigned
        if (targetObject != null)
        {
            targetObject = GameObject.FindWithTag("Home");
            targetPosition = targetObject.transform.position;
        }

        // Pick a random initial wander direction
        targetWanderOffset = Random.Range(-1f, 1f);
        currentWanderOffset = targetWanderOffset;

        // Store initial position for movement tracking
        lastPosition = transform.position;

        // Set the initial destination
        UpdateDestination();

        // Register with AnimalRegistry so WaveManager can modify our speed
        if (AnimalRegistry.Instance != null)
        {
            AnimalRegistry.Instance.RegisterAnimal(this);
        }
    }

    private void Update()
    {
        // Make sure we have the NavMeshAgent
        if (navMeshAgent == null)
        {
            return;
        }

        // Update the wandering behavior
        UpdateWander();

        // Update the destination with new wander offset
        UpdateDestination();

        // Check if we've arrived at the final target
        CheckArrival();
    }

    private void LateUpdate()
    {
        // Handle sprite billboard and flipping after all movement is done
        UpdateSpriteBillboard();
        UpdateSpriteFlip();
    }

    private void OnDestroy()
    {
        // Unregister from AnimalRegistry when destroyed
        if (AnimalRegistry.Instance != null)
        {
            AnimalRegistry.Instance.UnregisterAnimal(this);
        }
    }

    // ============================================
    // PUBLIC METHODS
    // ============================================

    /// <summary>
    /// Sets the movement speed at runtime.
    /// Called by AnimalRegistry when wave effects change the speed.
    /// </summary>
    public void SetSpeed(float newSpeed)
    {
        movementSpeed = newSpeed;

        // Update the NavMeshAgent speed if it exists
        if (navMeshAgent != null)
        {
            navMeshAgent.speed = movementSpeed;
        }
    }

    // ============================================
    // NAVIGATION LOGIC
    // ============================================

    /// <summary>
    /// Updates the NavMeshAgent destination with the current wander offset.
    /// The agent automatically finds a path around obstacles.
    /// </summary>
    private void UpdateDestination()
    {
        // Calculate the destination with wander offset
        // We add a Z offset to make the animal wander naturally
        currentDestination = targetPosition;
        currentDestination.z = targetPosition.z + (currentWanderOffset * wanderStrength);

        // Tell the NavMeshAgent to go to this destination
        // The agent will automatically path around NavMesh obstacles (forests)
        navMeshAgent.SetDestination(currentDestination);
    }

    /// <summary>
    /// Updates the wandering behavior, making the animal drift up/down naturally.
    /// </summary>
    private void UpdateWander()
    {
        // Count up the wander timer
        wanderTimer = wanderTimer + Time.deltaTime;

        // Check if it's time to pick a new wander direction
        if (wanderTimer >= wanderChangeInterval)
        {
            // Pick a new random wander offset between -1 and 1
            targetWanderOffset = Random.Range(-1f, 1f);

            // Reset the timer
            wanderTimer = 0f;
        }

        // Smoothly move current offset toward target offset
        // This prevents jerky direction changes
        currentWanderOffset = Mathf.Lerp(currentWanderOffset, targetWanderOffset, wanderSmoothSpeed * Time.deltaTime);
    }

    /// <summary>
    /// Checks if the animal has reached the target and destroys it if so.
    /// </summary>
    private void CheckArrival()
    {
        // Calculate distance to the final target (ignoring Y axis)
        Vector3 toTarget = targetPosition - transform.position;
        toTarget.y = 0f;
        float distanceToTarget = toTarget.magnitude;

        // Check if we're close enough to the target
        if (distanceToTarget <= arrivalDistance)
        {
            // Animal has arrived, destroy it
            Destroy(gameObject);
        }
    }

    // ============================================
    // SPRITE BILLBOARD AND FLIP LOGIC
    // ============================================

    /// <summary>
    /// Makes the sprite always face the camera (Don't Starve style).
    /// The sprite stays upright and only rotates horizontally to face the camera.
    /// </summary>
    private void UpdateSpriteBillboard()
    {
        // Skip if billboard is disabled or we don't have required references
        if (billboardEnabled == false)
        {
            return;
        }

        if (spriteTransform == null || mainCamera == null)
        {
            return;
        }

        // Get the camera's forward direction (where it's looking)
        Vector3 cameraForward = mainCamera.transform.forward;

        // We want the sprite to face OPPOSITE to where the camera is looking
        // so the sprite faces TOWARD the camera
        Vector3 lookDirection = -cameraForward;

        // Create a rotation that looks in that direction
        Quaternion targetRotation = Quaternion.LookRotation(lookDirection);

        // Apply the rotation to the sprite
        spriteTransform.rotation = targetRotation;
    }

    /// <summary>
    /// Flips the sprite horizontally based on movement direction.
    /// </summary>
    private void UpdateSpriteFlip()
    {
        // Skip if flip is disabled or we don't have the SpriteRenderer
        if (flipBasedOnMovement == false)
        {
            return;
        }

        if (spriteRenderer == null)
        {
            return;
        }

        // Calculate movement since last frame
        Vector3 movement = transform.position - lastPosition;

        // Only flip if there's significant horizontal movement
        if (Mathf.Abs(movement.x) > 0.01f)
        {
            // Moving right (positive X) = not flipped
            // Moving left (negative X) = flipped
            spriteRenderer.flipX = movement.x < 0;
        }

        // Store position for next frame
        lastPosition = transform.position;
    }
}

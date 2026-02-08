using UnityEngine;
using UnityEngine.AI;

public class HunterController : MonoBehaviour
{
    // ============================================
    // WANDERING SETTINGS
    // ============================================

    [Header("Wandering")]
    [Tooltip("How far the hunter can wander from its starting position.")]
    [SerializeField] private float wanderRadius = 10f;

    [Tooltip("How long the hunter waits at each wander point before moving again.")]
    [SerializeField] private float wanderWaitTime = 2f;

    [Tooltip("How fast the hunter moves while wandering.")]
    [SerializeField] private float wanderSpeed = 2f;

    // ============================================
    // VISION SETTINGS
    // ============================================

    [Header("Vision")]
    [Tooltip("How far the hunter can see.")]
    [SerializeField] private float visionRange = 8f;

    [Tooltip("The angle of the hunter's vision cone (in degrees).")]
    [SerializeField] private float visionAngle = 60f;

    [Tooltip("Layer mask for obstacles that block line of sight.")]
    [SerializeField] private LayerMask obstacleLayerMask;

    // ============================================
    // DETECTION SETTINGS
    // ============================================

    [Header("Detection")]
    [Tooltip("How long the hunter must see the wolf before shooting (in seconds).")]
    [SerializeField] private float detectionTime = 1.5f;

    [Tooltip("How fast the hunter moves while chasing.")]
    [SerializeField] private float chaseSpeed = 4f;

    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip wolfSpotted;
    [SerializeField] private AudioClip shootWolf;


    // ============================================
    // SHOOTING SETTINGS
    // ============================================

    [Header("Shooting")]
    [Tooltip("How long after shooting before the hunter can act again.")]
    [SerializeField] private float shootCooldown = 1f;

    [Tooltip("The bullet prefab to spawn when shooting.")]
    [SerializeField] private GameObject bulletPrefab;

    [Tooltip("Where the bullet spawns from (if not set, uses hunter position).")]
    [SerializeField] private Transform bulletSpawnPoint;

    [Tooltip("Angle in degrees between each bullet in the spread.")]
    [SerializeField] private float bulletSpreadAngle = 15f;

    // ============================================
    // SPRITE SETTINGS
    // ============================================

    [Header("Sprite")]
    [Tooltip("Reference to the child object that holds the SpriteRenderer.")]
    [SerializeField] private Transform spriteTransform;

    [Tooltip("If true, the sprite will always face the camera.")]
    [SerializeField] private bool billboardEnabled = true;

    // ============================================
    // PRIVATE VARIABLES
    // ============================================

    // Hunter states
    private enum HunterState
    {
        Wandering,
        Alert,
        Shooting
    }

    private HunterState currentState = HunterState.Wandering;

    // Component references
    private NavMeshAgent navMeshAgent;
    private Camera mainCamera;
    private SpriteRenderer spriteRenderer;

    // Wolf reference
    private Transform wolfTransform;
    private PlayerInteractions wolfInteractions;

    // Wandering variables
    private Vector3 startPosition;
    private float wanderTimer;
    private bool isWaiting;

    // Detection variables
    private float currentDetectionProgress;

    // Shooting variables
    private float shootCooldownTimer;

    // How many bullets to fire per shot (1 = normal, 3 = shotgun spread)
    private int bulletsPerShot = 1;

    // Last position for sprite flipping
    private Vector3 lastPosition;

    // ============================================
    // UNITY LIFECYCLE METHODS
    // ============================================

    private void Start()
    {
        // Get the NavMeshAgent
        navMeshAgent = GetComponent<NavMeshAgent>();

        if (navMeshAgent == null)
        {
            Debug.LogError("HunterController: No NavMeshAgent found!");
            return;
        }

        // Set initial speed
        navMeshAgent.speed = wanderSpeed;

        // Get the main camera for billboard effect
        mainCamera = Camera.main;

        // Try to find the SpriteRenderer on the sprite child
        if (spriteTransform != null)
        {
            spriteRenderer = spriteTransform.GetComponent<SpriteRenderer>();
        }

        // Store starting position for wandering
        startPosition = transform.position;
        lastPosition = transform.position;

        // Find the wolf in the scene
        GameObject wolfObject = GameObject.FindGameObjectWithTag("Player");
        if (wolfObject != null)
        {
            wolfTransform = wolfObject.transform;
            wolfInteractions = wolfObject.GetComponent<PlayerInteractions>();
        }

        // Pick first wander destination
        PickNewWanderDestination();
    }

    private void Update()
    {
        // Handle the current state
        switch (currentState)
        {
            case HunterState.Wandering:
                UpdateWandering();
                break;

            case HunterState.Alert:
                UpdateAlert();
                break;

            case HunterState.Shooting:
                UpdateShooting();
                break;
        }

        // Always check for wolf visibility (except when shooting)
        if (currentState != HunterState.Shooting)
        {
            CheckForWolf();
        }
    }

    private void LateUpdate()
    {
        // Handle sprite billboard and flipping
        UpdateSpriteBillboard();
        UpdateSpriteFlip();
    }

    // ============================================
    // STATE: WANDERING
    // ============================================

    private void UpdateWandering()
    {
        // If waiting at a point, count down the timer
        if (isWaiting == true)
        {
            wanderTimer = wanderTimer - Time.deltaTime;

            if (wanderTimer <= 0f)
            {
                isWaiting = false;
                PickNewWanderDestination();
            }

            return;
        }

        // Check if we've reached the destination
        if (navMeshAgent.remainingDistance <= navMeshAgent.stoppingDistance)
        {
            // Start waiting
            isWaiting = true;
            wanderTimer = wanderWaitTime;
        }
    }

    private void PickNewWanderDestination()
    {
        // Pick a random point within the wander radius
        Vector2 randomCircle = Random.insideUnitCircle * wanderRadius;
        Vector3 randomPoint = startPosition + new Vector3(randomCircle.x, 0f, randomCircle.y);

        // Try to find a valid point on the NavMesh
        NavMeshHit hit;
        if (NavMesh.SamplePosition(randomPoint, out hit, wanderRadius, NavMesh.AllAreas))
        {
            navMeshAgent.SetDestination(hit.position);
        }
    }

    // ============================================
    // STATE: ALERT (Detecting Wolf)
    // ============================================

    private void UpdateAlert()
    {
        // Make sure we still have a valid wolf reference
        if (wolfTransform == null)
        {
            EnterWanderingState();
            return;
        }

        // Face the wolf while alert
        Vector3 directionToWolf = wolfTransform.position - transform.position;
        directionToWolf.y = 0f;

        if (directionToWolf.sqrMagnitude > 0.01f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(directionToWolf);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, 5f * Time.deltaTime);
        }

        // Stop moving while alert
        navMeshAgent.SetDestination(transform.position);

        // Increase detection progress
        currentDetectionProgress = currentDetectionProgress + Time.deltaTime;

        // Check if detection is complete
        if (currentDetectionProgress >= detectionTime)
        {
            EnterShootingState();
        }
    }

    private void EnterAlertState()
    {
        currentState = HunterState.Alert;
        currentDetectionProgress = 0f;
        
        audioSource.PlayOneShot(wolfSpotted);

        Debug.Log("Hunter: ALERT! I see the wolf!");
    }

    // ============================================
    // STATE: SHOOTING
    // ============================================

    private void UpdateShooting()
    {
        // Count down the cooldown timer
        shootCooldownTimer = shootCooldownTimer - Time.deltaTime;

        if (shootCooldownTimer <= 0f)
        {
            // Go back to wandering
            EnterWanderingState();
        }
    }

    private void EnterShootingState()
    {
        currentState = HunterState.Shooting;
        shootCooldownTimer = shootCooldown;

        // Shoot at the wolf!
        ShootWolf();

        audioSource.PlayOneShot(shootWolf);

        Debug.Log("Hunter: BANG! Shooting the wolf!");
    }

    private void ShootWolf()
    {
        // Check if we have a bullet prefab
        if (bulletPrefab == null)
        {
            Debug.LogError("HunterController: No bullet prefab assigned!");
            return;
        }

        // Check if wolf exists
        if (wolfTransform == null)
        {
            return;
        }

        // Determine spawn position
        Vector3 spawnPosition;
        if (bulletSpawnPoint != null)
        {
            spawnPosition = bulletSpawnPoint.position;
        }
        else
        {
            // Default: spawn at hunter position, slightly forward and up
            spawnPosition = transform.position + transform.forward * 0.5f + Vector3.up * 1f;
        }

        // Calculate direction to wolf
        Vector3 directionToWolf = wolfTransform.position - spawnPosition;
        directionToWolf.y = 0f; // Keep bullet on same height
        directionToWolf.Normalize();

        // Spawn bullets in a spread pattern
        // For 1 bullet: just shoots straight
        // For 3 bullets: one straight, one at -15°, one at +15°
        for (int i = 0; i < bulletsPerShot; i++)
        {
            // Calculate the angle offset for this bullet
            // Centers the spread around 0 degrees
            // Example with 3 bullets and 15° spread: -15°, 0°, +15°
            float halfSpread = (bulletsPerShot - 1) * bulletSpreadAngle / 2f;
            float angleOffset = -halfSpread + (i * bulletSpreadAngle);

            // Rotate the direction by the angle offset
            Vector3 bulletDirection = Quaternion.Euler(0f, angleOffset, 0f) * directionToWolf;

            // Spawn the bullet
            GameObject bulletObject = Instantiate(bulletPrefab, spawnPosition, Quaternion.identity);

            // Set the bullet's direction
            HunterBullet bullet = bulletObject.GetComponent<HunterBullet>();
            if (bullet != null)
            {
                bullet.SetDirection(bulletDirection);
            }
            else
            {
                Debug.LogError("HunterController: Bullet prefab has no HunterBullet component!");
            }
        }
    }

    private void EnterWanderingState()
    {
        currentState = HunterState.Wandering;
        currentDetectionProgress = 0f;
        navMeshAgent.speed = wanderSpeed;
        isWaiting = false;

        PickNewWanderDestination();
    }

    // ============================================
    // PUBLIC METHODS
    // ============================================

    /// <summary>
    /// Sets how many bullets the hunter fires per shot.
    /// Called by WaveManager when the shotgun spread wave triggers.
    /// </summary>
    public void SetBulletsPerShot(int count)
    {
        bulletsPerShot = count;
        Debug.Log("Hunter: Now firing " + bulletsPerShot + " bullets per shot!");
    }

    // ============================================
    // VISION / DETECTION
    // ============================================

    private void CheckForWolf()
    {
        // Make sure we have a wolf reference
        if (wolfTransform == null)
        {
            return;
        }

        // Check if wolf is visible
        bool canSeeWolf = CanSeeWolf();

        if (canSeeWolf == true)
        {
            // If we're wandering, switch to alert
            if (currentState == HunterState.Wandering)
            {
                EnterAlertState();
            }
        }
        else
        {
            // If we were alert but lost sight, go back to wandering
            if (currentState == HunterState.Alert)
            {
                Debug.Log("Hunter: Lost sight of wolf, resuming patrol.");
                EnterWanderingState();
            }
        }
    }

    private bool CanSeeWolf()
    {
        // Check if wolf is hidden in forest
        if (wolfInteractions != null && wolfInteractions.IsHidden == true)
        {
            return false;
        }

        // Check distance
        Vector3 directionToWolf = wolfTransform.position - transform.position;
        float distanceToWolf = directionToWolf.magnitude;

        if (distanceToWolf > visionRange)
        {
            return false;
        }

        // Check angle (is wolf within vision cone?)
        directionToWolf.y = 0f;
        Vector3 hunterForward = transform.forward;
        hunterForward.y = 0f;

        float angleToWolf = Vector3.Angle(hunterForward, directionToWolf);

        if (angleToWolf > visionAngle / 2f)
        {
            return false;
        }

        // Check line of sight (raycast for obstacles)
        Vector3 rayStart = transform.position + Vector3.up * 0.5f;
        Vector3 rayDirection = (wolfTransform.position + Vector3.up * 0.5f) - rayStart;

        RaycastHit hit;
        if (Physics.Raycast(rayStart, rayDirection, out hit, visionRange, obstacleLayerMask))
        {
            // Something is blocking the view
            return false;
        }

        // Wolf is visible!
        return true;
    }

    // ============================================
    // SPRITE BILLBOARD AND FLIP
    // ============================================

    private void UpdateSpriteBillboard()
    {
        if (billboardEnabled == false)
        {
            return;
        }

        if (spriteTransform == null || mainCamera == null)
        {
            return;
        }

        // Get the camera's forward direction
        Vector3 cameraForward = mainCamera.transform.forward;

        // Face opposite to camera (toward the camera)
        Vector3 lookDirection = -cameraForward;

        // Create and apply rotation
        Quaternion targetRotation = Quaternion.LookRotation(lookDirection);
        spriteTransform.rotation = targetRotation;
    }

    private void UpdateSpriteFlip()
    {
        if (spriteRenderer == null)
        {
            return;
        }

        // Calculate movement since last frame
        Vector3 movement = transform.position - lastPosition;

        // Only flip if there's significant horizontal movement
        if (Mathf.Abs(movement.x) > 0.01f)
        {
            spriteRenderer.flipX = movement.x < 0;
        }

        lastPosition = transform.position;
    }

    // ============================================
    // DEBUG VISUALIZATION
    // ============================================

    private void OnDrawGizmosSelected()
    {
        // Draw wander radius
        Gizmos.color = Color.blue;
        Vector3 center = Application.isPlaying ? startPosition : transform.position;
        Gizmos.DrawWireSphere(center, wanderRadius);

        // Draw vision range
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, visionRange);

        // Draw vision cone
        Gizmos.color = Color.red;
        Vector3 forward = transform.forward * visionRange;
        float halfAngle = visionAngle / 2f;

        Vector3 leftEdge = Quaternion.Euler(0, -halfAngle, 0) * forward;
        Vector3 rightEdge = Quaternion.Euler(0, halfAngle, 0) * forward;

        Gizmos.DrawLine(transform.position, transform.position + leftEdge);
        Gizmos.DrawLine(transform.position, transform.position + rightEdge);
  
    }
}

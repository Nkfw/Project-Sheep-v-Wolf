using UnityEngine;

/// <summary>
/// A bullet projectile that flies toward the wolf.
/// Spawned by HunterController when shooting.
/// The wolf detects the bullet via PlayerInteractions.
/// </summary>
public class HunterBullet : MonoBehaviour
{
    // ============================================
    // SETTINGS (editable in Inspector)
    // ============================================

    [Header("Movement")]
    [Tooltip("How fast the bullet travels.")]
    [SerializeField] private float speed = 15f;

    [Header("Lifetime")]
    [Tooltip("How long the bullet lives before being destroyed (in seconds).")]
    [SerializeField] private float lifetime = 5f;

    // ============================================
    // PRIVATE VARIABLES
    // ============================================

    // Direction the bullet is traveling
    private Vector3 moveDirection;

    // Timer for auto-destruction
    private float lifetimeTimer;

    // ============================================
    // PUBLIC METHODS
    // ============================================

    /// <summary>
    /// Sets the direction the bullet should travel.
    /// Called by HunterController after spawning.
    /// </summary>
    public void SetDirection(Vector3 direction)
    {
        moveDirection = direction.normalized;
    }

    // ============================================
    // UNITY LIFECYCLE METHODS
    // ============================================

    private void Start()
    {
        lifetimeTimer = lifetime;
    }

    private void Update()
    {
        // Move the bullet forward
        transform.position = transform.position + (moveDirection * speed * Time.deltaTime);

        // Count down lifetime
        lifetimeTimer = lifetimeTimer - Time.deltaTime;

        if (lifetimeTimer <= 0f)
        {
            Destroy(gameObject);
        }
    }
}

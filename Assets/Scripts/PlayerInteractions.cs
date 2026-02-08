using System.Collections.Generic;
using UnityEngine;

public class PlayerInteractions : MonoBehaviour
{
    // ============================================
    // INTERACTION SETTINGS (editable in Inspector)
    // ============================================

    [Header("Detection Settings")]
    [Tooltip("Tag used to identify animals that can be eaten.")]
    [SerializeField] private string animalTag = "Animal";

    [Tooltip("Tag used to identify hunter bullets.")]
    [SerializeField] private string bulletTag = "Bullet";

    [Header("Action Settings (E Key)")]
    [Tooltip("Range for the action ability (affects animals within this radius).")]
    [SerializeField] private float actionRange = 3f;

    [Header("Spell Settings (Q Key)")]
    [Tooltip("Range for directional spells (affects animals in front within this distance).")]
    [SerializeField] private float spellRange = 5f;

    [Tooltip("Angle in degrees for directional spells (how wide the cone is).")]
    [SerializeField] private float spellAngle = 45f;

    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip eatAnimals;

    // ============================================
    // HIDING STATE (for stealth mechanics)
    // ============================================

    [SerializeField] int animalPoints = 1;   

    // Whether the player is currently hidden (in a forest)
    // Hunters will check this to decide if they can see the player
    private bool isHidden = false;

    // Public property so other scripts can check if player is hidden
    public bool IsHidden
    {
        get { return isHidden; }
    }

    // ============================================
    // PRIVATE VARIABLES
    // ============================================

    // List of animals currently in contact with the player (inside trigger)
    private List<GameObject> animalsInContact = new List<GameObject>();

    // ============================================
    // UNITY COLLISION EVENTS
    // ============================================

    /// <summary>
    /// Called when another collider enters our trigger collider.
    /// We use this to track which animals are touching the player.
    /// </summary>
    private void OnTriggerEnter(Collider other)
    {
        // Check if the object that entered is an animal
        if (other.CompareTag(animalTag) == true)
        {
            // Add to our list of animals in contact
            animalsInContact.Add(other.gameObject);

            // For now, destroy the animal immediately on contact
            EatAnimal(other.gameObject);
            audioSource.PlayOneShot(eatAnimals);
            return;
        }

        // Check if the object that entered is a bullet
        if (other.CompareTag(bulletTag) == true)
        {
            // Get hit by the bullet
            HitByBullet(other.gameObject);
        }
    }

    /// <summary>
    /// Called when another collider exits our trigger collider.
    /// We use this to remove animals from our tracking list.
    /// </summary>
    private void OnTriggerExit(Collider other)
    {
        // Check if the object that left is an animal
        if (other.CompareTag(animalTag) == true)
        {
            // Remove from our list
            animalsInContact.Remove(other.gameObject);
        }
    }

    // ============================================
    // EATING (BASIC INTERACTION)
    // ============================================

    /// <summary>
    /// Called when a bullet hits the player.
    /// </summary>
    private void HitByBullet(GameObject bullet)
    {
        // Destroy the bullet
        Destroy(bullet);

        // Deal damage to the wolf
        WolfHealth wolfHealth = GetComponent<WolfHealth>();
        if (wolfHealth != null)
        {
            wolfHealth.TakeDamage(1);
        }

        Debug.Log("Wolf got hit by a bullet!");
    }

    /// <summary>
    /// Destroys an animal when the player eats it.
    /// </summary>
    private void EatAnimal(GameObject animal)
    {
        // Remove from tracking list if present
        if (animalsInContact.Contains(animal) == true)
        {
            animalsInContact.Remove(animal);
        }

        ScoreManager.Instance.AddScore(animalPoints);

        // TODO: Add sound effect here later
        // TODO: Add particle effect here later

        // Destroy the animal
        Destroy(animal);

        Debug.Log("Ate an animal!");
    }

    // ============================================
    // ACTION ABILITY (E KEY) - Area Around Player
    // ============================================

    /// <summary>
    /// Called from PlayerControls when player presses the Action button (E).
    /// Affects all animals within actionRange radius around the player.
    /// </summary>
    public void PerformAction()
    {
        // Find all colliders within the action range
        Collider[] colliders = Physics.OverlapSphere(transform.position, actionRange);

        // Loop through each collider found
        for (int i = 0; i < colliders.Length; i++)
        {
            Collider collider = colliders[i];

            // Check if this is an animal
            if (collider.CompareTag(animalTag) == true)
            {
                // For now, just log it - we'll add actual effects later
                Debug.Log("Action hit animal: " + collider.gameObject.name);

                // TODO: Add action effect here (damage, stun, etc.)
            }
        }
    }

    // ============================================
    // SPELL ABILITY (Q KEY) - Cone In Front of Player
    // ============================================

    /// <summary>
    /// Called from PlayerControls when player presses the Spell button (Q).
    /// Affects animals in front of the player within spellRange and spellAngle.
    /// </summary>
    public void PerformSpell()
    {
        // Find all colliders within the spell range
        Collider[] colliders = Physics.OverlapSphere(transform.position, spellRange);

        // Loop through each collider found
        for (int i = 0; i < colliders.Length; i++)
        {
            Collider collider = colliders[i];

            // Check if this is an animal
            if (collider.CompareTag(animalTag) == false)
            {
                continue;
            }

            // Check if the animal is in front of the player (within the cone angle)
            bool isInFront = IsTargetInFront(collider.transform.position);

            if (isInFront == true)
            {
                // For now, just log it - we'll add actual effects later
                Debug.Log("Spell hit animal: " + collider.gameObject.name);

                // TODO: Add spell effect here (fire breath, etc.)
            }
        }
    }

    /// <summary>
    /// Checks if a target position is within the cone in front of the player.
    /// </summary>
    private bool IsTargetInFront(Vector3 targetPosition)
    {
        // Get direction from player to target
        Vector3 directionToTarget = targetPosition - transform.position;

        // Ignore Y difference (keep it horizontal)
        directionToTarget.y = 0f;

        // Get the player's forward direction (where they're facing)
        Vector3 playerForward = transform.forward;
        playerForward.y = 0f;

        // Calculate the angle between player's forward and direction to target
        float angleToTarget = Vector3.Angle(playerForward, directionToTarget);

        // Check if the angle is within our spell cone
        // We divide spellAngle by 2 because the angle is measured from center
        bool isWithinAngle = angleToTarget <= (spellAngle / 2f);

        return isWithinAngle;
    }

    // ============================================
    // HIDING SYSTEM (Called by ForestArea)
    // ============================================

    /// <summary>
    /// Sets the player's hidden state. Called by ForestArea when player enters/exits.
    /// </summary>
    public void SetHidden(bool hidden)
    {
        isHidden = hidden;

        // TODO: Add visual feedback here (change player color, show icon, etc.)
    }

    // ============================================
    // DEBUG VISUALIZATION
    // ============================================

    /// <summary>
    /// Draws gizmos in the Scene view to visualize the interaction ranges.
    /// </summary>
    private void OnDrawGizmosSelected()
    {
        // Draw action range (yellow circle)
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, actionRange);

        // Draw spell range (red circle)
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, spellRange);

        // Draw spell cone direction
        Gizmos.color = Color.red;
        Vector3 forward = transform.forward * spellRange;

        // Calculate cone edges
        float halfAngle = spellAngle / 2f;
        Vector3 leftEdge = Quaternion.Euler(0, -halfAngle, 0) * forward;
        Vector3 rightEdge = Quaternion.Euler(0, halfAngle, 0) * forward;

        Gizmos.DrawLine(transform.position, transform.position + leftEdge);
        Gizmos.DrawLine(transform.position, transform.position + rightEdge);
    }
}

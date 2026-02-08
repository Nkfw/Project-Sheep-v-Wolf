using UnityEngine;
using System;
using System.Collections;

/// <summary>
/// Handles the wolf's health and death.
/// Attach this to the Wolf/Player GameObject.
/// </summary>
/// 

public class WolfHealth : MonoBehaviour
{
    // ============================================
    // HEALTH SETTINGS (editable in Inspector)
    // ============================================

    [Header("Health")]
    [Tooltip("Maximum health the wolf can have.")]
    [SerializeField] private int maxHealth = 1;

    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip gameOver;

    [Header("Game Over")]
    [Tooltip("Delay before transitioning to game over screen (seconds).")]
    [SerializeField] private float gameOverDelay = 1.6f;

    // ============================================
    // PRIVATE VARIABLES
    // ============================================

    // Current health
    private int currentHealth;

    // Whether the wolf is dead
    private bool isDead = false;

    // ============================================
    // PUBLIC PROPERTIES
    // ============================================

    /// <summary>
    /// Returns the current health of the wolf.
    /// </summary>
    public int CurrentHealth
    {
        get { return currentHealth; }
    }

    /// <summary>
    /// Returns the maximum health of the wolf.
    /// </summary>
    public int MaxHealth
    {
        get { return maxHealth; }
    }

    /// <summary>
    /// Returns true if the wolf is dead.
    /// </summary>
    public bool IsDead
    {
        get { return isDead; }
    }

    // ============================================
    // UNITY LIFECYCLE METHODS
    // ============================================

    private void Start()
    {
        // Set health to max at start
        currentHealth = maxHealth;
    }

    private void Update()
    {
       
    }

    // ============================================
    // DAMAGE SYSTEM
    // ============================================

    /// <summary>
    /// Deals damage to the wolf. Called by hunters when they shoot.
    /// </summary>
    public void TakeDamage(int damage)
    {
        // Don't take damage if already dead
        if (isDead == true)
        {
            return;
        }

        // Apply damage
        currentHealth = currentHealth - damage;

        Debug.Log("Wolf: Took " + damage + " damage! Health: " + currentHealth + "/" + maxHealth);

        // Check for death
        if (currentHealth <= 0)
        {
            Die();
            return;
        }

        // TODO: Add visual feedback here (flash red, shake, etc.)
        // TODO: Add sound effect here
    }


    // ============================================
    // DEATH
    // ============================================

    /// <summary>
    /// Called when the wolf's health reaches zero.
    /// </summary>
    public event Action OnWolfDied; // Event triggered when the wolf dies

    private void Die()
    {
        isDead = true;

        Debug.Log("Wolf: DEAD! Game Over!");

        // Disable the wolf's movement and interactions immediately
        PlayerControls playerControls = GetComponent<PlayerControls>();
        if (playerControls != null)
        {
            playerControls.enabled = false;
        }

        PlayerInteractions playerInteractions = GetComponent<PlayerInteractions>();
        if (playerInteractions != null)
        {
            playerInteractions.enabled = false;
        }

        // Play game over sound and trigger event after delay
        audioSource.PlayOneShot(gameOver);
        StartCoroutine(TriggerGameOverAfterDelay());
    }

    /// <summary>
    /// Waits for the game over sound to play, then fires the death event.
    /// </summary>
    private IEnumerator TriggerGameOverAfterDelay()
    {
        // Wait for the delay so the sound can play
        yield return new WaitForSeconds(gameOverDelay);

        // Fire the death event to trigger scene transition
        if (OnWolfDied != null)
        {
            OnWolfDied.Invoke();
        }
    }


}

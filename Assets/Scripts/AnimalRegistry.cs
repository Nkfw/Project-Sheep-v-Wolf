using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Singleton that tracks all active animals in the scene.
/// Allows systems like WaveManager to modify all animals at once (e.g., change speed).
/// </summary>
public class AnimalRegistry : MonoBehaviour
{
    // ============================================
    // SINGLETON
    // ============================================

    public static AnimalRegistry Instance;

    // ============================================
    // CONFIGURATION
    // ============================================

    [Header("Speed Settings")]
    [Tooltip("The base speed for animals before any multipliers.")]
    [SerializeField] private float baseAnimalSpeed = 3f;

    // ============================================
    // PRIVATE VARIABLES
    // ============================================

    // List of all active animals in the scene
    private List<AnimalMovement> activeAnimals = new List<AnimalMovement>();

    // Current speed multiplier applied to all animals (1.0 = normal speed)
    private float currentSpeedMultiplier = 1f;

    // ============================================
    // UNITY LIFECYCLE
    // ============================================

    private void Awake()
    {
        // Set up singleton
        if (Instance == null)
        {
            Instance = this;
            Debug.Log("AnimalRegistry instance set.");
        }
        else
        {
            Debug.LogWarning("AnimalRegistry: Duplicate instance found, destroying this one.");
            Destroy(gameObject);
        }
    }

    // ============================================
    // PUBLIC METHODS
    // ============================================

    /// <summary>
    /// Registers an animal with the registry.
    /// Called by AnimalMovement.Start() when an animal spawns.
    /// </summary>
    public void RegisterAnimal(AnimalMovement animal)
    {
        // Don't add duplicates
        if (activeAnimals.Contains(animal) == true)
        {
            return;
        }

        activeAnimals.Add(animal);

        // Apply current speed multiplier to the new animal
        float adjustedSpeed = baseAnimalSpeed * currentSpeedMultiplier;
        animal.SetSpeed(adjustedSpeed);

        Debug.Log("AnimalRegistry: Registered animal. Total count: " + activeAnimals.Count);
    }

    /// <summary>
    /// Unregisters an animal from the registry.
    /// Called by AnimalMovement.OnDestroy() when an animal is destroyed.
    /// </summary>
    public void UnregisterAnimal(AnimalMovement animal)
    {
        if (activeAnimals.Contains(animal) == true)
        {
            activeAnimals.Remove(animal);
            Debug.Log("AnimalRegistry: Unregistered animal. Total count: " + activeAnimals.Count);
        }
    }

    /// <summary>
    /// Sets the speed multiplier for ALL animals (existing and future).
    /// Called by WaveManager when a wave effect changes animal speed.
    /// </summary>
    public void SetSpeedMultiplier(float multiplier)
    {
        currentSpeedMultiplier = multiplier;

        // Calculate the new speed
        float adjustedSpeed = baseAnimalSpeed * currentSpeedMultiplier;

        // Update all existing animals
        for (int i = 0; i < activeAnimals.Count; i++)
        {
            AnimalMovement animal = activeAnimals[i];

            // Make sure the animal still exists (might have been destroyed)
            if (animal != null)
            {
                animal.SetSpeed(adjustedSpeed);
            }
        }

        Debug.Log("AnimalRegistry: Set speed multiplier to " + multiplier +
                  " (adjusted speed: " + adjustedSpeed + ")");
    }

    /// <summary>
    /// Returns the current speed multiplier.
    /// Useful for debugging or UI display.
    /// </summary>
    public float GetCurrentSpeedMultiplier()
    {
        return currentSpeedMultiplier;
    }

    /// <summary>
    /// Returns the number of active animals.
    /// Useful for debugging or UI display.
    /// </summary>
    public int GetActiveAnimalCount()
    {
        return activeAnimals.Count;
    }

    /// <summary>
    /// Resets the speed multiplier back to normal.
    /// Call this when restarting the game.
    /// </summary>
    public void ResetSpeedMultiplier()
    {
        SetSpeedMultiplier(1f);
    }
}

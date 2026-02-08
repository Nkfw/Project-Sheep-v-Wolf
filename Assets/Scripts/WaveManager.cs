using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Manages wave mechanics and difficulty progression.
/// Listens to score changes and triggers effects when thresholds are reached.
/// </summary>
public class WaveManager : MonoBehaviour
{
    // ============================================
    // SINGLETON
    // ============================================

    public static WaveManager Instance;

    // ============================================
    // WAVE EFFECT TYPES
    // ============================================

    /// <summary>
    /// Types of effects that can be triggered at score thresholds.
    /// Add new effect types here as needed.
    /// </summary>
    public enum WaveEffectType
    {
        SpawnHunters,
        ChangeAnimalSpeed,
        ChangeHunterBullets
    }

    // ============================================
    // WAVE THRESHOLD DATA
    // ============================================

    /// <summary>
    /// Defines a score threshold and what effect to trigger.
    /// </summary>
    [Serializable]
    public class WaveThreshold
    {
        [Tooltip("The score at which this effect triggers.")]
        public int scoreThreshold;

        [Tooltip("What type of effect to trigger.")]
        public WaveEffectType effectType;

        [Tooltip("Integer parameter (e.g., number of hunters to spawn).")]
        public int intParameter;

        [Tooltip("Float parameter (e.g., speed multiplier).")]
        public float floatParameter = 1f;

        [Tooltip("Has this threshold already been triggered?")]
        [HideInInspector]
        public bool hasTriggered;
    }

    // ============================================
    // CONFIGURATION
    // ============================================

    [Header("Wave Thresholds")]
    [Tooltip("List of score thresholds and their effects.")]
    [SerializeField] private List<WaveThreshold> waveThresholds = new List<WaveThreshold>();

    [Header("Hunter Spawning")]
    [Tooltip("Hunter prefab to spawn when SpawnHunters effect triggers.")]
    [SerializeField] private GameObject hunterPrefab;

    [Tooltip("Possible spawn positions for hunters.")]
    [SerializeField] private List<Transform> hunterSpawnPoints = new List<Transform>();

    // ============================================
    // PRIVATE VARIABLES
    // ============================================

    // Tracks which spawn point to use next (cycles through the list)
    private int nextSpawnPointIndex = 0;

    // Current bullets per shot for hunters (applied to newly spawned hunters)
    private int currentHunterBulletCount = 1;

    // ============================================
    // UNITY LIFECYCLE
    // ============================================

    private void Awake()
    {
        // Set up singleton
        if (Instance == null)
        {
            Instance = this;
            Debug.Log("WaveManager instance set.");
        }
        else
        {
            Debug.LogWarning("WaveManager: Duplicate instance found, destroying this one.");
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        // Subscribe to score changes from ScoreManager
        if (ScoreManager.Instance != null)
        {
            ScoreManager.Instance.OnScoreChanged += HandleScoreChanged;
            Debug.Log("WaveManager: Subscribed to ScoreManager.OnScoreChanged");
        }
        else
        {
            Debug.LogWarning("WaveManager: ScoreManager.Instance not found! " +
                           "Make sure ScoreManager exists in the scene.");
        }
    }

    private void OnDestroy()
    {
        // Unsubscribe to prevent memory leaks
        if (ScoreManager.Instance != null)
        {
            ScoreManager.Instance.OnScoreChanged -= HandleScoreChanged;
        }
    }

    // ============================================
    // SCORE CHANGE HANDLER
    // ============================================

    /// <summary>
    /// Called when the player's score changes.
    /// Checks if any thresholds were crossed and triggers their effects.
    /// </summary>
    private void HandleScoreChanged(int oldScore, int newScore)
    {
        // Check each threshold to see if we just crossed it
        for (int i = 0; i < waveThresholds.Count; i++)
        {
            WaveThreshold threshold = waveThresholds[i];

            // Skip if this threshold was already triggered
            if (threshold.hasTriggered == true)
            {
                continue;
            }

            // Check if we just crossed this threshold
            // Old score was below the threshold, new score is at or above
            bool wasBelowThreshold = oldScore < threshold.scoreThreshold;
            bool isAtOrAboveThreshold = newScore >= threshold.scoreThreshold;
            bool justCrossedThreshold = wasBelowThreshold && isAtOrAboveThreshold;

            if (justCrossedThreshold == true)
            {
                // Mark as triggered so it won't fire again
                threshold.hasTriggered = true;

                // Execute the effect
                ExecuteEffect(threshold);
            }
        }
    }

    // ============================================
    // EFFECT EXECUTION
    // ============================================

    /// <summary>
    /// Executes the effect defined in a wave threshold.
    /// </summary>
    private void ExecuteEffect(WaveThreshold threshold)
    {
        Debug.Log("WaveManager: Triggering effect '" + threshold.effectType +
                  "' at score " + threshold.scoreThreshold);

        // Handle different effect types
        if (threshold.effectType == WaveEffectType.SpawnHunters)
        {
            SpawnHunters(threshold.intParameter);
        }
        else if (threshold.effectType == WaveEffectType.ChangeAnimalSpeed)
        {
            SetAnimalSpeedMultiplier(threshold.floatParameter);
        }
        else if (threshold.effectType == WaveEffectType.ChangeHunterBullets)
        {
            SetHunterBulletCount(threshold.intParameter);
        }
    }

    /// <summary>
    /// Spawns the specified number of hunters at available spawn points.
    /// </summary>
    public void SpawnHunters(int count)
    {
        // Validate hunter prefab
        if (hunterPrefab == null)
        {
            Debug.LogError("WaveManager: Cannot spawn hunters - no hunter prefab assigned!");
            return;
        }

        // Validate spawn points
        if (hunterSpawnPoints.Count == 0)
        {
            Debug.LogError("WaveManager: Cannot spawn hunters - no spawn points assigned!");
            return;
        }

        // Spawn the requested number of hunters
        for (int i = 0; i < count; i++)
        {
            // Get the next spawn point (cycle through the list)
            Transform spawnPoint = hunterSpawnPoints[nextSpawnPointIndex];
            nextSpawnPointIndex = (nextSpawnPointIndex + 1) % hunterSpawnPoints.Count;

            // Find a valid position on the NavMesh near the spawn point
            Vector3 spawnPosition = spawnPoint.position;
            NavMeshHit navMeshHit;
            bool foundValidPosition = NavMesh.SamplePosition(
                spawnPoint.position,
                out navMeshHit,
                5f,
                NavMesh.AllAreas
            );

            if (foundValidPosition == true)
            {
                spawnPosition = navMeshHit.position;
            }

            // Spawn the hunter
            GameObject hunterObject = Instantiate(hunterPrefab, spawnPosition, Quaternion.identity);

            // Apply current bullet count if it has been upgraded
            if (currentHunterBulletCount > 1)
            {
                HunterController hunterController = hunterObject.GetComponent<HunterController>();
                if (hunterController != null)
                {
                    hunterController.SetBulletsPerShot(currentHunterBulletCount);
                }
            }

            Debug.Log("WaveManager: Spawned hunter at " + spawnPosition);
        }
    }

    /// <summary>
    /// Sets the speed multiplier for all animals (existing and future).
    /// </summary>
    public void SetAnimalSpeedMultiplier(float multiplier)
    {
        if (AnimalRegistry.Instance != null)
        {
            AnimalRegistry.Instance.SetSpeedMultiplier(multiplier);
            Debug.Log("WaveManager: Set animal speed multiplier to " + multiplier);
        }
        else
        {
            Debug.LogWarning("WaveManager: AnimalRegistry.Instance not found! " +
                           "Make sure AnimalRegistry exists in the scene.");
        }
    }

    /// <summary>
    /// Sets the bullet count for ALL hunters (existing and future).
    /// Called when a ChangeHunterBullets threshold is crossed.
    /// </summary>
    public void SetHunterBulletCount(int bulletCount)
    {
        // Store for future hunters
        currentHunterBulletCount = bulletCount;

        // Find all existing hunters in the scene and update them
        HunterController[] allHunters = FindObjectsByType<HunterController>(FindObjectsSortMode.None);

        for (int i = 0; i < allHunters.Length; i++)
        {
            allHunters[i].SetBulletsPerShot(bulletCount);
        }

        Debug.Log("WaveManager: Set hunter bullet count to " + bulletCount +
                  " for " + allHunters.Length + " existing hunters.");
    }

    // ============================================
    // PUBLIC METHODS
    // ============================================

    /// <summary>
    /// Resets all thresholds so they can trigger again.
    /// Call this when restarting the game.
    /// </summary>
    public void ResetThresholds()
    {
        for (int i = 0; i < waveThresholds.Count; i++)
        {
            waveThresholds[i].hasTriggered = false;
        }

        // Also reset spawn point index
        nextSpawnPointIndex = 0;

        Debug.Log("WaveManager: All thresholds reset.");
    }

    /// <summary>
    /// Resets all wave-related systems.
    /// Call this when restarting the game.
    /// </summary>
    public void ResetAll()
    {
        ResetThresholds();

        // Reset animal speed if registry exists
        if (AnimalRegistry.Instance != null)
        {
            AnimalRegistry.Instance.ResetSpeedMultiplier();
        }

        // Reset hunter bullet count back to normal
        currentHunterBulletCount = 1;
    }
}

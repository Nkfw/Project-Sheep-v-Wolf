using System.Collections.Generic;
using UnityEngine;

public class AnimalSpawner : MonoBehaviour
{
    // ============================================
    // SPAWNER SETTINGS (editable in Inspector)
    // ============================================

    [Header("Animals to Spawn")]
    [Tooltip("List of animal prefabs. A random one will be picked each spawn.")]
    [SerializeField] private List<GameObject> animalPrefabs;

    [Header("Spawn Timing")]
    [Tooltip("Time in seconds between each spawn.")]
    public float spawnInterval = 4f;

    [Header("Spawn Position")]
    [Tooltip("X position where animals spawn.")]
    [SerializeField] private float spawnX = -28f;

    [Tooltip("Y position where animals spawn.")]
    [SerializeField] private float spawnY = 1f;

    [Tooltip("Minimum Z position for random spawn range.")]
    [SerializeField] private float spawnZMin = -14f;

    [Tooltip("Maximum Z position for random spawn range.")]
    [SerializeField] private float spawnZMax = 20f;

    // ============================================
    // PRIVATE VARIABLES
    // ============================================

    // Timer that counts up to spawnInterval
    private float spawnTimer;

    // ============================================
    // UNITY LIFECYCLE METHODS
    // ============================================

    private void Update()
    {
        // Count up the timer each frame
        spawnTimer = spawnTimer + Time.deltaTime;

        // Check if it's time to spawn a new animal
        if (spawnTimer >= spawnInterval)
        {
            SpawnAnimal();

            // Reset the timer
            spawnTimer = 0f;
        }
    }

    // ============================================
    // SPAWNING LOGIC
    // ============================================

    /// <summary>
    /// Spawns a random animal from the prefab list at a random position within the spawn range.
    /// </summary>
    private void SpawnAnimal()
    {
        // Make sure we have animals to spawn
        if (animalPrefabs == null || animalPrefabs.Count == 0)
        {
            Debug.LogWarning("AnimalSpawner: No animal prefabs assigned!");
            return;
        }

        // Pick a random animal from the list
        int randomIndex = Random.Range(0, animalPrefabs.Count);
        GameObject animalToSpawn = animalPrefabs[randomIndex];

        // Make sure the selected prefab is not null
        if (animalToSpawn == null)
        {
            Debug.LogWarning("AnimalSpawner: Animal prefab at index " + randomIndex + " is null!");
            return;
        }

        // Calculate random spawn position
        // X is fixed, Y is fixed, Z is random within range
        float randomZ = Random.Range(spawnZMin, spawnZMax);
        Vector3 spawnPosition = new Vector3(spawnX, spawnY, randomZ);

        // Spawn the animal at the calculated position with no rotation
        Instantiate(animalToSpawn, spawnPosition, Quaternion.identity);
    }
}

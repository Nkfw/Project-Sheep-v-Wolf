using UnityEngine;

public class ForestArea : MonoBehaviour
{
    // ============================================
    // FOREST SETTINGS (editable in Inspector)
    // ============================================

    [Header("Player Detection")]
    [Tooltip("Tag used to identify the player.")]
    [SerializeField] private string playerTag = "Player";

    // ============================================
    // UNITY TRIGGER EVENTS
    // ============================================

    /// <summary>
    /// Called when something enters the forest trigger area.
    /// If it's the player, they become hidden.
    /// </summary>
    private void OnTriggerEnter(Collider other)
    {
        // Check if the object that entered is the player
        if (other.CompareTag(playerTag) == false)
        {
            return;
        }

        // Try to get the PlayerInteractions component
        PlayerInteractions playerInteractions = other.GetComponent<PlayerInteractions>();

        if (playerInteractions == null)
        {
            Debug.LogWarning("ForestArea: Player entered but has no PlayerInteractions component!");
            return;
        }

        // Tell the player they are now hidden
        playerInteractions.SetHidden(true);

        Debug.Log("Player entered forest - now HIDDEN");
    }

    /// <summary>
    /// Called when something exits the forest trigger area.
    /// If it's the player, they become visible again.
    /// </summary>
    private void OnTriggerExit(Collider other)
    {
        // Check if the object that left is the player
        if (other.CompareTag(playerTag) == false)
        {
            return;
        }

        // Try to get the PlayerInteractions component
        PlayerInteractions playerInteractions = other.GetComponent<PlayerInteractions>();

        if (playerInteractions == null)
        {
            return;
        }

        // Tell the player they are now visible
        playerInteractions.SetHidden(false);

        Debug.Log("Player exited forest - now VISIBLE");
    }
}

using UnityEngine;

public class PlayerDoor : MonoBehaviour
{
    // The tag of the player that should trigger this door
    public string playerTag = "Player1"; // Set to "Player1" for DoorRed, "Player2" for DoorBlue

    // The visual indicator for when the door is activated
    public GameObject activationIndicator;

    // Tracks if the correct player is at this door
    private bool isPlayerAtDoor = false;

    // Debug mode
    public bool debugMode = true;

    private void Start()
    {
        // Hide the indicator at start
        if (activationIndicator != null)
        {
            activationIndicator.SetActive(false);
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (debugMode) Debug.Log("[PlayerDoor] " + gameObject.name + ": OnTriggerEnter2D with " + collision.gameObject.name + " (tag: " + collision.tag + ")");

        // Check if the correct player entered
        if (collision.CompareTag(playerTag))
        {
            if (debugMode) Debug.Log("[PlayerDoor] Correct player entered door: " + gameObject.name);
            isPlayerAtDoor = true;

            // Show activation indicator
            if (activationIndicator != null)
            {
                activationIndicator.SetActive(true);
            }

            // Find the LevelAdvancer to notify it
            LevelAdvancer levelAdvancer = FindObjectOfType<LevelAdvancer>();
            if (levelAdvancer != null)
            {
                levelAdvancer.CheckAllDoors();
            }
            else
            {
                Debug.LogError("[PlayerDoor] No LevelAdvancer found in the scene!");
            }
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (debugMode) Debug.Log("[PlayerDoor] " + gameObject.name + ": OnTriggerExit2D with " + collision.gameObject.name);

        // Check if the correct player left
        if (collision.CompareTag(playerTag))
        {
            if (debugMode) Debug.Log("[PlayerDoor] Player left door: " + gameObject.name);
            isPlayerAtDoor = false;

            // Hide activation indicator
            if (activationIndicator != null)
            {
                activationIndicator.SetActive(false);
            }

            // Find the LevelAdvancer to notify it
            LevelAdvancer levelAdvancer = FindObjectOfType<LevelAdvancer>();
            if (levelAdvancer != null)
            {
                levelAdvancer.CheckAllDoors();
            }
            else
            {
                Debug.LogError("[PlayerDoor] No LevelAdvancer found in the scene!");
            }
        }
    }

    // Returns true if the correct player is at this door
    public bool IsActivated()
    {
        return isPlayerAtDoor;
    }
}
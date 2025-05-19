using UnityEngine;

public class LevelAdvancer : MonoBehaviour
{
    // References to the two doors
    public PlayerDoor redDoor;   // For Player1
    public PlayerDoor blueDoor;  // For Player2

    // Optional wait time before triggering level change
    public float transitionDelay = 0.5f;

    // Cooldown to prevent multiple triggers
    private float advancementCooldown = 0f;
    public float cooldownDuration = 2f;

    // Debug mode
    public bool debugMode = true;

    private void Start()
    {
        // Try to find doors if not assigned
        if (redDoor == null || blueDoor == null)
        {
            PlayerDoor[] doors = FindObjectsOfType<PlayerDoor>();
            foreach (PlayerDoor door in doors)
            {
                if (door.gameObject.name.Contains("Red"))
                {
                    redDoor = door;
                }
                else if (door.gameObject.name.Contains("Blue"))
                {
                    blueDoor = door;
                }
            }
        }
    }

    private void Update()
    {
        // Update cooldown timer
        if (advancementCooldown > 0)
        {
            advancementCooldown -= Time.deltaTime;
        }
    }

    // Called by each door when a player enters or exits
    public void CheckAllDoors()
    {
        if (advancementCooldown > 0)
        {
            return; // Still on cooldown
        }

        if (debugMode) Debug.Log("[LevelAdvancer] Checking doors...");

        // Check if both doors are activated
        bool bothActivated = false;

        if (redDoor != null && blueDoor != null)
        {
            bothActivated = redDoor.IsActivated() && blueDoor.IsActivated();

            if (debugMode)
            {
                Debug.Log("[LevelAdvancer] Red Door: " + (redDoor.IsActivated() ? "ACTIVATED" : "inactive"));
                Debug.Log("[LevelAdvancer] Blue Door: " + (blueDoor.IsActivated() ? "ACTIVATED" : "inactive"));
            }
        }
        else
        {
            Debug.LogError("[LevelAdvancer] Doors not assigned properly!");
            return;
        }

        // If both doors are activated, advance to the next level
        if (bothActivated)
        {
            if (debugMode) Debug.Log("[LevelAdvancer] BOTH DOORS ACTIVATED! Advancing to next level.");
            AdvanceLevel();
        }
    }

    private void AdvanceLevel()
    {
        // Start cooldown to prevent multiple triggers
        advancementCooldown = cooldownDuration;

        // Wait for the specified delay before changing level
        if (transitionDelay > 0)
        {
            Invoke("TriggerLevelChange", transitionDelay);
        }
        else
        {
            TriggerLevelChange();
        }
    }

    private void TriggerLevelChange()
    {
        // Find LevelManager and tell it to go to next level
        LevelManager levelManager = FindObjectOfType<LevelManager>();
        if (levelManager != null)
        {
            levelManager.NextLevel();
        }
        else
        {
            Debug.LogError("[LevelAdvancer] No LevelManager found!");
        }
    }
}
using UnityEngine;
using System.Collections.Generic;

public class LevelManager : MonoBehaviour
{
    // Prefix for level objects
    public string levelPrefix = "level";

    // Level objects from the scene
    private List<GameObject> levelObjects = new List<GameObject>();

    // Current active level index
    private int currentLevelIndex = -1;

    // Transition settings
    public float fadeOutDuration = 0.5f;       // How long it takes to fade to black
    public float blackScreenDuration = 0.2f;   // Time to wait before fading back in
    public float fadeInDuration = 1.0f;        // How long it takes to fade back in

    // Debug mode
    public bool debugMode = true;

    private void Start()
    {
        Debug.Log("[LevelManager] Start called");

        // Find all level objects in the scene
        FindAllLevels();

        // Print all found levels
        Debug.Log("[LevelManager] Found levels: " + levelObjects.Count);
        foreach (GameObject level in levelObjects)
        {
            Debug.Log("[LevelManager] Level in list: " + level.name);
        }

        // Important: We DO NOT activate any level here by default
        // We assume level1 is already active in the scene editor

        // Just record which level is currently active
        for (int i = 0; i < levelObjects.Count; i++)
        {
            if (levelObjects[i].activeSelf)
            {
                currentLevelIndex = i;
                Debug.Log("[LevelManager] Detected active level at start: " + levelObjects[i].name + " (index " + i + ")");
                break;
            }
        }

        // If no level is active, warn but don't change anything
        if (currentLevelIndex == -1 && levelObjects.Count > 0)
        {
            Debug.LogWarning("[LevelManager] No level is currently active! Should have level1 active in editor.");
            currentLevelIndex = 0; // Assume first level
        }

        // Make sure FadeManager exists
        if (FadeManager.Instance == null)
        {
            GameObject fadeManagerObj = new GameObject("FadeManager");
            fadeManagerObj.AddComponent<FadeManager>();
        }
    }

    // Check for space bar input
    private void Update()
    {
        // Check if space key was pressed this frame
        if (Input.GetKeyDown(KeyCode.Space))
        {
            Debug.Log("[LevelManager] Space key pressed - restarting level");
            RestartCurrentLevel();
        }
    }

    private void FindAllLevels()
    {
        Debug.Log("[LevelManager] Finding all level objects in scene...");
        levelObjects.Clear();

        // Find all game objects in the scene
        GameObject[] allObjects = GameObject.FindObjectsOfType<GameObject>(true);

        // Find all root objects that start with the level prefix
        foreach (GameObject obj in allObjects)
        {
            if (obj.transform.parent == null && obj.name.StartsWith(levelPrefix))
            {
                levelObjects.Add(obj);
                Debug.Log("[LevelManager] Found level: " + obj.name + " (active: " + obj.activeSelf + ")");
            }
        }

        // Sort levels by name to ensure they're in the right order
        levelObjects.Sort((a, b) => a.name.CompareTo(b.name));
    }

    // Called when a level should be changed
    public void NextLevel()
    {
        if (debugMode) Debug.Log("[LevelManager] NextLevel called - Current level: " + currentLevelIndex);

        // Check if there are any levels
        if (levelObjects.Count == 0)
        {
            Debug.LogWarning("[LevelManager] No levels available!");
            return;
        }

        // Calculate next level index
        int nextIndex = (currentLevelIndex + 1) % levelObjects.Count;

        // Use the FadeManager for transition
        if (FadeManager.Instance != null)
        {
            Debug.Log("[LevelManager] Starting fade transition to level " + nextIndex);
            FadeManager.Instance.FadeOutAndIn(
                fadeOutDuration,
                blackScreenDuration,
                fadeInDuration,
                () => {
                    // This action happens during the black screen
                    ActivateLevel(nextIndex);
                }
            );
        }
        else
        {
            // If no FadeManager, just change level immediately
            Debug.LogWarning("[LevelManager] No FadeManager found! Changing level without transition.");
            ActivateLevel(nextIndex);
        }
    }

    // For compatibility with old scripts
    public void LevelCleared()
    {
        Debug.Log("[LevelManager] LevelCleared called (redirecting to NextLevel)");
        NextLevel();
    }

    private void ActivateLevel(int index)
    {
        if (levelObjects.Count == 0)
        {
            Debug.LogError("[LevelManager] No levels to activate!");
            return;
        }

        // Make sure index is valid
        index = Mathf.Clamp(index, 0, levelObjects.Count - 1);

        // Skip if already on this level
        if (index == currentLevelIndex)
        {
            Debug.Log("[LevelManager] Already on level " + index + ": " + levelObjects[index].name);
            return;
        }

        Debug.Log("[LevelManager] Activating level " + index + ": " + levelObjects[index].name);

        // Deactivate all levels EXCEPT the one we're activating
        foreach (GameObject levelObj in levelObjects)
        {
            if (levelObj == levelObjects[index])
                continue; // Skip the one we're about to activate

            Debug.Log("[LevelManager] Deactivating: " + levelObj.name);
            levelObj.SetActive(false);
        }

        // Activate the chosen level
        Debug.Log("[LevelManager] Activating: " + levelObjects[index].name);
        levelObjects[index].SetActive(true);
        currentLevelIndex = index;
    }

    // For testing in the editor
    [ContextMenu("Go To Next Level")]
    public void TestNextLevel()
    {
        NextLevel();
    }

    // For refreshing level list in the editor
    [ContextMenu("Refresh Level List")]
    public void RefreshLevelList()
    {
        FindAllLevels();
    }

    // Called when the current level should restart
    public void RestartCurrentLevel()
    {
        Debug.Log("[LevelManager] RestartCurrentLevel called - Current level: " + currentLevelIndex);

        // Check if there is a current level
        if (currentLevelIndex < 0 || levelObjects.Count == 0)
        {
            Debug.LogWarning("[LevelManager] No current level to restart!");
            return;
        }

        // Use the FadeManager for transition
        if (FadeManager.Instance != null)
        {
            Debug.Log("[LevelManager] Starting fade transition to restart level " + currentLevelIndex);
            FadeManager.Instance.FadeOutAndIn(
                fadeOutDuration,
                blackScreenDuration,
                fadeInDuration,
                () => {
                    // This action happens during the black screen
                    ResetCurrentLevel();
                }
            );
        }
        else
        {
            // If no FadeManager, just reset level immediately
            Debug.LogWarning("[LevelManager] No FadeManager found! Restarting level without transition.");
            ResetCurrentLevel();
        }
    }

    private void ResetCurrentLevel()
    {
        if (currentLevelIndex < 0 || currentLevelIndex >= levelObjects.Count)
        {
            Debug.LogError("[LevelManager] Invalid current level index: " + currentLevelIndex);
            return;
        }

        GameObject currentLevel = levelObjects[currentLevelIndex];

        // Find all ResettableObjects in the current level
        ResettableObject[] resettables = currentLevel.GetComponentsInChildren<ResettableObject>(true);

        // Reset each object
        Debug.Log("[LevelManager] Resetting " + resettables.Length + " objects in level " + currentLevelIndex);
        foreach (ResettableObject obj in resettables)
        {
            obj.ResetToInitialState();
        }

        // Additional reset logic can go here
        // For example, respawn the player, reset level timer, etc.
    }

    // For testing in the editor
    [ContextMenu("Restart Current Level")]
    public void TestRestartLevel()
    {
        RestartCurrentLevel();
    }
}
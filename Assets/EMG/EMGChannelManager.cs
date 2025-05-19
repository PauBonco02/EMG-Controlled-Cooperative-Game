using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// This sript is in charge of managing the signals coming from the EMG channels specified in the inspector.
/// It also implements a singleton pattern for easy global access from other scripts.
/// </summary>

[System.Serializable]
public class EMGChannelConfig
{
    public int sensorNumber = 1;
    public string channelName = "EMG Channel";
    public float threshold = 50f;
    public bool isEnabled = true;
    public Color signalColor = Color.green;
}

public class EMGChannelManager : MonoBehaviour
{
    public static EMGChannelManager Instance { get; private set; }

    // Control whether to load saved settings (add this toggle)
    [Header("Settings")]
    [Tooltip("If true, load saved settings from PlayerPrefs. If false, use values set in the Inspector.")]
    [SerializeField] private bool loadSavedSettings = false;

    [Header("Channel Configuration")]

    [SerializeField]
    private List<EMGChannelConfig> _channelConfigs = new List<EMGChannelConfig>
    {
        new EMGChannelConfig { sensorNumber = 13, channelName = "Channel 1", signalColor = Color.green },
        new EMGChannelConfig { sensorNumber = 15, channelName = "Channel 2", signalColor = Color.blue },
        new EMGChannelConfig { sensorNumber = 3, channelName = "Channel 3", signalColor = Color.yellow },
        new EMGChannelConfig { sensorNumber = 4, channelName = "Channel 4", signalColor = Color.magenta }
    };

    // Display settings that apply to all channels
    [SerializeField] private float _minDisplayRange = 0f;
    [SerializeField] private float _maxDisplayRange = 200f;
    [SerializeField] private int _averagingDuration = 100; // ms of signal taken each time to compute average power

    void Awake()
    {
        // Log initial channel values from Inspector
        Debug.Log("INITIAL channel sensor numbers: " +
                  _channelConfigs[0].sensorNumber + ", " +
                  _channelConfigs[1].sensorNumber + ", " +
                  _channelConfigs[2].sensorNumber + ", " +
                  _channelConfigs[3].sensorNumber);

        // Singleton pattern for easy access
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);

            // Only load saved settings if toggle is on
            if (loadSavedSettings)
            {
                Debug.Log("Loading settings from PlayerPrefs...");
                LoadCalibration();
            }
            else
            {
                Debug.Log("Using Inspector values (loadSavedSettings = false)");
            }
        }
        else
        {
            Destroy(gameObject);
        }

        // Log final channel values after any loading
        Debug.Log("FINAL channel sensor numbers: " +
                  _channelConfigs[0].sensorNumber + ", " +
                  _channelConfigs[1].sensorNumber + ", " +
                  _channelConfigs[2].sensorNumber + ", " +
                  _channelConfigs[3].sensorNumber);
    }

    // Add a method to clear saved settings
    public void ClearSavedSettings()
    {
        for (int i = 0; i < _channelConfigs.Count; i++)
        {
            PlayerPrefs.DeleteKey($"EMGChannel_{i}_SensorNumber");
            PlayerPrefs.DeleteKey($"EMGChannel_{i}_Name");
            PlayerPrefs.DeleteKey($"EMGChannel_{i}_Threshold");
            PlayerPrefs.DeleteKey($"EMGChannel_{i}_Enabled");
            PlayerPrefs.DeleteKey($"EMGChannel_{i}_ColorR");
            PlayerPrefs.DeleteKey($"EMGChannel_{i}_ColorG");
            PlayerPrefs.DeleteKey($"EMGChannel_{i}_ColorB");
        }

        PlayerPrefs.DeleteKey("EMGMinDisplayRange");
        PlayerPrefs.DeleteKey("EMGMaxDisplayRange");
        PlayerPrefs.DeleteKey("EMGAveragingDuration");
        PlayerPrefs.Save();

        Debug.Log("Cleared all saved EMG settings from PlayerPrefs");
    }

    public List<EMGChannelConfig> GetChannelConfigs()
    {
        return _channelConfigs;
    }

    public EMGChannelConfig GetChannelConfig(int index)
    {
        if (index >= 0 && index < _channelConfigs.Count)
            return _channelConfigs[index];
        return null;
    }

    public void SetChannelConfig(int index, EMGChannelConfig config)
    {
        if (index >= 0 && index < _channelConfigs.Count)
            _channelConfigs[index] = config;
    }

    public float MinDisplayRange
    {
        get { return _minDisplayRange; }
        set { _minDisplayRange = value; }
    }

    public float MaxDisplayRange
    {
        get { return _maxDisplayRange; }
        set { _maxDisplayRange = value; }
    }

    public int AveragingDuration
    {
        get { return _averagingDuration; }
        set { _averagingDuration = value; }
    }

    // Save calibration settings to PlayerPrefs for persistence
    public void SaveCalibration()
    {
        for (int i = 0; i < _channelConfigs.Count; i++)
        {
            PlayerPrefs.SetInt($"EMGChannel_{i}_SensorNumber", _channelConfigs[i].sensorNumber);
            PlayerPrefs.SetString($"EMGChannel_{i}_Name", _channelConfigs[i].channelName);
            PlayerPrefs.SetFloat($"EMGChannel_{i}_Threshold", _channelConfigs[i].threshold);
            PlayerPrefs.SetInt($"EMGChannel_{i}_Enabled", _channelConfigs[i].isEnabled ? 1 : 0);
            // Store color components
            PlayerPrefs.SetFloat($"EMGChannel_{i}_ColorR", _channelConfigs[i].signalColor.r);
            PlayerPrefs.SetFloat($"EMGChannel_{i}_ColorG", _channelConfigs[i].signalColor.g);
            PlayerPrefs.SetFloat($"EMGChannel_{i}_ColorB", _channelConfigs[i].signalColor.b);
        }

        PlayerPrefs.SetFloat("EMGMinDisplayRange", _minDisplayRange);
        PlayerPrefs.SetFloat("EMGMaxDisplayRange", _maxDisplayRange);
        // Averaging duration is fixed at 100ms, but save it anyway for compatibility
        PlayerPrefs.SetInt("EMGAveragingDuration", _averagingDuration);
        PlayerPrefs.Save();

        Debug.Log("EMG calibration saved successfully");
    }

    // Load calibration settings from PlayerPrefs
    public void LoadCalibration()
    {
        bool settingsFound = false;

        for (int i = 0; i < _channelConfigs.Count; i++)
        {
            if (PlayerPrefs.HasKey($"EMGChannel_{i}_SensorNumber"))
            {
                settingsFound = true;
                _channelConfigs[i].sensorNumber = PlayerPrefs.GetInt($"EMGChannel_{i}_SensorNumber");
                _channelConfigs[i].channelName = PlayerPrefs.GetString($"EMGChannel_{i}_Name");
                _channelConfigs[i].threshold = PlayerPrefs.GetFloat($"EMGChannel_{i}_Threshold");
                _channelConfigs[i].isEnabled = PlayerPrefs.GetInt($"EMGChannel_{i}_Enabled") == 1;

                // Load color components
                float r = PlayerPrefs.GetFloat($"EMGChannel_{i}_ColorR", _channelConfigs[i].signalColor.r);
                float g = PlayerPrefs.GetFloat($"EMGChannel_{i}_ColorG", _channelConfigs[i].signalColor.g);
                float b = PlayerPrefs.GetFloat($"EMGChannel_{i}_ColorB", _channelConfigs[i].signalColor.b);
                _channelConfigs[i].signalColor = new Color(r, g, b);
            }
        }

        if (PlayerPrefs.HasKey("EMGMinDisplayRange"))
            _minDisplayRange = PlayerPrefs.GetFloat("EMGMinDisplayRange");

        if (PlayerPrefs.HasKey("EMGMaxDisplayRange"))
            _maxDisplayRange = PlayerPrefs.GetFloat("EMGMaxDisplayRange");

        // Always set to 100ms, but read from settings for compatibility
        _averagingDuration = PlayerPrefs.GetInt("EMGAveragingDuration", 100);
        // Force it to 100ms regardless of saved value
        _averagingDuration = 100;

        if (settingsFound)
        {
            Debug.Log("EMG calibration loaded successfully from PlayerPrefs");
        }
        else
        {
            Debug.Log("No saved EMG calibration found in PlayerPrefs");
        }
    }

    // Called from editor script or menu
    [ContextMenu("Clear Saved Settings")]
    private void ClearSettingsFromMenu()
    {
        ClearSavedSettings();
    }
}
using UnityEngine;
using System;
using System.Collections.Generic;
using Waveplus.DaqSys;
using Waveplus.DaqSys.Definitions;
using Waveplus.DaqSysInterface;
using WaveplusLab.Shared.Definitions;

/// <summary>
/// This script interfaces with the EMG sensors thorugh the Waveplus DAQ hardware system. It processes raw signals, computes the powers, average them and detects whether they exceed the thresholds set.
/// It also provides a visual menu that shows the powers of each EMG channels and has sliders to calibrate the thresholds. It ultimately provides methods for other scripts such as Movement.cs to check
/// if the threshold is exceeded to trigger actions (player movement).
/// </summary>

public class EMGSignalTester : MonoBehaviour
{
    private DaqSystem _daqSystem;
    private bool _isCapturing = false;

    // Public property to check if capturing is active
    public bool IsCapturing => _isCapturing;

    // Signal data for each channel
    [Serializable]
    private class ChannelData
    {
        public int sensorNumber;
        public float currentSignal = 0;
        public float maxSignal = 0;
        public bool isAboveThreshold = false;
        public float thresholdTimer = 0;
        public Queue<float> signalBuffer = new Queue<float>();
        public float bufferedSignal = 0;
        public float lastBufferUpdateTime = 0;
        public Texture2D signalTexture;
        public bool isActivated = false;
    }

    private List<ChannelData> _channels = new List<ChannelData>();

    private bool _receivingData = false;
    private int _dataPointsReceived = 0;

    // Pre-create shared textures 
    private Texture2D _backgroundTex;
    private Texture2D _thresholdTex;
    private Texture2D _warningTex;

    // UI states
    private bool _showVisualization = true;

    void Start()
    {
        Debug.Log("Multi-Channel EMG Signal Tester starting...");

        // Create reusable textures
        _backgroundTex = CreateColorTexture(new Color(0.2f, 0.2f, 0.2f));
        _thresholdTex = CreateColorTexture(Color.yellow);
        _warningTex = CreateColorTexture(new Color(1.0f, 0.3f, 0.3f, 0.3f));

        // Initialize channels based on manager configuration
        InitializeChannels();

        // Automatically initialize and start capturing
        InitializeDaqSystem();

        // Start capture automatically
        if (_daqSystem != null && !_isCapturing)
        {
            StartCapturing();
        }
    }

    void InitializeChannels()
    {
        _channels.Clear();

        // Get configuration from the manager
        var channelManager = EMGChannelManager.Instance;
        if (channelManager == null)
        {
            Debug.LogError("EMGChannelManager not found!");
            return;
        }

        var channelConfigs = channelManager.GetChannelConfigs();
        foreach (var config in channelConfigs)
        {
            if (config.isEnabled)
            {
                ChannelData channel = new ChannelData
                {
                    sensorNumber = config.sensorNumber,
                    signalTexture = CreateColorTexture(config.signalColor)
                };

                _channels.Add(channel);
            }
        }
    }

    void Update()
    {
        if (!_isCapturing)
            return;

        var channelManager = EMGChannelManager.Instance;
        if (channelManager == null)
            return;

        float averagingDuration = channelManager.AveragingDuration;

        // Process each channel separately - THIS ALWAYS RUNS regardless of visualization
        for (int i = 0; i < _channels.Count; i++)
        {
            var channel = _channels[i];

            // Update the buffered signal display at a more stable rate
            if (Time.time - channel.lastBufferUpdateTime >= (averagingDuration / 1000f))
            {
                channel.lastBufferUpdateTime = Time.time;

                // Calculate average from buffer
                if (channel.signalBuffer.Count > 0)
                {
                    float sum = 0;
                    foreach (float value in channel.signalBuffer)
                    {
                        sum += value;
                    }
                    channel.bufferedSignal = sum / channel.signalBuffer.Count;
                }

                // Clear buffer for next averaging period
                channel.signalBuffer.Clear();

                // Get threshold for this channel
                float threshold = channelManager.GetChannelConfig(i).threshold;

                // Check threshold
                bool wasAboveThreshold = channel.isAboveThreshold;
                channel.isAboveThreshold = channel.bufferedSignal >= threshold;

                // If we just crossed the threshold, set the activation flag
                if (!wasAboveThreshold && channel.isAboveThreshold)
                {
                    channel.isActivated = true;
                    Debug.Log($"Channel {i} activated! Signal: {channel.bufferedSignal:F2}, Threshold: {threshold:F2}");
                }

                // Reset warning timer if above threshold
                if (channel.isAboveThreshold)
                    channel.thresholdTimer = 0.5f; // Show warning for 0.5 seconds
            }

            // Update threshold timer
            if (channel.thresholdTimer > 0)
                channel.thresholdTimer -= Time.deltaTime;
        }
    }

    void OnDestroy()
    {
        CleanUp();

        // Clean up textures
        Destroy(_backgroundTex);
        Destroy(_thresholdTex);
        Destroy(_warningTex);

        // Clean up channel textures
        foreach (var channel in _channels)
        {
            Destroy(channel.signalTexture);
        }
    }

    private Texture2D CreateColorTexture(Color color)
    {
        Texture2D tex = new Texture2D(1, 1);
        tex.SetPixel(0, 0, color);
        tex.Apply();
        return tex;
    }

    private void InitializeDaqSystem()
    {
        try
        {
            // Create the DaqSystem instance
            _daqSystem = new DaqSystem();

            // Add event handler for data available
            _daqSystem.DataAvailable += OnDataAvailable;

            Debug.Log($"DaqSystem initialized. State: {_daqSystem.State}");
            Debug.Log($"Installed sensors: {_daqSystem.InstalledSensors}");
        }
        catch (Exception ex)
        {
            Debug.LogError($"Failed to initialize DaqSystem: {ex.Message}");
        }
    }

    private void OnDataAvailable(object sender, DataAvailableEventArgs e)
    {
        try
        {
            // Check if we have valid data
            if (e.ScanNumber <= 0)
                return;

            _receivingData = true;
            _dataPointsReceived += e.ScanNumber;

            // Process each channel
            foreach (var channel in _channels)
            {
                // Calculate signal power for the sensor
                // Note: Adjust this index if your SDK uses 0-based indexing
                int sensorIndex = channel.sensorNumber - 1; // Convert from 1-based to 0-based

                float sum = 0;
                int count = 0;

                for (int i = 0; i < e.ScanNumber; i++)
                {
                    // Access EMG sample
                    float sample = e.Samples[sensorIndex, i];

                    // Use absolute value for power calculation
                    float absValue = Mathf.Abs(sample);
                    sum += absValue;
                    count++;

                    // Track max value
                    if (absValue > channel.maxSignal)
                        channel.maxSignal = absValue;
                }

                // Calculate average power for this data batch
                if (count > 0)
                {
                    channel.currentSignal = sum / count;

                    // Add to buffer for time-based averaging
                    channel.signalBuffer.Enqueue(channel.currentSignal);
                }
            }
        }
        catch (Exception ex)
        {
            // Only log occasionally to prevent spam
            if (Time.frameCount % 300 == 0)
                Debug.LogError($"Error processing data: {ex.Message}");
        }
    }

    public void StartCapturing()
    {
        try
        {
            if (_daqSystem == null)
            {
                Debug.LogError("DaqSystem is not initialized");
                return;
            }

            // Configure each sensor as EMG sensor
            foreach (var channel in _channels)
            {
                SensorConfiguration sensorConfig = new SensorConfiguration
                {
                    SensorType = SensorType.EMG_SENSOR,
                    AccelerometerFullScale = AccelerometerFullScale.g_2
                };

                _daqSystem.ConfigureSensor(sensorConfig, channel.sensorNumber);
                Debug.Log($"Configured sensor {channel.sensorNumber} as EMG sensor");

                // Reset channel statistics
                channel.currentSignal = 0;
                channel.bufferedSignal = 0;
                channel.signalBuffer.Clear();
                channel.isAboveThreshold = false;
                channel.thresholdTimer = 0;
                channel.isActivated = false;
            }

            // Reset overall statistics
            _dataPointsReceived = 0;
            _receivingData = false;

            // Start capturing
            _daqSystem.StartCapturing(DataAvailableEventPeriod.ms_100);
            _isCapturing = true;
            Debug.Log("Capturing started");
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error starting capture: {ex.Message}");
            Debug.LogError($"Stack trace: {ex.StackTrace}");
        }
    }

    public void StopCapturing()
    {
        try
        {
            if (_daqSystem != null && _isCapturing)
            {
                _daqSystem.StopCapturing();
                _isCapturing = false;
                Debug.Log("Capturing stopped");
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error stopping capture: {ex.Message}");
        }
    }

    private void CleanUp()
    {
        try
        {
            if (_isCapturing)
                StopCapturing();

            if (_daqSystem != null)
            {
                _daqSystem.DataAvailable -= OnDataAvailable;
                _daqSystem.Dispose();
                _daqSystem = null;
                Debug.Log("DaqSystem disposed");
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error cleaning up: {ex.Message}");
        }
    }

    // Helper to normalize a value within a range
    private float NormalizeValue(float value, float min, float max)
    {
        if (max <= min)
            return 0;

        return Mathf.Clamp01((value - min) / (max - min));
    }

    // Method to check if any channel is activated
    public bool IsChannelActivated(int channelIndex)
    {
        if (channelIndex >= 0 && channelIndex < _channels.Count)
        {
            bool isActivated = _channels[channelIndex].isActivated;

            // Reset activation flag after reading it
            if (isActivated)
                _channels[channelIndex].isActivated = false;

            return isActivated;
        }
        return false;
    }

    // Helper methods for external access
    public bool IsChannelDataAvailable(int channelIndex)
    {
        return channelIndex >= 0 && channelIndex < _channels.Count;
    }

    public float GetChannelCurrentSignal(int channelIndex)
    {
        if (channelIndex >= 0 && channelIndex < _channels.Count)
            return _channels[channelIndex].currentSignal;
        return 0;
    }

    public float GetChannelBufferedSignal(int channelIndex)
    {
        if (channelIndex >= 0 && channelIndex < _channels.Count)
            return _channels[channelIndex].bufferedSignal;
        return 0;
    }

    public float GetChannelMaxSignal(int channelIndex)
    {
        if (channelIndex >= 0 && channelIndex < _channels.Count)
            return _channels[channelIndex].maxSignal;
        return 0;
    }

    void OnGUI()
    {
        var channelManager = EMGChannelManager.Instance;
        if (channelManager == null)
            return;

        // Create a dedicated area for the toggle button at the top of the screen
        GUI.Box(new Rect(10, 10, 160, 30), "");
        _showVisualization = GUI.Toggle(new Rect(15, 15, 150, 20), _showVisualization, "Show EMG Signals");

        // Exit if visualization is turned off - but keep signal processing active
        if (!_showVisualization)
            return;

        // Main container
        GUILayout.BeginArea(new Rect(10, 50, 400, 380));

        // Title
        GUILayout.Label("EMG Signal Monitor", GUI.skin.box);

        // Show only the status and save button, without start/stop controls
        if (_daqSystem == null)
        {
            GUILayout.Label("Status: DaqSystem not initialized");

            // Attempt to initialize if not already
            if (GUILayout.Button("Initialize"))
            {
                InitializeDaqSystem();
                if (_daqSystem != null) StartCapturing();
            }
        }
        else
        {
            // Just show status and save button
            GUILayout.Label(_isCapturing ? "Status: Capturing" : "Status: Not capturing");

            // Display all channels (no tabs)
            var channelConfigs = channelManager.GetChannelConfigs();

            for (int channelIndex = 0; channelIndex < _channels.Count; channelIndex++)
            {
                if (channelIndex >= channelConfigs.Count)
                    continue;

                var config = channelConfigs[channelIndex];
                var channel = _channels[channelIndex];

                GUILayout.Space(5);
                GUILayout.Label($"Channel {channelIndex + 1}: Sensor {channel.sensorNumber}", GUI.skin.box);

                // Signal bar
                Rect barRect = GUILayoutUtility.GetRect(380, 20);

                // Draw background
                GUI.DrawTexture(barRect, _backgroundTex);

                // Calculate positions with fixed range 0-100
                float normalizedSignal = Mathf.Clamp01(channel.bufferedSignal / 100f);
                float normalizedThreshold = Mathf.Clamp01(config.threshold / 100f);

                // Draw signal bar
                Rect signalRect = new Rect(barRect.x, barRect.y, barRect.width * normalizedSignal, barRect.height);

                // Change color based on threshold
                if (channel.bufferedSignal >= config.threshold)
                    GUI.DrawTexture(signalRect, CreateColorTexture(Color.red));
                else
                    GUI.DrawTexture(signalRect, channel.signalTexture);

                // Draw threshold line
                Rect thresholdRect = new Rect(barRect.x + barRect.width * normalizedThreshold - 1, barRect.y, 2, barRect.height);
                GUI.DrawTexture(thresholdRect, _thresholdTex);

                // Show indicator if threshold crossed
                if (channel.thresholdTimer > 0)
                {
                    GUI.DrawTexture(barRect, _warningTex);
                }

                // Threshold slider
                GUILayout.BeginHorizontal();
                GUILayout.Label("Threshold:", GUILayout.Width(70));
                float newThreshold = GUILayout.HorizontalSlider(config.threshold, 0f, 100f, GUILayout.Width(250));
                GUILayout.Label(newThreshold.ToString("F1"), GUILayout.Width(40));
                GUILayout.EndHorizontal();

                // Update threshold if changed
                if (newThreshold != config.threshold)
                {
                    config.threshold = newThreshold;
                    channelManager.SetChannelConfig(channelIndex, config);
                }
            }
        }

        GUILayout.EndArea();
    }
}
using UnityEngine;
using UnityEngine.Audio;
using TMPro;
using System.Collections.Generic;

public class GameSettings : MonoBehaviour
{
    [Header("Audio")]
    [SerializeField] private AudioMixer audioMixer;
    [SerializeField] private string masterVolumeParameter = "MasterVolume";

    [Header("Resolution")]
    [SerializeField] private TMP_Dropdown resolutionDropdown;
    private Resolution[] availableResolutions;

    private void Awake()
    {
        ResolutionSetup();

        Screen.fullScreenMode = FullScreenMode.ExclusiveFullScreen;
        Screen.fullScreen = true;
    }

    /* =========================
     * AUDIO
     * ========================= */

    // Expected slider range: 0.0001 - 1
    public void SetMasterVolume(float value)
    {
        float volume = Mathf.Log10(Mathf.Clamp(value, 0.0001f, 1f)) * 20f;
        audioMixer.SetFloat(masterVolumeParameter, volume);
    }

    /* =========================
     * WINDOW MODE
     * ========================= */

    public void SetWindowMode(int index)
    {
        switch (index)
        {
            case 0:
                SetFullscreenExclusive();
                break;

            case 1:
                SetBorderlessWindowed();
                break;

            case 2:
                SetWindowed();
                break;
        }
    }

    public void SetFullscreenExclusive()
    {
        Screen.fullScreenMode = FullScreenMode.ExclusiveFullScreen;
        Screen.fullScreen = true;
    }

    public void SetBorderlessWindowed()
    {
        Screen.fullScreenMode = FullScreenMode.FullScreenWindow;
        Screen.fullScreen = true;
    }

    public void SetWindowed()
    {
        Screen.fullScreenMode = FullScreenMode.Windowed;
        Screen.fullScreen = false;
    }

    /* =========================
     * RESOLUTION
     * ========================= */

    public void ResolutionSetup()
    {
        // Get all supported screen resolutions
        availableResolutions = Screen.resolutions;

        // Clear any default options
        resolutionDropdown.ClearOptions();

        // Create a list of strings like "1920 x 1080"
        List<string> options = new List<string>();
        int currentResolutionIndex = 0;

        for (int i = 0; i < availableResolutions.Length; i++)
        {
            string option = $"{availableResolutions[i].width} x {availableResolutions[i].height}";
            options.Add(option);

            if (availableResolutions[i].width == Screen.currentResolution.width &&
                availableResolutions[i].height == Screen.currentResolution.height)
            {
                currentResolutionIndex = i;
            }
        }

        // Add options to dropdown
        resolutionDropdown.AddOptions(options);

        // Set current value
        resolutionDropdown.value = currentResolutionIndex;
        resolutionDropdown.RefreshShownValue();

        // Add listener for value change
        resolutionDropdown.onValueChanged.AddListener(SetResolution);
    }

    public void SetResolution(int resolutionIndex)
    {
        if (resolutionIndex < 0 || resolutionIndex >= availableResolutions.Length)
            return;

        Resolution res = availableResolutions[resolutionIndex];
        Screen.SetResolution(res.width, res.height, Screen.fullScreenMode);
    }
}

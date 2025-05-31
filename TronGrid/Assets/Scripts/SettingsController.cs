using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;
using TMPro;
using System.IO;

public class SettingsManager : MonoBehaviour
{
    // Singleton
    public static SettingsManager Instance;

    [System.Serializable]
    public class GameSettingsData
    {
        public float musicVolume = 1f;
        public float sfxVolume = 1f;
        public bool isFullscreen = true;
        public int resolutionIndex = 0;
        public int minutosDePartida = 5;
    }

    [Header("Audio")]
    public Slider musicSlider;
    public Slider sfxSlider;
    public AudioMixer audioMixer;

    [Header("Video")]
    public TMP_Dropdown resolutionDropdown;
    public Toggle fullscreenToggle;

    [Header("Gameplay")]
    public TMP_InputField minutosPartidaInput;

    public static float MusicVolume = 1f;
    public static float SFXVolume = 1f;
    public static int MinutosDePartida = 5;
    public static bool IsFullscreen = true;
    public static Resolution CurrentResolution;
    public static int ResolutionIndex = 0;

    private static Resolution[] resolutions;
    private static string savePath => Application.persistentDataPath + "/settings.json";

    void Awake()
    {
        if (Instance != null && Instance != this)
            Destroy(Instance.gameObject);

        Instance = this;
        DontDestroyOnLoad(this.gameObject);
    }

    void Start()
    {
        AdjustAspectRatio();
        InitializeResolutionDropdown();
        LoadOrCreateSettings();
        ApplySettingsToUI();
        ApplySettingsToSystem();
    }

    private void AdjustAspectRatio()
    {
        float targetAspect = 16f / 9f; // Relación de aspecto 16:9 deseada
        float windowAspect = (float)Screen.width / (float)Screen.height; // Relación de aspecto actual

        // Ajustar la cámara para mantener la relación de aspecto 16:9
        Camera camera = Camera.main;
        camera.aspect = targetAspect;

        // Calcular el factor de escala
        float scaleHeight = windowAspect / targetAspect;
        Rect rect = camera.rect;

        if (scaleHeight < 1.0f)
        {
            // Si la pantalla es más ancha que la relación de aspecto objetivo, se agregan barras negras verticales
            rect.width = 1.0f;
            rect.height = scaleHeight;
            rect.x = 0;
            rect.y = (1.0f - scaleHeight) / 2.0f; // Centrar las barras negras verticales
        }
        else
        {
            // Si la pantalla es más alta que la relación de aspecto objetivo, se agregan barras negras horizontales
            float scaleWidth = 1.0f / scaleHeight;
            rect.width = scaleWidth;
            rect.height = 1.0f;
            rect.x = (1.0f - scaleWidth) / 2.0f; // Centrar las barras negras horizontales
            rect.y = 0;
        }

        camera.rect = rect;
    }


    private void InitializeResolutionDropdown()
    {
        resolutions = Screen.resolutions;
        if (resolutionDropdown != null)
        {
            resolutionDropdown.ClearOptions();
            var options = new System.Collections.Generic.List<string>();

            int[] targetWidths = { 1920, 1280, 854, 3840, 2560, 1600, 640 };
            int[] targetHeights = { 1080, 720, 480, 2160, 1440, 900, 360 };

            foreach (var resolution in resolutions)
            {
                for (int i = 0; i < targetWidths.Length; i++)
                {
                    if (resolution.width == targetWidths[i] && resolution.height == targetHeights[i])
                    {
                        options.Add($"{resolution.width} x {resolution.height}");
                        break;
                    }
                }
            }

            resolutionDropdown.AddOptions(options);
        }
    }

    private void ApplySettingsToUI()
    {
        if (musicSlider != null) musicSlider.value = MusicVolume;
        if (sfxSlider != null) sfxSlider.value = SFXVolume;
        if (fullscreenToggle != null) fullscreenToggle.isOn = IsFullscreen;
        if (minutosPartidaInput != null) minutosPartidaInput.text = MinutosDePartida.ToString();
        if (resolutionDropdown != null)
        {
            resolutionDropdown.value = ResolutionIndex;
            resolutionDropdown.RefreshShownValue();
        }
    }

    private void ApplySettingsToSystem()
    {
        audioMixer.SetFloat("MusicVolume", Mathf.Log10(Mathf.Clamp(MusicVolume, 0.0001f, 1f)) * 20);
        audioMixer.SetFloat("SFXVolume", Mathf.Log10(Mathf.Clamp(SFXVolume, 0.0001f, 1f)) * 20);



        if (resolutions.Length > ResolutionIndex)
        {
            CurrentResolution = resolutions[ResolutionIndex];
            Screen.SetResolution(CurrentResolution.width, CurrentResolution.height, IsFullscreen);
        }

        Screen.fullScreen = IsFullscreen;
    }

    public void SetMusicVolume()
    {
        MusicVolume = musicSlider.value;
        audioMixer.SetFloat("MusicVolume", Mathf.Log10(Mathf.Clamp(MusicVolume, 0.0001f, 1f)) * 20);
        SaveSettings();
    }

    public void SetSFXVolume()
    {
        SFXVolume = sfxSlider.value;
        audioMixer.SetFloat("SFXVolume", Mathf.Log10(Mathf.Clamp(SFXVolume, 0.0001f, 1f)) * 20);
        SaveSettings();
    }

    public void SetResolution()
    {
        ResolutionIndex = resolutionDropdown.value;
        CurrentResolution = resolutions[ResolutionIndex];
        Screen.SetResolution(CurrentResolution.width, CurrentResolution.height, IsFullscreen);
        SaveSettings();
    }

    public void SetFullscreen()
    {
        IsFullscreen = fullscreenToggle.isOn;
        Screen.fullScreen = IsFullscreen;
        SaveSettings();
    }

    public void setMinutosPartida()
    {
        MinutosDePartida = int.Parse(minutosPartidaInput.text);
        SaveSettings();
    }

    public bool LoadSettings()
    {
        if (File.Exists(savePath))
        {
            try
            {
                string json = File.ReadAllText(savePath);
                GameSettingsData data = JsonUtility.FromJson<GameSettingsData>(json);
                MusicVolume = data.musicVolume;
                SFXVolume = data.sfxVolume;
                IsFullscreen = data.isFullscreen;
                ResolutionIndex = data.resolutionIndex;
                MinutosDePartida = data.minutosDePartida;
                return true;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Error al cargar configuración: {e.Message}");
            }
        }
        return false;
    }

    private void GenerateDefaultSettings()
    {
        MusicVolume = 1f;
        SFXVolume = 1f;
        IsFullscreen = true;
        ResolutionIndex = 0;
        MinutosDePartida = 5;
        SaveSettings();
    }

    public void SaveSettings()
    {
        GameSettingsData data = new GameSettingsData
        {
            musicVolume = MusicVolume,
            sfxVolume = SFXVolume,
            isFullscreen = IsFullscreen,
            resolutionIndex = ResolutionIndex,
            minutosDePartida = MinutosDePartida
        };

        string json = JsonUtility.ToJson(data, true);
        File.WriteAllText(savePath, json);
        Debug.Log($"Configuración guardada en: {savePath}");
    }

    public void LoadOrCreateSettings()
    {
        if (!File.Exists(savePath))
        {
            GenerateDefaultSettings();
            Debug.Log("Archivo de configuración creado con valores por defecto.");
        }
        else if (!LoadSettings())
        {
            GenerateDefaultSettings();
        }
    }
}

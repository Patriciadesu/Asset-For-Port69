using UnityEngine;
using System;

[System.Serializable]
public class DayNightSettings
{
    [Header("Time Settings")]
    [Range(0.1f, 10f)]
    public float timeSpeed = 1f;

    [Range(0f, 24f)]
    public float startTime = 6f; // Start at 6 AM

    [Header("Sun Settings")]
    public Light sunLight;
    public Gradient sunColor = new Gradient();
    public AnimationCurve sunIntensity = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("Moon Settings")]
    public Light moonLight;
    public Color moonColor = Color.white;
    [Range(0f, 2f)]
    public float moonIntensity = 0.5f;

    [Header("Sky Settings")]
    public Material skyboxMaterial;
    public Gradient skyTint = new Gradient();
    public AnimationCurve skyExposure = AnimationCurve.EaseInOut(0, 0.5f, 1, 1.3f);

    [Header("Fog Settings")]
    public bool useFog = true;
    public Gradient fogColor = new Gradient();
    public AnimationCurve fogDensity = AnimationCurve.EaseInOut(0, 0.01f, 1, 0.05f);
}

public class DayNightCycle : MonoBehaviour
{
    [Header("Day/Night Cycle Controller")]
    public DayNightSettings settings = new DayNightSettings();

    [Header("Debug Info")]
    [SerializeField, Range(0f, 24f)]
    private float currentTime;

    [SerializeField]
    private string timeDisplay;

    [Header("Events")]
    public UnityEngine.Events.UnityEvent OnSunrise;
    public UnityEngine.Events.UnityEvent OnNoon;
    public UnityEngine.Events.UnityEvent OnSunset;
    public UnityEngine.Events.UnityEvent OnMidnight;
    public UnityEngine.Events.UnityEvent OnNewDay;

    // Private variables
    private bool hasSunriseTriggered, hasNoonTriggered, hasSunsetTriggered, hasMidnightTriggered;
    [SerializeField]
    private int dayCount = 1;

    // Skybox runtime handling
    private Material originalSkyboxMaterial;
    private Material runtimeSkyboxMaterial;

    // Public properties for easy access
    public float CurrentTime => currentTime;
    public bool IsDay => currentTime >= 6f && currentTime < 18f;
    public bool IsNight => !IsDay;
    public string TimeString => FormatTime(currentTime);
    public int DayCount => dayCount;

    void Start()
    {
        InitializeDayNightCycle();
    }

    void Update()
    {
        UpdateTime();
        UpdateLighting();
        UpdateSkybox();
        UpdateFog();
        CheckTimeEvents();
        UpdateDebugDisplay();
    }

    void InitializeDayNightCycle()
    {
        currentTime = settings.startTime;

        // Initialize default gradients if not set
        if (settings.sunColor.colorKeys.Length <= 1)
        {
            SetupDefaultSunGradient();
        }

        if (settings.skyTint.colorKeys.Length <= 1)
        {
            SetupDefaultSkyGradient();
        }

        if (settings.fogColor.colorKeys.Length <= 1)
        {
            SetupDefaultFogGradient();
        }

        // Enable fog if using it
        if (settings.useFog)
        {
            RenderSettings.fog = true;
        }

        // Prepare runtime skybox instance to avoid editing the asset during Play Mode
        originalSkyboxMaterial = RenderSettings.skybox;

        Material sourceSkybox = settings.skyboxMaterial != null ? settings.skyboxMaterial : originalSkyboxMaterial;
        if (sourceSkybox != null)
        {
            runtimeSkyboxMaterial = new Material(sourceSkybox)
            {
                name = sourceSkybox.name + " (Runtime)"
            };
            RenderSettings.skybox = runtimeSkyboxMaterial;
        }
    }

    void UpdateTime()
    {
        currentTime += Time.deltaTime * settings.timeSpeed;

        if (currentTime >= 24f)
        {
            currentTime = 0f;
            dayCount++;
            ResetDailyEvents();
            OnNewDay?.Invoke();
        }
    }

    void UpdateLighting()
    {
        float normalizedTime = currentTime / 24f;

        // Update Sun
        if (settings.sunLight != null)
        {
            // Sun rotation (rises in east, sets in west)
            float sunAngle = (currentTime - 6f) * 15f; // 15 degrees per hour, starting at sunrise
            settings.sunLight.transform.rotation = Quaternion.Euler(sunAngle - 90f, 30f, 0f);

            // Sun color and intensity
            settings.sunLight.color = settings.sunColor.Evaluate(normalizedTime);
            settings.sunLight.intensity = settings.sunIntensity.Evaluate(normalizedTime);

            // Disable sun at night
            settings.sunLight.enabled = IsDay;
        }

        // Update Moon
        if (settings.moonLight != null)
        {
            // Moon rotation (opposite to sun)
            float moonAngle = (currentTime - 18f) * 15f;
            settings.moonLight.transform.rotation = Quaternion.Euler(moonAngle - 90f, -30f, 0f);

            settings.moonLight.color = settings.moonColor;
            settings.moonLight.intensity = IsNight ? settings.moonIntensity : 0f;
            settings.moonLight.enabled = IsNight;
        }
    }

    void UpdateSkybox()
    {
        Material targetSkybox = runtimeSkyboxMaterial != null ? runtimeSkyboxMaterial : settings.skyboxMaterial;
        if (targetSkybox != null)
        {
            float normalizedTime = currentTime / 24f;

            // Update sky tint
            if (targetSkybox.HasProperty("_Tint"))
            {
                targetSkybox.SetColor("_Tint", settings.skyTint.Evaluate(normalizedTime));
            }

            // Update sky exposure
            if (targetSkybox.HasProperty("_Exposure"))
            {
                targetSkybox.SetFloat("_Exposure", settings.skyExposure.Evaluate(normalizedTime));
            }

            if (RenderSettings.skybox != targetSkybox)
            {
                RenderSettings.skybox = targetSkybox;
            }
        }
    }

    void UpdateFog()
    {
        if (settings.useFog)
        {
            float normalizedTime = currentTime / 24f;
            RenderSettings.fogColor = settings.fogColor.Evaluate(normalizedTime);
            RenderSettings.fogDensity = settings.fogDensity.Evaluate(normalizedTime);
        }
    }

    void CheckTimeEvents()
    {
        // Sunrise (6 AM)
        if (currentTime >= 6f && currentTime < 6.1f && !hasSunriseTriggered)
        {
            OnSunrise?.Invoke();
            hasSunriseTriggered = true;
        }

        // Noon (12 PM)
        if (currentTime >= 12f && currentTime < 12.1f && !hasNoonTriggered)
        {
            OnNoon?.Invoke();
            hasNoonTriggered = true;
        }

        // Sunset (6 PM)
        if (currentTime >= 18f && currentTime < 18.1f && !hasSunsetTriggered)
        {
            OnSunset?.Invoke();
            hasSunsetTriggered = true;
        }

        // Midnight (12 AM)
        if (currentTime >= 0f && currentTime < 0.1f && !hasMidnightTriggered)
        {
            OnMidnight?.Invoke();
            hasMidnightTriggered = true;
        }
    }

    void ResetDailyEvents()
    {
        hasSunriseTriggered = hasNoonTriggered = hasSunsetTriggered = hasMidnightTriggered = false;
    }

    void OnDisable()
    {
        if (runtimeSkyboxMaterial != null)
        {
            if (Application.isPlaying)
            {
                Destroy(runtimeSkyboxMaterial);
            }
            else
            {
                DestroyImmediate(runtimeSkyboxMaterial);
            }
            runtimeSkyboxMaterial = null;
        }

        if (originalSkyboxMaterial != null)
        {
            RenderSettings.skybox = originalSkyboxMaterial;
        }
    }

    void UpdateDebugDisplay()
    {
        timeDisplay = FormatTime(currentTime);
    }

    string FormatTime(float time)
    {
        int hours = Mathf.FloorToInt(time);
        int minutes = Mathf.FloorToInt((time - hours) * 60);
        string period = hours >= 12 ? "PM" : "AM";
        int displayHour = hours == 0 ? 12 : (hours > 12 ? hours - 12 : hours);
        return $"{displayHour:00}:{minutes:00} {period}";
    }

    void SetupDefaultSunGradient()
    {
        GradientColorKey[] colorKeys = new GradientColorKey[5];
        GradientAlphaKey[] alphaKeys = new GradientAlphaKey[5];

        // Night -> Dawn -> Day -> Dusk -> Night (loop)
        Color nightColor = new Color(0.2f, 0.2f, 0.4f);
        colorKeys[0] = new GradientColorKey(nightColor, 0f);     // Night
        colorKeys[1] = new GradientColorKey(new Color(1f, 0.6f, 0.3f), 0.25f);    // Dawn
        colorKeys[2] = new GradientColorKey(new Color(1f, 0.95f, 0.8f), 0.5f);    // Day
        colorKeys[3] = new GradientColorKey(new Color(1f, 0.4f, 0.2f), 0.75f);    // Dusk
        colorKeys[4] = new GradientColorKey(nightColor, 1f);     // Night (same as 0f)

        for (int i = 0; i < 4; i++)
            alphaKeys[i] = new GradientAlphaKey(1f, i * 0.25f);
        alphaKeys[4] = new GradientAlphaKey(1f, 1f); // Ensure last alpha matches first

        settings.sunColor.SetKeys(colorKeys, alphaKeys);

        settings.sunIntensity = new AnimationCurve(
            new Keyframe(0f, 0f, 0f, 0f),    // Midnight, low, flat
            new Keyframe(0.5f, 1f, 0f, 0f),    // Noon, high, flat
            new Keyframe(1f, 0f, 0f, 0f)     // Midnight, low, flat (same as 0f)
        );
    }

    void SetupDefaultSkyGradient()
    {
        GradientColorKey[] colorKeys = new GradientColorKey[4];
        GradientAlphaKey[] alphaKeys = new GradientAlphaKey[4];

        Color nightColor = new Color(0.3f, 0.3f, 0.5f);
        colorKeys[0] = new GradientColorKey(nightColor, 0f);     // Night
        colorKeys[1] = new GradientColorKey(new Color(0.8f, 0.5f, 0.3f), 0.3f);   // Dawn
        colorKeys[2] = new GradientColorKey(new Color(0.5f, 0.7f, 1f), 0.7f);     // Day
        colorKeys[3] = new GradientColorKey(nightColor, 1f);     // Night (same as 0f)

        for (int i = 0; i < 3; i++)
            alphaKeys[i] = new GradientAlphaKey(1f, i * 0.33f);
        alphaKeys[3] = new GradientAlphaKey(1f, 1f); // Ensure last alpha matches first

        settings.skyTint.SetKeys(colorKeys, alphaKeys);

        // Set a looping skyExposure curve: low at midnight, high at noon, low at midnight
        settings.skyExposure = new AnimationCurve(
            new Keyframe(0f, 0.7f, 0f, 0f),    // Midnight, low, flat
            new Keyframe(0.5f, 1f, 0f, 0f),    // Noon, high, flat
            new Keyframe(1f, 0.7f, 0f, 0f)     // Midnight, low, flat (same as 0f)
        );
    }

    void SetupDefaultFogGradient()
    {
        GradientColorKey[] colorKeys = new GradientColorKey[4];
        GradientAlphaKey[] alphaKeys = new GradientAlphaKey[4];

        Color nightColor = new Color(0.1f, 0.1f, 0.2f);
        colorKeys[0] = new GradientColorKey(nightColor, 0f);     // Night
        colorKeys[1] = new GradientColorKey(new Color(0.6f, 0.4f, 0.3f), 0.3f);   // Dawn
        colorKeys[2] = new GradientColorKey(new Color(0.7f, 0.8f, 1f), 0.7f);     // Day
        colorKeys[3] = new GradientColorKey(nightColor, 1f);     // Night (same as 0f)

        for (int i = 0; i < 3; i++)
            alphaKeys[i] = new GradientAlphaKey(1f, i * 0.33f);
        alphaKeys[3] = new GradientAlphaKey(1f, 1f); // Ensure last alpha matches first

        settings.fogColor.SetKeys(colorKeys, alphaKeys);

        settings.fogDensity = new AnimationCurve(
            new Keyframe(0f, 0.01f, 0f, 0f),    // Midnight, low, flat
            new Keyframe(0.5f, 0.05f, 0f, 0f),    // Noon, high, flat
            new Keyframe(1f, 0.01f, 0f, 0f)     // Midnight, low, flat (same as 0f)
        );
    }

    // Public methods for external control
    public void SetTime(float time)
    {
        currentTime = Mathf.Clamp(time, 0f, 24f);
    }

    public void SetTimeSpeed(float speed)
    {
        settings.timeSpeed = Mathf.Max(0.1f, speed);
    }

    public void PauseTime()
    {
        settings.timeSpeed = 0f;
    }

    public void ResumeTime()
    {
        settings.timeSpeed = 1f;
    }

    public void SetDay(int day)
    {
        dayCount = Mathf.Max(1, day);
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using DG.Tweening;                 // DOTween
using NaughtyAttributes;           // Inspector helpers
using UnityEngine;
using UnityEngine.Rendering;

[AddComponentMenu("Rendering/Volume Looper (Auto-Detect)")]
public class VolumeLooper : MonoBehaviour
{
    [Header("Target")]
    [Tooltip("If set, will use this Volume's profile. Otherwise uses 'profile' below.")]
    public Volume volume;

    [Tooltip("If 'volume' is not set, assign a VolumeProfile asset here.")]
    public VolumeProfile profile;

    [Header("Playback")]
    public bool playOnEnable = false;

    [Min(0f)]
    [Tooltip("Per-tween speed multiplier. 1 = normal.")]
    public float globalTimeScale = 1f;

    [Space]
    [ReorderableList]
    public List<LoopTrack> tracks = new()
    {
        new LoopTrack
        {
            // Example: Vignette.intensity 0 <-> 0.5
            hintComponentName = "Vignette",
            hintParamName = "intensity",
            floatFrom = 0f, floatTo = 0.5f,
            duration = 2f, ease = Ease.InOutSine, loopType = LoopType.Yoyo, loops = -1,
            autoEnableOverride = true
        },
        new LoopTrack
        {
            // Example: ColorAdjustments.colorFilter A <-> B
            hintComponentName = "ColorAdjustments",
            hintParamName = "colorFilter",
            colorA = new Color(1f, 0.8f, 0.8f, 1f),
            colorB = new Color(0.8f, 0.8f, 1f, 1f),
            duration = 4f, ease = Ease.InOutSine, loopType = LoopType.Yoyo, loops = -1,
            autoEnableOverride = true
        }
    };

    private readonly List<Tween> _activeTweens = new();

    private VolumeProfile EffectiveProfile
        => volume != null ? (volume.sharedProfile != null ? volume.sharedProfile : volume.profile) : profile;

    private bool HasProfile => EffectiveProfile != null;

    [ShowIf(nameof(ShowNoProfileWarning))]
    [InfoBox("Assign a Volume or Profile to enable dropdowns.", EInfoBoxType.Warning)]
    public string _profileWarningSpacer;
    private bool ShowNoProfileWarning => !HasProfile;

    void OnEnable()
    {
        PushProfileToTracks();
        if (playOnEnable) Play();
    }

    void OnDisable()
    {
        Stop();
    }

    void OnValidate()
    {
        PushProfileToTracks();
    }

    void OnTriggerEnter(Collider other)
    {
        if(!playOnEnable)Play();
    }
    void OnTriggerExit(Collider other)
    {
        if(!playOnEnable)Stop();
    }

    private void PushProfileToTracks()
    {
        var prof = EffectiveProfile;
        foreach (var t in tracks) t.EditorReceiveProfile(prof);
    }

    [Button("Play / Refresh")]
    public void Play()
    {
        Stop();

        var prof = EffectiveProfile;
        if (prof == null)
        {
            Debug.LogWarning("[VolumeLooper] No Volume/Profile assigned.");
            return;
        }

        foreach (var t in tracks)
        {
            if (!t.enabled) continue;

            if (!t.TryBind(prof))
            {
                Debug.LogWarning($"[VolumeLooper] Track could not bind: {t.GetLabel()}");
                continue;
            }

            if (t.autoEnableOverride) t.SetOverrideState(true);

            t.SetInitialValue();
            var tw = t.BuildTween();
            if (tw == null) continue;

            if (!Mathf.Approximately(globalTimeScale, 1f))
                tw.timeScale = Mathf.Max(0f, globalTimeScale);

            _activeTweens.Add(tw);
        }
    }

    [Button("Stop")]
    public void Stop()
    {
        foreach (var tw in _activeTweens)
        {
            if (tw != null && tw.IsActive()) tw.Kill();
        }
        _activeTweens.Clear();
    }

    [Button("Refresh Dropdowns (Editor)")]
    private void RefreshDropdowns()
    {
        PushProfileToTracks();
        #if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(this);
        #endif
    }

    // ================== Nested Types ==================

    [Serializable]
    public class LoopTrack
    {
        // We auto-detect whether the selected parameter is float or color.
        private enum DetectedType { None, Float, Color }

        [Header("Enable")]
        public bool enabled = true;

        [Header("Select Override (Component → Parameter)")]
        [OnValueChanged(nameof(ClearSelectedParam))]
        [Dropdown(nameof(GetComponentNames))]
        public string componentName;

        // Blended param dropdown (float + color)
        [Dropdown(nameof(GetParamNames))]
        [OnValueChanged(nameof(OnParamPicked))]
        public string paramName;

        [Tooltip("Enable the parameter's overrideState and the component (active).")]
        public bool autoEnableOverride = true;

        [Header("Tween")]
        [ShowIf(nameof(ShowFloatSection))]
        [Label("From")]
        [AllowNesting]
        public float floatFrom = 0f;

        [ShowIf(nameof(ShowFloatSection))]
        [Label("To")]
        [AllowNesting]
        public float floatTo = 1f;

        // Color UI (visible only when a color parameter is selected)
        [ShowIf(nameof(ShowColorSection))]
        [ Label("Color A")]
        [AllowNesting]
        public Color colorA = Color.white;

        [ShowIf(nameof(ShowColorSection))]
        [Label("Color B")]
        [AllowNesting]
        public Color colorB = Color.black;
        public Ease ease = Ease.InOutSine;
        public LoopType loopType = LoopType.Yoyo;
        [Tooltip("-1 = infinite")]
        public int loops = -1;
        [Min(0f)] public float duration = 1f;
        [Min(0f)] public float delay = 0f;

        // Float UI (visible only when a float parameter is selected)
        

        // ------- Non-serialized caches -------
        [NonSerialized] private VolumeProfile _profile;
        [NonSerialized] private VolumeComponent _component;
        [NonSerialized] private FieldInfo _field;       // the selected field (float or color)
        [NonSerialized] private object _paramObj;       // VolumeParameter<float> or VolumeParameter<Color>
        [NonSerialized] private DetectedType _detected = DetectedType.None;

        // Hints for first-time default
        [SerializeField, HideInInspector] public string hintComponentName = null;
        [SerializeField, HideInInspector] public string hintParamName = null;

        // ---------- Inspector Helpers ----------

        private bool ShowFloatSection => _detected == DetectedType.Float && !string.IsNullOrEmpty(paramName);
        private bool ShowColorSection => _detected == DetectedType.Color && !string.IsNullOrEmpty(paramName);

        private void ClearSelectedParam()
        {
            paramName = null;
            _detected = DetectedType.None;
            _field = null;
            _paramObj = null;
        }

        private void OnParamPicked()
        {
            // Re-evaluate detection when user changes param
            DetectSelectedParamType();
        }

        public string[] GetComponentNames()
        {
            if (_profile == null) return new[] { "(Assign Volume/Profile first)" };

            var names = _profile.components
                .Where(c => c != null)
                .Select(c => c.name)
                .Distinct()
                .OrderBy(n => n)
                .ToArray();

            return names.Length > 0 ? names : new[] { "(No Components In Profile)" };
        }

        // Blend float + color parameter names into one dropdown
        public string[] GetParamNames()
        {
            if (_profile == null || string.IsNullOrEmpty(componentName))
                return new[] { "(Select Component first)" };

            var comp = ResolveComponent(_profile, componentName, quiet: true);
            if (comp == null) return new[] { "(Component Missing In Profile)" };

            var floats = GetVolumeParameterFields(comp, typeof(float)).Select(f => f.Name);
            var colors = GetVolumeParameterFields(comp, typeof(Color)).Select(f => f.Name);

            var all = floats.Concat(colors).Distinct().OrderBy(n => n).ToArray();
            return all.Length > 0 ? all : new[] { "(No Float/Color Parameters Found)" };
        }

        public string GetLabel()
        {
            if (string.IsNullOrEmpty(componentName))
                return "(Unassigned Track)";
            return string.IsNullOrEmpty(paramName)
                ? $"{componentName}.(?)"
                : $"{componentName}.{paramName}";
        }

        // ---------- Editor & Binding ----------

        public void EditorReceiveProfile(VolumeProfile profile)
        {
            _profile = profile;

            if (!Application.isPlaying && _profile != null)
            {
                if (string.IsNullOrEmpty(componentName) && !string.IsNullOrEmpty(hintComponentName))
                {
                    if (_profile.components.Any(c => c != null && c.name == hintComponentName))
                        componentName = hintComponentName;
                }

                if (string.IsNullOrEmpty(paramName) && !string.IsNullOrEmpty(hintParamName))
                {
                    var comp = ResolveComponent(_profile, componentName, true);
                    if (comp != null)
                    {
                        var has = GetVolumeParameterFields(comp, typeof(float)).Any(f => f.Name == hintParamName) ||
                                  GetVolumeParameterFields(comp, typeof(Color)).Any(f => f.Name == hintParamName);
                        if (has) paramName = hintParamName;
                    }
                }

                DetectSelectedParamType();
            }
        }

        public bool TryBind(VolumeProfile profile)
        {
            _profile = profile;
            if (_profile == null) return false;

            _component = ResolveComponent(_profile, componentName);
            if (_component == null) return false;

            if (string.IsNullOrEmpty(paramName)) return false;

            // Try find float param first, then color
            _field = GetVolumeParameterFields(_component, typeof(float)).FirstOrDefault(f => f.Name == paramName);
            _detected = DetectedType.None;
            if (_field != null)
            {
                _paramObj = _field.GetValue(_component);
                _detected = DetectedType.Float;
            }
            else
            {
                _field = GetVolumeParameterFields(_component, typeof(Color)).FirstOrDefault(f => f.Name == paramName);
                if (_field != null)
                {
                    _paramObj = _field.GetValue(_component);
                    _detected = DetectedType.Color;
                }
            }

            return _paramObj != null && _detected != DetectedType.None;
        }

        public void SetOverrideState(bool state)
        {
            if (_component == null || _paramObj == null) return;

            try
            {
                var ovStateProp = _paramObj.GetType().GetProperty("overrideState");
                ovStateProp?.SetValue(_paramObj, state);

                // Support both property or field named "active"
                var activeProp = _component.GetType().GetProperty("active");
                if (activeProp != null && activeProp.CanWrite) activeProp.SetValue(_component, true);
                var activeField = _component.GetType().GetField("active");
                if (activeField != null) activeField.SetValue(_component, true);
            }
            catch { /* ignore */ }
        }

        public void SetInitialValue()
        {
            if (_paramObj == null) return;

            var valueProp = _paramObj.GetType().GetProperty("value");
            if (valueProp == null || !valueProp.CanWrite) return;

            if (_detected == DetectedType.Float)
                valueProp.SetValue(_paramObj, floatFrom);
            else if (_detected == DetectedType.Color)
                valueProp.SetValue(_paramObj, colorA);
        }

        public Tween BuildTween()
        {
            if (_paramObj == null) return null;

            var valueProp = _paramObj.GetType().GetProperty("value");
            if (valueProp == null || !valueProp.CanRead || !valueProp.CanWrite) return null;

            if (_detected == DetectedType.Float)
            {
                return DOTween.To(
                        () => (float)valueProp.GetValue(_paramObj),
                        v => valueProp.SetValue(_paramObj, v),
                        floatTo,
                        Mathf.Max(0.0001f, duration)
                    )
                    .SetEase(ease)
                    .SetDelay(delay)
                    .SetUpdate(true)
                    .SetLoops(loops, loopType);
            }
            else if (_detected == DetectedType.Color)
            {
                return DOTween.To(
                        () => (Color)valueProp.GetValue(_paramObj),
                        c => valueProp.SetValue(_paramObj, c),
                        colorB,
                        Mathf.Max(0.0001f, duration)
                    )
                    .SetEase(ease)
                    .SetDelay(delay)
                    .SetUpdate(true)
                    .SetLoops(loops, loopType);
            }

            return null;
        }

        private void DetectSelectedParamType()
        {
            _detected = DetectedType.None;
            _field = null;
            _paramObj = null;

            if (_profile == null || string.IsNullOrEmpty(componentName) || string.IsNullOrEmpty(paramName)) return;

            var comp = ResolveComponent(_profile, componentName, true);
            if (comp == null) return;

            var f = GetVolumeParameterFields(comp, typeof(float)).FirstOrDefault(fi => fi.Name == paramName);
            if (f != null) { _detected = DetectedType.Float; _field = f; return; }

            var c = GetVolumeParameterFields(comp, typeof(Color)).FirstOrDefault(fi => fi.Name == paramName);
            if (c != null) { _detected = DetectedType.Color; _field = c; return; }
        }

        // ---------- Reflection Helpers ----------

        private static VolumeComponent ResolveComponent(VolumeProfile p, string name, bool quiet = false)
        {
            if (p == null || string.IsNullOrEmpty(name)) return null;

            var comp = p.components.FirstOrDefault(c => c != null && c.name == name);
            if (comp == null && !quiet)
                Debug.LogWarning($"[VolumeLooper] Component '{name}' not found in profile '{p.name}'.");
            return comp;
        }

        private static IEnumerable<FieldInfo> GetVolumeParameterFields(VolumeComponent comp, Type wantType)
        {
            // Return fields whose type derives from VolumeParameter<wantType>
            if (comp == null) yield break;

            var flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
            foreach (var f in comp.GetType().GetFields(flags))
            {
                var ft = f.FieldType;
                if (!typeof(VolumeParameter).IsAssignableFrom(ft)) continue;

                if (IsVolumeParameterOf(ft, wantType))
                    yield return f;
            }
        }

        private static bool IsVolumeParameterOf(Type t, Type want)
        {
            while (t != null && t != typeof(object))
            {
                if (t.IsGenericType)
                {
                    var def = t.GetGenericTypeDefinition();
                    var args = t.GetGenericArguments();
                    if (def == typeof(VolumeParameter<>) && args.Length == 1 && args[0] == want)
                        return true;
                }
                t = t.BaseType;
            }
            return false;
        }
    }
}

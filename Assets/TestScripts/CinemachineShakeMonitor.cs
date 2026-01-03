using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

/// <summary>
/// Runtime diagnostics for Cinemachine shake.
/// - Tracks active brain & active camera (including blend)
/// - Tracks all Perlin noise components and logs when amplitude/frequency/profile changes
/// - Optionally inspects impulse manager event count (reflection, best-effort)
/// - Optional overlay (OnGUI)
///
/// Works with Cinemachine 2 & 3 via reflection (fields or properties).
/// </summary>
public class CinemachineShakeMonitor : MonoBehaviour
{
    [Header("Logging")]
    public bool logToConsole = true;
    public bool logOnlyWhenChanged = true;
    public float floatEpsilon = 0.0001f;

    [Header("Overlay")]
    public bool showOverlay = true;
    public KeyCode toggleOverlayKey = KeyCode.F7;
    public int overlayFontSize = 14;
    public int overlayMaxLines = 18;

    [Header("Scanning")]
    public bool rescanScenePeriodically = true;
    public float rescanIntervalSeconds = 1.0f;

    [Header("Optional watchdog")]
    [Tooltip("If enabled, when no 'expected shake tag' this frame, and any active Perlin amplitude > 0, it will log it (and optionally force to 0).")]
    public bool alertWhenAmplitudeNonZeroWithoutTag = true;

    [Tooltip("DANGEROUS: force amplitude to 0 when alert triggers. Use only for debugging.")]
    public bool forceAmplitudeZeroOnAlert = false;

    public KeyCode toggleForceZeroKey = KeyCode.F8;

    // Cached reflection types
    private Type _brainType;
    private Type _perlinType;
    private Type _impulseManagerType;

    // Instances
    private readonly List<Component> _brains = new();
    private readonly List<Component> _perlincs = new();

    // Last snapshots
    private readonly Dictionary<int, NoiseSnapshot> _lastNoise = new();
    private BrainSnapshot _lastBrain;

    // Overlay ring buffer
    private readonly Queue<string> _overlayLines = new();

    private float _nextRescan;

    private void Awake()
    {
        CacheTypes();
        RescanNow();
        _nextRescan = Time.unscaledTime + rescanIntervalSeconds;
    }

    private void Update()
    {
        if (Input.GetKeyDown(toggleOverlayKey)) showOverlay = !showOverlay;
        if (Input.GetKeyDown(toggleForceZeroKey)) forceAmplitudeZeroOnAlert = !forceAmplitudeZeroOnAlert;

        if (rescanScenePeriodically && Time.unscaledTime >= _nextRescan)
        {
            RescanNow();
            _nextRescan = Time.unscaledTime + Mathf.Max(0.1f, rescanIntervalSeconds);
        }

        UpdateBrainDiagnostics();
        UpdateNoiseDiagnostics();
    }

    private void OnGUI()
    {
        if (!showOverlay) return;

        var style = new GUIStyle(GUI.skin.label)
        {
            fontSize = overlayFontSize,
            richText = true
        };

        float x = 12f;
        float y = 12f;

        foreach (var line in _overlayLines)
        {
            GUI.Label(new Rect(x, y, Screen.width - 24f, 999f), line, style);
            y += overlayFontSize + 4;
            if (y > Screen.height - 50) break;
        }

        GUI.Label(
            new Rect(x, Screen.height - 30, Screen.width - 24f, 30),
            $"<color=#aaaaaa>F7 Overlay: {(showOverlay ? "ON" : "OFF")} | F8 ForceZeroOnAlert: {(forceAmplitudeZeroOnAlert ? "ON" : "OFF")} | LastTagFrame: {CinemachineShakeAudit.LastTagFrame}</color>",
            style
        );
    }

    // ------------------- Core diagnostics -------------------

    private void UpdateBrainDiagnostics()
    {
        // Typically you have 1 brain (Main Camera).
        // We log active camera changes + blend changes.
        for (int i = 0; i < _brains.Count; i++)
        {
            var brain = _brains[i];
            bool isActive = brain is Behaviour b && b.isActiveAndEnabled;
            if (!brain || !isActive) continue;

            var snap = ReadBrainSnapshot(brain);
            bool changed = !_lastBrain.Equals(snap);

            if (!logOnlyWhenChanged || changed)
            {
                if (changed)
                {
                    Emit($"<color=#7fd3ff>[Brain]</color> activeCam: <b>{snap.activeCamName}</b>  blend: {snap.blendDesc}");
                }
            }

            _lastBrain = snap;
            break; // prefer first enabled brain
        }
    }

    private void UpdateNoiseDiagnostics()
    {
        int currentFrame = Time.frameCount;
        bool hasExpectedTagThisFrame = CinemachineShakeAudit.LastTagFrame == currentFrame;

        for (int i = 0; i < _perlincs.Count; i++)
        {
            var perlin = _perlincs[i];
            if (!perlin) continue;

            var snap = ReadNoiseSnapshot(perlin);
            int id = perlin.GetInstanceID();

            if (_lastNoise.TryGetValue(id, out var last))
            {
                bool changed =
                    !Approximately(last.amplitude, snap.amplitude) ||
                    !Approximately(last.frequency, snap.frequency) ||
                    !ReferenceEquals(last.profileObj, snap.profileObj) ||
                    last.ownerCamName != snap.ownerCamName ||
                    last.enabled != snap.enabled;

                if (!logOnlyWhenChanged || changed)
                {
                    if (changed)
                    {
                        string tag = (CinemachineShakeAudit.LastTagFrame == currentFrame)
                            ? $"  <color=#ffd27f>tag:</color> {CinemachineShakeAudit.LastTag}"
                            : "";

                        Emit($"<color=#b7ff9b>[Perlin]</color> cam=<b>{snap.ownerCamName}</b> enabled={snap.enabled}  amp {last.amplitude:0.###}->{snap.amplitude:0.###}  freq {last.frequency:0.###}->{snap.frequency:0.###}  profile {(last.profileObj == null ? "null" : last.profileObj.name)}->{(snap.profileObj == null ? "null" : snap.profileObj.name)}{tag}");
                    }
                }
            }
            else
            {
                Emit($"<color=#b7ff9b>[Perlin]</color> detected cam=<b>{snap.ownerCamName}</b> enabled={snap.enabled} amp={snap.amplitude:0.###} freq={snap.frequency:0.###} profile={(snap.profileObj == null ? "null" : snap.profileObj.name)}");
            }

            _lastNoise[id] = snap;

            // Watchdog: amplitude > 0 but no expected tag this frame
            if (alertWhenAmplitudeNonZeroWithoutTag && !hasExpectedTagThisFrame)
            {
                if (snap.enabled && snap.amplitude > floatEpsilon)
                {
                    Emit($"<color=#ff8080>[ALERT]</color> amp>0 without tag this frame. cam=<b>{snap.ownerCamName}</b> amp={snap.amplitude:0.###}. (Someone is keeping shake alive)");
                    if (forceAmplitudeZeroOnAlert)
                    {
                        WritePerlinAmplitude(perlin, 0f);
                        Emit($"<color=#ff8080>[FORCE]</color> set amplitude to 0 on cam=<b>{snap.ownerCamName}</b>");
                    }
                }
            }
        }

        // Optional: impulse manager events count (best effort)
        var impulseCount = TryGetImpulseEventCount();
        if (impulseCount >= 0)
        {
            // Don't spam: only show in overlay
            PushOverlayLine($"<color=#aaaaaa>[Impulse]</color> activeEventsCount={impulseCount}");
        }
    }

    // ------------------- Reflection scanning -------------------

    private void CacheTypes()
    {
        _brainType = FindTypeByNames(
            "Cinemachine.CinemachineBrain",
            "Unity.Cinemachine.CinemachineBrain",
            "CinemachineBrain"
        );

        _perlinType = FindTypeByNames(
            "Cinemachine.CinemachineBasicMultiChannelPerlin",
            "Unity.Cinemachine.CinemachineBasicMultiChannelPerlin",
            "CinemachineBasicMultiChannelPerlin"
        );

        _impulseManagerType = FindTypeByNames(
            "Cinemachine.CinemachineImpulseManager",
            "Unity.Cinemachine.CinemachineImpulseManager",
            "CinemachineImpulseManager"
        );
    }

    private void RescanNow()
    {
        _brains.Clear();
        _perlincs.Clear();

        if (_brainType != null)
        {
            var brains = FindObjectsByTypeAll(_brainType);
            for (int i = 0; i < brains.Count; i++)
            {
                if (brains[i] is Component c && c.gameObject.scene.IsValid())
                    _brains.Add(c);
            }
        }

        if (_perlinType != null)
        {
            var perlins = FindObjectsByTypeAll(_perlinType);
            for (int i = 0; i < perlins.Count; i++)
            {
                if (perlins[i] is Component c && c.gameObject.scene.IsValid())
                    _perlincs.Add(c);
            }
        }

        Emit($"<color=#aaaaaa>[Scan]</color> brains={_brains.Count}, perlins={_perlincs.Count}");
    }

    private static List<UnityEngine.Object> FindObjectsByTypeAll(Type t)
    {
        // Works in runtime too (includes inactive)
        var arr = Resources.FindObjectsOfTypeAll(t);
        var list = new List<UnityEngine.Object>(arr.Length);
        for (int i = 0; i < arr.Length; i++) list.Add(arr[i]);
        return list;
    }

    private static Type FindTypeByNames(params string[] names)
    {
        var asms = AppDomain.CurrentDomain.GetAssemblies();
        for (int n = 0; n < names.Length; n++)
        {
            string name = names[n];
            for (int i = 0; i < asms.Length; i++)
            {
                Type t = asms[i].GetType(name, false);
                if (t != null) return t;
            }
        }

        // Fallback: partial match
        for (int i = 0; i < asms.Length; i++)
        {
            Type[] types;
            try { types = asms[i].GetTypes(); }
            catch { continue; }

            for (int k = 0; k < types.Length; k++)
            {
                if (types[k].Name == names[names.Length - 1]) return types[k];
            }
        }

        return null;
    }

    // ------------------- Brain snapshot -------------------

    private BrainSnapshot ReadBrainSnapshot(Component brain)
    {
        string activeCamName = "null";
        string blendDesc = "none";

        object activeCamObj = GetMemberValue(brain, "ActiveVirtualCamera") ?? GetMemberValue(brain, "ActiveCamera");
        if (activeCamObj != null)
        {
            var camGO = TryGetVirtualCameraGameObject(activeCamObj);
            activeCamName = camGO ? camGO.name : activeCamObj.ToString();
        }

        object blendObj = GetMemberValue(brain, "ActiveBlend");
        if (blendObj != null)
        {
            // Try read blend's fields: CamA, CamB, BlendWeight or Duration/TimeInBlend
            object camA = GetMemberValue(blendObj, "CamA") ?? GetMemberValue(blendObj, "CameraA");
            object camB = GetMemberValue(blendObj, "CamB") ?? GetMemberValue(blendObj, "CameraB");
            float w = ReadFloat(blendObj, "BlendWeight", "m_BlendWeight", "Weight");
            string aName = camA != null ? (TryGetVirtualCameraGameObject(camA)?.name ?? camA.ToString()) : "null";
            string bName = camB != null ? (TryGetVirtualCameraGameObject(camB)?.name ?? camB.ToString()) : "null";
            blendDesc = $"{aName} -> {bName}  w={w:0.##}";
        }

        return new BrainSnapshot(activeCamName, blendDesc);
    }

    private static GameObject TryGetVirtualCameraGameObject(object camObj)
    {
        if (camObj == null) return null;

        // CM2: ICinemachineCamera has VirtualCameraGameObject
        object vgo = GetMemberValue(camObj, "VirtualCameraGameObject");
        if (vgo is GameObject go) return go;

        // CM3: often the camera object itself is a Component
        if (camObj is Component c) return c.gameObject;

        // maybe has GameObject property
        object go2 = GetMemberValue(camObj, "gameObject");
        return go2 as GameObject;
    }

    // ------------------- Noise snapshot -------------------

    private NoiseSnapshot ReadNoiseSnapshot(Component perlin)
    {
        bool enabled = perlin is Behaviour b && b.isActiveAndEnabled;

        float amp = ReadFloat(perlin, "m_AmplitudeGain", "AmplitudeGain", "amplitudeGain");
        float freq = ReadFloat(perlin, "m_FrequencyGain", "FrequencyGain", "frequencyGain");
        UnityEngine.Object profile = ReadUnityObject(perlin, "m_NoiseProfile", "NoiseProfile", "noiseProfile");

        string ownerCamName = perlin.gameObject.name;
        // Try to find parent Cinemachine camera object name if exists (best-effort)
        var t = perlin.transform;
        for (int i = 0; i < 4 && t != null; i++)
        {
            if (t.parent == null) break;
            t = t.parent;
            ownerCamName = t.name;
        }

        return new NoiseSnapshot(ownerCamName, enabled, amp, freq, profile);
    }

    private void WritePerlinAmplitude(Component perlin, float value)
    {
        // field or property
        if (!TrySetFloat(perlin, "m_AmplitudeGain", value))
            TrySetFloat(perlin, "AmplitudeGain", value);
    }

    // ------------------- Impulse (best effort) -------------------

    private int TryGetImpulseEventCount()
    {
        if (_impulseManagerType == null) return -1;

        // Common patterns:
        // - static Instance property
        // - static Singleton field
        object mgr = GetStaticMemberValue(_impulseManagerType, "Instance") ??
                     GetStaticMemberValue(_impulseManagerType, "instance") ??
                     GetStaticMemberValue(_impulseManagerType, "s_Instance");

        if (mgr == null) return -1;

        // Try known fields for active events
        object events = GetMemberValue(mgr, "m_ImpulseEvents") ??
                        GetMemberValue(mgr, "m_ActiveEvents") ??
                        GetMemberValue(mgr, "ImpulseEvents");

        if (events == null) return -1;

        // If it's IList
        if (events is System.Collections.ICollection col) return col.Count;

        // Try Count property
        object countObj = GetMemberValue(events, "Count");
        if (countObj is int c) return c;

        return -1;
    }

    // ------------------- Reflection helpers -------------------

    private static object GetMemberValue(object obj, string name)
    {
        if (obj == null) return null;
        var t = obj.GetType();

        // property
        var p = t.GetProperty(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (p != null) return SafeGet(() => p.GetValue(obj));

        // field
        var f = t.GetField(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (f != null) return SafeGet(() => f.GetValue(obj));

        return null;
    }

    private static object GetStaticMemberValue(Type t, string name)
    {
        if (t == null) return null;

        var p = t.GetProperty(name, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
        if (p != null) return SafeGet(() => p.GetValue(null));

        var f = t.GetField(name, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
        if (f != null) return SafeGet(() => f.GetValue(null));

        return null;
    }

    private static float ReadFloat(object obj, params string[] names)
    {
        for (int i = 0; i < names.Length; i++)
        {
            object v = GetMemberValue(obj, names[i]);
            if (v is float f) return f;
            if (v is double d) return (float)d;
            if (v is int n) return n;
        }
        return 0f;
    }

    private static UnityEngine.Object ReadUnityObject(object obj, params string[] names)
    {
        for (int i = 0; i < names.Length; i++)
        {
            object v = GetMemberValue(obj, names[i]);
            if (v is UnityEngine.Object uo) return uo;
        }
        return null;
    }

    private static bool TrySetFloat(object obj, string name, float value)
    {
        if (obj == null) return false;
        var t = obj.GetType();

        var p = t.GetProperty(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (p != null && p.CanWrite && p.PropertyType == typeof(float))
        {
            try { p.SetValue(obj, value); return true; } catch { return false; }
        }

        var f = t.GetField(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (f != null && f.FieldType == typeof(float))
        {
            try { f.SetValue(obj, value); return true; } catch { return false; }
        }

        return false;
    }

    private static T SafeGet<T>(Func<T> f)
    {
        try { return f(); } catch { return default; }
    }

    private bool Approximately(float a, float b) => Mathf.Abs(a - b) <= floatEpsilon;

    // ------------------- Output -------------------

    private void Emit(string msg)
    {
        string line = $"<color=#aaaaaa>[f={Time.frameCount} t={Time.unscaledTime:0.000}]</color> {msg}";
        if (logToConsole) Debug.Log(StripRichText(line));
        PushOverlayLine(line);
    }

    private void PushOverlayLine(string richLine)
    {
        // Keep overlay compact
        while (_overlayLines.Count >= overlayMaxLines) _overlayLines.Dequeue();
        _overlayLines.Enqueue(richLine);
    }

    private static string StripRichText(string s)
    {
        // Console log doesn't need rich text; keep it readable
        // Minimal strip (not full HTML parsing)
        return s.Replace("<b>", "").Replace("</b>", "");
    }

    // ------------------- Snapshot structs -------------------

    private readonly struct BrainSnapshot
    {
        public readonly string activeCamName;
        public readonly string blendDesc;

        public BrainSnapshot(string activeCamName, string blendDesc)
        {
            this.activeCamName = activeCamName ?? "null";
            this.blendDesc = blendDesc ?? "none";
        }

        public bool Equals(BrainSnapshot other)
            => activeCamName == other.activeCamName && blendDesc == other.blendDesc;
    }

    private readonly struct NoiseSnapshot
    {
        public readonly string ownerCamName;
        public readonly bool enabled;
        public readonly float amplitude;
        public readonly float frequency;
        public readonly UnityEngine.Object profileObj;

        public NoiseSnapshot(string ownerCamName, bool enabled, float amp, float freq, UnityEngine.Object profileObj)
        {
            this.ownerCamName = ownerCamName ?? "unknown";
            this.enabled = enabled;
            amplitude = amp;
            frequency = freq;
            this.profileObj = profileObj;
        }
    }
}

/// <summary>
/// Optional "audit tag" helper.
/// Call CinemachineShakeAudit.Tag(this, "reason") right before you change Perlin values.
/// The monitor will print tag if it detects change in the same frame.
/// </summary>
public static class CinemachineShakeAudit
{
    public static string LastTag { get; private set; } = "";
    public static int LastTagFrame { get; private set; } = -1;

    public static void Tag(UnityEngine.Object owner, string tag)
    {
        LastTag = owner ? $"{owner.name} :: {tag}" : tag;
        LastTagFrame = Time.frameCount;
    }
}

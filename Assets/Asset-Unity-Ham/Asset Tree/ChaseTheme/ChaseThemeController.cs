using System;
using System.Collections;
using System.Reflection;
using UnityEngine;

namespace HeartbeatThemeSystem
{
    [DisallowMultipleComponent]
    public class ChaseThemeController : MonoBehaviour
    {
        [Serializable]
        public class MusicZone
        {
            public string zoneName = "Zone";
            [Min(0.1f)] public float radius = 15f;
            public AudioClip music;
            [HideInInspector] public bool isPlayerInside;
        }

        [Header("Zones")]
        [SerializeField] private MusicZone[] zones = Array.Empty<MusicZone>();

        [Header("Crossfade")]
        [SerializeField, Min(0.1f)] private float fadeDuration = 2f;
        [SerializeField, Range(0f, 1f)] private float maxVolume = 1f;
        [SerializeField, Min(0.1f)] private float preLoopFadeTime = 1f;

        private AudioSource sourceA;
        private AudioSource sourceB;
        private AudioSource activeSource;
        private AudioSource fadingSource;
        private AudioClip currentClip;
        private bool isFading;

        private Transform resolvedPlayer;
        private static readonly Type PlayerType = Type.GetType("Player")
                                             ?? Type.GetType("Player, Assembly-CSharp");
        private static readonly PropertyInfo PlayerInstanceProperty =
            PlayerType?.GetProperty("Instance", BindingFlags.Public | BindingFlags.Static);
        private const string ResourceFolder = "HeartbeatThemeSystem";

        private static readonly (string label, float radius, string clipName)[] DefaultZones =
        {
            ("layer1", 90f, "layer1"),
            ("layer2", 60f, "layer2"),
            ("layer3", 30f, "layer3")
        };

        private void Reset()
        {
            SetupDefaultZones();
        }

        private void OnValidate()
        {
#if UNITY_EDITOR
            if (Application.isPlaying) return;
            EnsureDefaultZones();
            AssignDefaultClips();
#endif
        }

        private void Awake()
        {
            EnsureDefaultZones();
            AssignDefaultClips();
            sourceA = CreateSource("ChaseTheme_A");
            sourceB = CreateSource("ChaseTheme_B");
            activeSource = sourceA;
            fadingSource = sourceB;
        }

        private void Update()
        {
            var player = ResolvePlayer();
            if (!player || zones == null || zones.Length == 0) return;

            Vector3 controllerPosition = transform.position;
            float distance = Vector3.Distance(player.position, controllerPosition);
            MusicZone nearestZone = null;
            float smallestRadius = float.PositiveInfinity;

            foreach (var zone in zones)
            {
                if (zone == null) continue;
                float radius = Mathf.Max(zone.radius, 0.1f);
                zone.isPlayerInside = distance <= radius;

                if (zone.isPlayerInside && radius < smallestRadius)
                {
                    smallestRadius = radius;
                    nearestZone = zone;
                }
            }

            if (nearestZone != null && !isFading)
            {
                if (nearestZone.music != currentClip)
                {
                    StartCoroutine(CrossfadeTo(nearestZone.music));
                    currentClip = nearestZone.music;
                }
                else if (activeSource.clip != null &&
                         activeSource.clip.length - activeSource.time <= preLoopFadeTime)
                {
                    StartCoroutine(CrossfadeTo(activeSource.clip));
                }
            }

            if (nearestZone == null && !isFading && activeSource.isPlaying)
            {
                StartCoroutine(FadeOut(activeSource));
                currentClip = null;
            }
        }

        private AudioSource CreateSource(string label)
        {
            var src = gameObject.AddComponent<AudioSource>();
            src.playOnAwake = false;
            src.loop = true;
            src.volume = 0f;
            src.name = label;
            return src;
        }

        private IEnumerator CrossfadeTo(AudioClip newClip)
        {
            if (newClip == null) yield break;

            isFading = true;
            fadingSource.clip = newClip;
            fadingSource.volume = 0f;
            fadingSource.Play();

            float timer = 0f;
            while (timer < fadeDuration)
            {
                float t = timer / fadeDuration;
                activeSource.volume = Mathf.Lerp(maxVolume, 0f, t);
                fadingSource.volume = Mathf.Lerp(0f, maxVolume, t);
                timer += Time.deltaTime;
                yield return null;
            }

            activeSource.Stop();
            var temp = activeSource;
            activeSource = fadingSource;
            fadingSource = temp;
            isFading = false;
        }

        private IEnumerator FadeOut(AudioSource source)
        {
            isFading = true;
            float startVolume = source.volume;
            float timer = 0f;

            while (timer < fadeDuration)
            {
                float t = timer / fadeDuration;
                source.volume = Mathf.Lerp(startVolume, 0f, t);
                timer += Time.deltaTime;
                yield return null;
            }

            source.Stop();
            isFading = false;
        }

        private Transform ResolvePlayer()
        {
            if (resolvedPlayer) return resolvedPlayer;

            if (PlayerInstanceProperty != null)
            {
                var singleton = PlayerInstanceProperty.GetValue(null) as Component;
                if (singleton)
                {
                    return resolvedPlayer = singleton.transform;
                }
            }

            var taggedPlayer = GameObject.FindGameObjectWithTag("Player");
            if (taggedPlayer)
            {
                return resolvedPlayer = taggedPlayer.transform;
            }

            return null;
        }

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            if (zones == null) return;

            foreach (var zone in zones)
            {
                if (zone == null) continue;
                Gizmos.color = Color.cyan;
                Gizmos.DrawWireSphere(transform.position, zone.radius);
            }
        }
#endif

        private void EnsureDefaultZones()
        {
            if (zones != null && zones.Length > 0) return;
            SetupDefaultZones();
        }

        private void SetupDefaultZones()
        {
            zones = new MusicZone[DefaultZones.Length];
            for (int i = 0; i < DefaultZones.Length; i++)
            {
                zones[i] = new MusicZone
                {
                    zoneName = DefaultZones[i].label,
                    radius = DefaultZones[i].radius
                };
            }
        }

        private void AssignDefaultClips()
        {
            if (zones == null) return;
            for (int i = 0; i < zones.Length && i < DefaultZones.Length; i++)
            {
                if (zones[i] == null || zones[i].music != null) continue;
                string resourcePath = string.IsNullOrEmpty(ResourceFolder)
                    ? DefaultZones[i].clipName
                    : $"{ResourceFolder}/{DefaultZones[i].clipName}";

                zones[i].music = Resources.Load<AudioClip>(resourcePath);
            }
        }
    }
}


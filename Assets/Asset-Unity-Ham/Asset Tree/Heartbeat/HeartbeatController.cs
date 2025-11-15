using System;
using System.Reflection;
using UnityEngine;

namespace HeartbeatThemeSystem
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(AudioSource))]
    public class HeartbeatController : MonoBehaviour
    {
        [Header("Detection")]
        [SerializeField, Min(0f)] private float detectionRadius = 20f;
        [SerializeField, HideInInspector] private string playerTag = "Player";

        [Header("Audio")]
        [SerializeField] private AudioClip heartbeatClip;
        [SerializeField, Range(0f, 1f)] private float minVolume = 0.15f;
        [SerializeField, Range(0f, 1f)] private float maxVolume = 0.6f;
        [SerializeField] private float minPitch = 0.75f;
        [SerializeField] private float maxPitch = 1.2f;

        [Header("Runtime (read-only)")]
        [SerializeField, HideInInspector, Tooltip("Current distance from the tracked player.")]
        private float currentDistance;

        private AudioSource heartbeatSource;
        private Transform resolvedPlayer;

        private static readonly Type PlayerType = Type.GetType("Player")
                                             ?? Type.GetType("Player, Assembly-CSharp");
        private static readonly PropertyInfo PlayerInstanceProperty =
            PlayerType?.GetProperty("Instance", BindingFlags.Public | BindingFlags.Static);
        private const string ResourceFolder = "HeartbeatThemeSystem";
        private const string DefaultHeartbeatClipName = "HeartBeat";

        private void Reset()
        {
            AssignDefaultClip();
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (Application.isPlaying) return;
            AssignDefaultClip();
        }
#endif

        private void Awake()
        {
            AssignDefaultClip();
            heartbeatSource = GetComponent<AudioSource>();
            heartbeatSource.loop = true;
            heartbeatSource.playOnAwake = false;
            heartbeatSource.clip = heartbeatClip ?? heartbeatSource.clip;
        }

        private void Update()
        {
            var target = ResolvePlayer();
            if (!target)
            {
                if (heartbeatSource.isPlaying)
                {
                    heartbeatSource.Stop();
                }
                return;
            }

            currentDistance = Vector3.Distance(transform.position, target.position);

            if (currentDistance > detectionRadius || heartbeatSource.clip == null)
            {
                if (heartbeatSource.isPlaying)
                {
                    heartbeatSource.Stop();
                }
                return;
            }

            float t = Mathf.Clamp01(currentDistance / Mathf.Max(detectionRadius, 0.001f));
            heartbeatSource.pitch = Mathf.Lerp(maxPitch, minPitch, t);
            heartbeatSource.volume = Mathf.Lerp(maxVolume, minVolume, t);

            if (!heartbeatSource.isPlaying)
            {
                heartbeatSource.Play();
            }
        }

        private Transform ResolvePlayer()
        {
            if (resolvedPlayer) return resolvedPlayer;

            // Try Player singleton via reflection so the asset can live alone.
            if (PlayerInstanceProperty != null)
            {
                var singleton = PlayerInstanceProperty.GetValue(null) as Component;
                if (singleton)
                {
                    return resolvedPlayer = singleton.transform;
                }
            }

            // Fallback to tag search.
            if (!string.IsNullOrWhiteSpace(playerTag))
            {
                var tagged = GameObject.FindGameObjectWithTag(playerTag);
                if (tagged)
                {
                    return resolvedPlayer = tagged.transform;
                }
            }

            return null;
        }

        private void AssignDefaultClip()
        {
            if (heartbeatClip) return;
            string resourcePath = string.IsNullOrEmpty(ResourceFolder)
                ? DefaultHeartbeatClipName
                : $"{ResourceFolder}/{DefaultHeartbeatClipName}";
            heartbeatClip = Resources.Load<AudioClip>(resourcePath);
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, detectionRadius);
        }
#endif
    }
}


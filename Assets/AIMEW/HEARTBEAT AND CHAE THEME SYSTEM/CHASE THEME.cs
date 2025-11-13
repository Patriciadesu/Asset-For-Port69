using UnityEngine;
using System.Collections;

[RequireComponent(typeof(AudioSource))]
public class CHASETHEME : MonoBehaviour
{
    [System.Serializable]
    public class MusicZone
    {
        public string zoneName;
        public Transform zoneObject; // GameObject แทน center ของโซน
        public float radius = 10f;
        public AudioClip music;
        [HideInInspector] public bool isPlayerInside;
    }

    [Header("Player Reference")]
    public Transform player;

    [Header("Music Zones")]
    public MusicZone[] zones;

    [Header("Crossfade Settings")]
    public float fadeDuration = 2f;       // เวลาสำหรับ crossfade
    public float maxVolume = 1f;          // volume สูงสุด
    public float preLoopFadeTime = 1.5f;  // วินาทีก่อนจบเพลง เพื่อ crossfade กับตัวเอง

    private AudioSource sourceA;
    private AudioSource sourceB;
    private AudioSource activeSource;
    private AudioSource fadingSource;
    private AudioClip currentClip;
    private bool isFading;

    void Start()
    {
        sourceA = gameObject.AddComponent<AudioSource>();
        sourceB = gameObject.AddComponent<AudioSource>();

        foreach (var src in new[] { sourceA, sourceB })
        {
            src.loop = true;
            src.playOnAwake = false;
            src.volume = 0;
        }

        activeSource = sourceA;
        fadingSource = sourceB;
    }

    void Update()
    {
        if (player == null || zones.Length == 0) return;

        MusicZone nearestZone = null;
        float nearestDistance = Mathf.Infinity;

        foreach (var zone in zones)
        {
            if (zone.zoneObject == null) continue;
            float distance = Vector3.Distance(player.position, zone.zoneObject.position);
            zone.isPlayerInside = distance <= zone.radius;

            if (zone.isPlayerInside && distance < nearestDistance)
            {
                nearestZone = zone;
                nearestDistance = distance;
            }
        }

        if (nearestZone != null && !isFading)
        {
            // เพลงใหม่จากโซน
            if (nearestZone.music != currentClip)
            {
                StartCoroutine(CrossfadeTo(nearestZone.music));
                currentClip = nearestZone.music;
            }
            // เพลงเดิมใกล้จบ → crossfade กับตัวเองเพื่อ seamless loop
            else if (activeSource.clip != null && activeSource.clip.length - activeSource.time <= preLoopFadeTime)
            {
                StartCoroutine(CrossfadeTo(activeSource.clip));
            }
        }

        // ถ้าออกจากทุกโซน → fade out
        if (nearestZone == null && !isFading && activeSource.isPlaying)
        {
            StartCoroutine(FadeOut(activeSource));
            currentClip = null;
        }
    }

    IEnumerator CrossfadeTo(AudioClip newClip)
    {
        isFading = true;

        fadingSource.clip = newClip;
        fadingSource.volume = 0;
        fadingSource.loop = true;
        fadingSource.Play();

        float timer = 0f;
        while (timer < fadeDuration)
        {
            float t = timer / fadeDuration;
            activeSource.volume = Mathf.Lerp(maxVolume, 0, t);
            fadingSource.volume = Mathf.Lerp(0, maxVolume, t);
            timer += Time.deltaTime;
            yield return null;
        }

        activeSource.Stop();
        var temp = activeSource;
        activeSource = fadingSource;
        fadingSource = temp;
        isFading = false;
    }

    IEnumerator FadeOut(AudioSource source)
    {
        isFading = true;
        float startVolume = source.volume;

        while (source.volume > 0)
        {
            source.volume -= Time.deltaTime / fadeDuration * startVolume;
            yield return null;
        }

        source.Stop();
        isFading = false;
    }

    private void OnDrawGizmos()
    {
        if (zones == null) return;

        foreach (var zone in zones)
        {
            if (zone.zoneObject == null) continue;
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(zone.zoneObject.position, zone.radius);

#if UNITY_EDITOR
            UnityEditor.Handles.Label(zone.zoneObject.position + Vector3.up * 2, zone.zoneName);
#endif
        }
    }
}

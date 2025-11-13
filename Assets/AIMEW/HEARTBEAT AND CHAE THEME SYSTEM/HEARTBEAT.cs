using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class HEARTBEAT : MonoBehaviour
{
    [Header("Detection Settings")]
    public float detectionRadius = 100f; // รัศมีตรวจจับ
    public string playerTag = "Player"; // Tag ของผู้เล่น

    [Header("Heartbeat Settings")]
    private float minPitch = 0.1f;  // เมื่ออยู่ไกลสุด
    private float maxPitch = 2.0f;  // เมื่ออยู่ใกล้สุด
    public float minVolume = 0.1f; // เมื่ออยู่ไกลสุด
    public float maxVolume = 0.5f; // เมื่ออยู่ใกล้สุด
    public AudioClip HeartBeatAudioClip; // เสียงหัวใจเต้น

    private AudioSource audioSource;
    private Transform player;
    private float distance;

    void Start()
    {
        audioSource = GetComponent<AudioSource>();
        audioSource.loop = true;
        audioSource.playOnAwake = false;

        if (HeartBeatAudioClip != null)
            audioSource.clip = HeartBeatAudioClip;
    }

    void Update()
    {
        if (player == null)
        {
            GameObject found = GameObject.FindGameObjectWithTag(playerTag);
            if (found != null) player = found.transform;
            return;
        }

        distance = Vector3.Distance(transform.position, player.position);

        if (distance <= detectionRadius)
        {
            if (!audioSource.isPlaying)
                audioSource.Play();

            // คำนวณอัตราส่วนระยะทาง (0 = ใกล้, 1 = ไกล)
            float t = Mathf.Clamp01(distance / detectionRadius);

            // ปรับ pitch และ volume ตามระยะ
            audioSource.pitch = Mathf.Lerp(maxPitch, minPitch, t);
            audioSource.volume = Mathf.Lerp(maxVolume, minVolume, t);
        }
        else
        {
            if (audioSource.isPlaying)
                audioSource.Stop();
        }
    }

    // แสดง Sphere Gizmo ใน Scene
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);
    }
}

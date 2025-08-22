using NaughtyAttributes;
using System.Security.Cryptography.X509Certificates;
using UnityEngine;

public class BowShot : PlayerExtension
{
    [Header("Properties")]
    public KeyCode activateKey = KeyCode.Mouse0;
    public float DrawTime = 3f;
    float pauseTime = 0.4f;

    bool canShot = false;
    private bool isHolding = false;  // กำลังกดค้างอยู่ไหม
    private bool isPaused = false;  // ถูกหยุดค้างแล้วหรือยัง
    private float clipLength = 0f;    // ความยาวคลิป (วินาที)

    [Header("Arrow Properties")]
    public GameObject projectilePrefab;
    public Transform spawnPoint;
    public float speed = 10f;

    public bool hasDestroyTime = true;
    [ShowIf("hasDestroyTime")] public float DestroyTime = 2f;

    public override void OnStart(Player player)
    {
        base.OnStart(player);
        CacheClipLength(); // อ่านความยาวคลิปตอนนี้ (ปลอดภัยกว่า Start/Awake)
    }
    void CacheClipLength()
    {
        if (_player == null)
        {
            Debug.LogError("[BowShot] _player ยังเป็น null; ตรวจว่า Player เรียก OnStart ให้หรือยัง");
            return;
        }
        var anim = _player.animator;
        if (!anim) { Debug.LogError("[BowShot] ไม่พบ Animator บน Player"); return; }
        var controller = anim.runtimeAnimatorController;
        if (!controller) { Debug.LogError("[BowShot] Animator ไม่มี RuntimeAnimatorController"); return; }

        clipLength = 0f;
        foreach (var c in controller.animationClips)
        {
            if (c && c.name == "BowShot")
            {
                clipLength = c.length;
                break;
            }
        }
        if (clipLength <= 0f)
            Debug.LogWarning($"[BowShot] ไม่พบคลิปชื่อ {"BowShot"} หรือความยาวเป็น 0");
    }
    void Update()
    {
        // ระหว่างกดค้าง: คอยตรวจว่าถึงเวลาค้างหรือยัง
        CheckHolding();
        if (Input.GetKeyDown(activateKey))
        {
            DrawBow();
        } 

        // ปล่อยปุ่ม: ให้เล่นต่อจากที่ค้างไว้
        if (Input.GetKeyUp(activateKey))
        {
            ShotBow();
        }

        if (Input.GetKeyUp(activateKey) && !canShot)
        {
            _player.animator.speed = 1f;
            _player.animator.Play("Idle", 0 ,0 );
        }
    }

    private void DrawBow()
    {
        if (!canShot)
        {
            isHolding = true;
            isPaused = false;

            // คำนวณ speed ให้ถึง pauseTime ใช้เวลาตาม DrawTime
            float p = Mathf.Clamp01(pauseTime);
            float L = Mathf.Max(clipLength, 0.0001f);
            float T = Mathf.Max(DrawTime, 0.0001f);
            float speedForDraw = (p * L) / T;       // สูตรสำคัญ
            _player.animator.speed = Mathf.Max(speedForDraw, 0.001f);

            _player.animator.Play("BowShot", 0, 0f);
        }
        
    }

    void CheckHolding()
    {
        if (isHolding && !isPaused)
        {
            var info = _player.animator.GetCurrentAnimatorStateInfo(0);

            if (info.IsName("BowShot"))
            {
                // ถ้าคลิปถูกติ๊ก Loop ไว้ normalizedTime จะวิ่งเกิน 1 ไปเรื่อย ๆ
                // ใช้ % 1f เพื่อดึงตำแหน่งในรอบปัจจุบัน
                float t = info.normalizedTime % 1f;

                // ใช้ >= แทน == และกันพลาดด้วย overshoot เล็กน้อย
                if (t + Time.deltaTime >= pauseTime)
                {
                    _player.animator.speed = 0f; // ค้างไว้ตรงนี้
                    isPaused = true;
                    canShot = true;
                }
            }
        }
    }
    void ShotBow()
    {
        if (canShot)
        {
            canShot = false;
            isHolding = false;
            _player.animator.speed = 1f;
            Shoot();
        }
        
    }
    void Shoot()
    {
        // สร้าง projectile ที่ตำแหน่ง spawnPoint
        GameObject projectile = Instantiate(projectilePrefab, spawnPoint.position, spawnPoint.rotation);
        if (hasDestroyTime)
        {
            Destroy(projectile, DestroyTime);
        }
        // ให้มันพุ่งไปข้างหน้า
        Rigidbody rb = projectile.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.linearVelocity = spawnPoint.forward * speed;
        }
    }
}

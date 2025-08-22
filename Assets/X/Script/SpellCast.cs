using NaughtyAttributes;
using UnityEngine;
using static UnityEditor.Experimental.GraphView.GraphView;

public class SpellCast : PlayerExtension
{
    [Header("Input")]
    public KeyCode activateKey = KeyCode.Mouse0;

    string stateName = "SpellCast"; // ชื่อ State/Clip ใน Animator
    float firePointNormalized = 0.65f; // จุดในคลิปที่จะยิง (0..1)
    int layer = 0;

    [Header("Spell")]
    public GameObject projectilePrefab;
    public Transform spawnPoint;
    public float timeToFire = 0.8f;
    public float speed = 12f;


    public bool hasDestroyTime = true;
    [ShowIf("hasDestroyTime")] public float destroyTime = 2f;

    // ภายใน
    float clipLength = 0f;
    bool isHolding = false; // กำลังกดค้างอยู่ไหม
    bool hasFired = false; // ยิงไปแล้วหรือยังในรอบนี้

    public bool useStamina;
    [ShowIf("useStamina")] public float staminaCost = 2f;
    public bool isUsingStamina => useStamina && hasFired;
    public bool canDrainStamina => _player.currentstamina >= staminaCost && useStamina;
    public void DrainStamina(float amount)
    {
        if (canDrainStamina)
        {
            _player.currentstamina = Mathf.Max(_player.currentstamina - amount, 0f);
        }
    }
    // แนะนำให้ Player เรียก OnStart เมื่อ Player พร้อม (_player ถูกเซ็ตแน่ๆ)
    public override void OnStart(Player player)
    {
        base.OnStart(player);
        CacheClipLength(); // อ่านความยาวคลิปตอนนี้ (ปลอดภัยกว่า Start/Awake)
    }

    void Update()
    {
        // เริ่มกด -> เริ่มร่าย
        if (Input.GetKeyDown(activateKey))
            BeginCast();

        // ระหว่างกดค้าง -> ตรวจว่าถึงจุดยิงหรือยัง
        TickUntilFirePoint();

        // ปล่อยก่อนถึงจุดยิง -> ยกเลิก
        if (Input.GetKeyUp(activateKey) && !hasFired)
        {
            CancelCast();
            // ถ้าปล่อยหลังยิงแล้ว จะปล่อยให้แอนิเมชันไปต่อหรือจะตัดกลับ Idle ก็ได้:
            // else _player.animator.CrossFadeInFixedTime("Idle", 0.05f);
        }
    }

    void CacheClipLength()
    {
        if (_player == null)
        {
            Debug.LogError("[SpellCast] _player ยังเป็น null; ตรวจว่า Player เรียก OnStart ให้หรือยัง");
            return;
        }
        var anim = _player.animator;
        if (!anim) { Debug.LogError("[SpellCast] ไม่พบ Animator บน Player"); return; }
        var controller = anim.runtimeAnimatorController;
        if (!controller) { Debug.LogError("[SpellCast] Animator ไม่มี RuntimeAnimatorController"); return; }

        clipLength = 0f;
        foreach (var c in controller.animationClips)
        {
            if (c && c.name == stateName)
            {
                clipLength = c.length;
                break;
            }
        }
        if (clipLength <= 0f)
            Debug.LogWarning($"[SpellCast] ไม่พบคลิปชื่อ {stateName} หรือความยาวเป็น 0");
    }

    void BeginCast()
    {
        if (!hasFired)
        {
            // รีเซ็ตสถานะรอบใหม่
            isHolding = true;
            hasFired = false;

            // ให้ถึง firePointNormalized ภายใน timeToFirePoint
            float p = Mathf.Clamp01(firePointNormalized);
            float L = (clipLength > 0f) ? clipLength : 1f;
            float T = Mathf.Max(timeToFire, 0.0001f);
            float speedToFire = (p * L) / T; // speed = ระยะทางในคลิป / เวลา

            var anim = _player.animator;
            anim.speed = Mathf.Max(speedToFire, 0.001f);
            anim.Play(stateName, layer, 0f); // เริ่มจากต้นทุกครั้ง
        }
        
    }

    void TickUntilFirePoint()
    {
        if (isHolding && !hasFired)
        {
            var info = _player.animator.GetCurrentAnimatorStateInfo(layer);
            if (!info.IsName(stateName)) return;

            // ถ้าคลิป Loop, normalizedTime จะ > 1; ใช้ % 1 ให้อยู่รอบปัจจุบัน
            float t = info.normalizedTime % 1f;

            // ใช้ >= ไม่ใช้ == และกันหลุดเฟรม
            if (t >= firePointNormalized - 0.001f)
            {
                hasFired = true;
                FireSpell();
            }
        }
        
    }

    void FireSpell()
    {
        if (hasFired)
        {
            hasFired = false;
            isHolding = false;

            if (canDrainStamina)
            {
                DrainStamina(staminaCost);
            }

            if (!projectilePrefab || !spawnPoint)
            {
                Debug.LogWarning("[SpellCast] projectilePrefab หรือ spawnPoint ยังไม่ถูกเซ็ต");
                return;
            }

            GameObject projectile = Instantiate(projectilePrefab, spawnPoint.position, Quaternion.identity);
            if (hasDestroyTime && destroyTime > 0f) Destroy(projectile, destroyTime);

            Rigidbody rb = projectile.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.useGravity = false;                 //  ปิดแรงโน้มถ่วงเฉพาะลูกนี้
                rb.linearDamping = 0f;                          // ให้คงความเร็ว (ไม่หน่วงอากาศ)
                rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic; // กันทะลุ
                rb.linearVelocity = spawnPoint.forward * speed; // พุ่งไปข้างหน้า (3D นิยมใช้ forward)
            }
        }
    }

    void CancelCast()
    {
        isHolding = false;
        hasFired = false;
        var anim = _player.animator;
        anim.speed = 1f; // เผื่อเคยปรับ
        anim.CrossFadeInFixedTime("Idle", 0.05f); // ตัดกลับ Idle นุ่ม ๆ
    }
}
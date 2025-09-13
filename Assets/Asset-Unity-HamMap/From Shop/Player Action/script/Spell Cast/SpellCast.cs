using NaughtyAttributes;
using UnityEngine;
using static UnityEditor.Experimental.GraphView.GraphView;

public class SpellCast : PlayerExtension
{
    [Header("Input")]
    public KeyCode activateKey = KeyCode.Mouse0;

    string stateName = "SpellCast"; // ���� State/Clip � Animator
    float firePointNormalized = 0.65f; // �ش㹤�Ի�����ԧ (0..1)
    int layer = 0;

    [Header("Spell")]
    public GameObject projectilePrefab;
    public Transform spawnPoint;
    public float damage;
    public float timeToFire = 0.8f;
    public float speed = 12f;


    public bool hasDestroyTime = true;
    [ShowIf("hasDestroyTime")] public float destroyTime = 2f;

    // ����
    float clipLength = 0f;
    bool isHolding = false; // ���ѧ����ҧ�������
    bool hasFired = false; // �ԧ����������ѧ��ͺ���

    public bool useStamina;
    [ShowIf("useStamina")] public float staminaCost = 2f;
    public bool isUsingStamina => useStamina && hasFired;
    public bool canDrainStamina => _player.Stat.currentstamina >= staminaCost && useStamina;
    public void DrainStamina(float amount)
    {
        if (canDrainStamina)
        {
            _player.Stat.currentstamina = Mathf.Max(_player.Stat.currentstamina - amount, 0f);
        }
    }
    // �й���� Player ���¡ OnStart ����� Player ����� (_player �١�����)
    public override void OnStart(Player player)
    {
        base.OnStart(player);
        CacheClipLength(); // ��ҹ������Ǥ�Ի�͹��� (��ʹ��¡��� Start/Awake)
    }

    void Update()
    {
        // ������� -> ���������
        if (Input.GetKeyDown(activateKey))
            BeginCast();

        // �����ҧ����ҧ -> ��Ǩ��Ҷ֧�ش�ԧ�����ѧ
        TickUntilFirePoint();

        // ����¡�͹�֧�ش�ԧ -> ¡��ԡ
        if (Input.GetKeyUp(activateKey) && !hasFired)
        {
            CancelCast();
            // ��һ������ѧ�ԧ���� �л��������͹����ѹ仵�����ͨеѴ��Ѻ Idle ����:
            // else _player.animator.CrossFadeInFixedTime("Idle", 0.05f);
        }
    }

    void CacheClipLength()
    {
        if (_player == null)
        {
            Debug.LogError("[SpellCast] _player �ѧ�� null; ��Ǩ��� Player ���¡ OnStart ��������ѧ");
            return;
        }
        var anim = _player.animator;
        if (!anim) { Debug.LogError("[SpellCast] ��辺 Animator �� Player"); return; }
        var controller = anim.runtimeAnimatorController;
        if (!controller) { Debug.LogError("[SpellCast] Animator ����� RuntimeAnimatorController"); return; }

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
            Debug.LogWarning($"[SpellCast] ��辺��Ի���� {stateName} ���ͤ�������� 0");
    }

    void BeginCast()
    {
        if (!hasFired)
        {
            // ����ʶҹ��ͺ����
            isHolding = true;
            hasFired = false;

            // ���֧ firePointNormalized ���� timeToFirePoint
            float p = Mathf.Clamp01(firePointNormalized);
            float L = (clipLength > 0f) ? clipLength : 1f;
            float T = Mathf.Max(timeToFire, 0.0001f);
            float speedToFire = (p * L) / T; // speed = ���зҧ㹤�Ի / ����

            var anim = _player.animator;
            anim.speed = Mathf.Max(speedToFire, 0.001f);
            anim.Play(stateName, layer, 0f); // ������ҡ�鹷ء����
        }
        
    }

    void TickUntilFirePoint()
    {
        if (isHolding && !hasFired)
        {
            var info = _player.animator.GetCurrentAnimatorStateInfo(layer);
            if (!info.IsName(stateName)) return;

            // ��Ҥ�Ի Loop, normalizedTime �� > 1; �� % 1 ��������ͺ�Ѩ�غѹ
            float t = info.normalizedTime % 1f;

            // �� >= ����� == ��Сѹ��ش���
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

            if (canDrainStamina) DrainStamina(staminaCost);

            if (spawnPoint == null)
            {
                spawnPoint = Player.Instance.camera.transform;
            }

            GameObject projectile = null;
            if (!projectilePrefab)
            {
                Quaternion rotation = (_player.Cam.cameraType == CameraType.FirstPerson) ? _player.camera.transform.rotation : _player.tpsVirtualCamera.transform.rotation;
                projectile = GameObject.CreatePrimitive(PrimitiveType.Cube);
                projectile.transform.position = spawnPoint.position;
                projectile.transform.rotation = Quaternion.identity;
                projectile.gameObject.transform.localScale = new Vector3(.5f, .5f, .5f);
                projectile.AddComponent<Rigidbody>();
            }
            else
            {
                projectile = Instantiate(projectilePrefab, spawnPoint.position, Quaternion.identity);
                if (projectile.GetComponent<Rigidbody>() == null)
                {
                    projectile.AddComponent<InteractableObject>();
                }
            }


            if (hasDestroyTime && destroyTime > 0f) Destroy(projectile, destroyTime);

            Rigidbody rb = projectile.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.useGravity = false;                 //  �Դ�ç�����ǧ੾���١���
                rb.linearDamping = 0f;                          // ��餧�������� (���˹�ǧ�ҡ��)
                rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic; // �ѹ����
                rb.linearVelocity = spawnPoint.forward * speed; // ���仢�ҧ˹�� (3D ������ forward)
            }
            MagicBullet magicBullet = projectile.GetComponent<MagicBullet>();
            if (magicBullet == null) magicBullet = projectile.AddComponent<MagicBullet>();
            magicBullet.SetUp(damage);
        }
    }

    void CancelCast()
    {
        isHolding = false;
        hasFired = false;
        var anim = _player.animator;
        anim.speed = 1f; // �����»�Ѻ
        anim.CrossFadeInFixedTime("Idle", 0.05f); // �Ѵ��Ѻ Idle ���� �
    }
}
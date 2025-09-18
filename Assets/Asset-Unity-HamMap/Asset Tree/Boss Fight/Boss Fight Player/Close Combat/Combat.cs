using NaughtyAttributes;
using System.Collections.Generic;
using UnityEngine;


public class Combat : PlayerExtension, IUseStamina
{
    [Header("Properties")]
    public KeyCode activateKey = KeyCode.Mouse0;
    public float cooldownTime = 0.5f;
    public float holdThreshold = 2f;
    public float damage = 10f;
    [Range(1, 10)] public float damagemultiplier = 2f;

    float oneHandHitWindow = 0.15f; // �������ҹѺⴹ�ͧ����Ф���
    float twoHandHitWindow = 0.20f;
    float heldTime;
    float lastPunchTime = 0f;
    bool isPunch = false;
    bool isHolding = false;
    bool is2hand;
    float keyDownTime = 0f;

    bool attackWindowOpen;
    HashSet<IEnemy> hitThisSwing = new();   // �ѹ�ի���ѵ�ٵ��������ԧ����

    bool isReadyToPunch => Time.time >= lastPunchTime + cooldownTime;
    bool CanPunch => isReadyToPunch && _player.Stat.currentstamina >= staminaCost;

    public bool useStamina;
    [ShowIf("useStamina")] public float staminaCost = 2f;
    [ShowIf("useStamina")][Range(1, 10)] public int staminaCostMultiplyer = 3;
    public bool isUsingStamina => useStamina && isPunch;
    public bool canDrainStamina => _player.Stat.currentstamina >= staminaCost && useStamina;

    public void DrainStamina(float amount)
    {
        if (canDrainStamina)
            _player.Stat.currentstamina = Mathf.Max(_player.Stat.currentstamina - amount, 0f);
    }

    public void SetUp(float _damage) => damage = _damage;

    void Update()
    {
        // ��������� (������ظ)
        if (Input.GetKey(activateKey)) PrepareCombat();
        else FinishedPrepareCombat();

        if (Input.GetKeyDown(activateKey) && CanPunch)
        {
            keyDownTime = Time.time;
            isHolding = true;
        }

        // �����
        if (Input.GetKeyUp(activateKey) && isHolding)
        {
            PrepareCombat();
            heldTime = Time.time - keyDownTime;

            if (heldTime < holdThreshold) Melee1Hand();
            else Melee2Hand();

            isHolding = false;
        }
    }

    void Melee1Hand()
    {
        if (isReadyToPunch)
        {
            if (isPunch) return;
            isPunch = true;
            attackWindowOpen = true;
            hitThisSwing.Clear();

            if (canDrainStamina) DrainStamina(staminaCost);

            _player.animator.SetTrigger("MeleeAttack1hand");

            // �Դ˹�ҵ�ҧⴹ੾�Ъ�ǧ���
            Invoke(nameof(CloseAttackWindow), oneHandHitWindow);
            Invoke(nameof(FinishedPunch), 0.2f);
        }
        
    }

    void Melee2Hand()
    {
        if (isReadyToPunch)
        {
            is2hand = true;
            if (isPunch) return;
            isPunch = true;
            attackWindowOpen = true;
            hitThisSwing.Clear();

            if (canDrainStamina) DrainStamina(staminaCost * staminaCostMultiplyer);

            _player.animator.SetTrigger("MeleeAttack2hand");

            Invoke(nameof(CloseAttackWindow), twoHandHitWindow);
            Invoke(nameof(FinishedPunch), 0.5f);
        }
    }

        void PrepareCombat() => _player.animator.SetBool("PrepareCombat", true);
    void FinishedPrepareCombat() => _player.animator.SetBool("PrepareCombat", false);

    void FinishedPunch()
    {
        if (!isPunch) return;
        isPunch = false;
        lastPunchTime = Time.time;
        attackWindowOpen = false;     // safety �Դ˹�ҵ�ҧ
        _player.animator.speed = 1f;
        is2hand = false;
    }

    void CloseAttackWindow() => attackWindowOpen = false;

    // ---------- �Ӵ���� ----------
    void TryHit(IEnemy boss)
    {
        if (!attackWindowOpen) return;
        if (!is2hand)
        {
            if (hitThisSwing.Add(boss))   // �����á�ͧ�ѵ�ٵ�ǹ�����ԧ�����ҹ��
                boss.TakeDamage(damage);
        }else
        {
            if (hitThisSwing.Add(boss))
                boss.TakeDamage(damage * damagemultiplier);
        }
        
    }


    // ���� Enter + Stay ��������Ҵ�ó��������͹�Ѻ�ѹ��������
    void OnTriggerEnter(Collider other) { if (other.TryGetComponent(out IEnemy b)) TryHit(b); }
    void OnTriggerStay(Collider other) { if (other.TryGetComponent(out IEnemy b)) TryHit(b); }

    void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.TryGetComponent(out IEnemy b)) TryHit(b);
    }
    void OnCollisionStay(Collision collision)
    {
        if (collision.gameObject.TryGetComponent(out IEnemy b)) TryHit(b);
    }
}

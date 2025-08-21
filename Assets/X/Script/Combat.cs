using NaughtyAttributes;
using Unity.VisualScripting.Dependencies.Sqlite;
using UnityEngine;


public class Combat : PlayerExtension, IUseStamina
{
    [Header("Properties")]
    public KeyCode activateKey = KeyCode.Mouse0;
    public float cooldownTime = 0.5f;
    public float holdThreshold = 2f;

    [SerializeField]
    float heldTime;
    private float lastPunchTime = 0f;
    private bool isPunch = false;
    private bool isHolding = false;
    private float keyDownTime = 0f; 

    private bool isReadyToPunch => Time.time >= lastPunchTime + cooldownTime;
    private bool CanPunch => isReadyToPunch && _player.currentstamina >= staminaCost;

    public bool useStamina;
    [ShowIf("useStamina")] public float staminaCost = 2f;
    [ShowIf("useStamina")] [Range(1, 10)] public int staminaCostMultiplyer;
    public bool isUsingStamina => useStamina && isPunch;
    public bool canDrainStamina => _player.currentstamina >= staminaCost && useStamina;
    public void DrainStamina(float amount)
    {
        if (canDrainStamina)
        {
            _player.currentstamina = Mathf.Max(_player.currentstamina - amount, 0f);
        }
    }
    void Update()
    {
        if (Input.GetKey(activateKey))
        {
            PrepareCombat();
        }
        else
        {
            FinishedPrepareCombat();
        }

        if (Input.GetKeyDown(activateKey) && CanPunch)
        {
            keyDownTime = Time.time;
            isHolding = true;
        }

        // ตอนปล่อยปุ่ม
        if (Input.GetKeyUp(activateKey) && isHolding)
        {
            PrepareCombat();
            heldTime = Time.time - keyDownTime;

            if (heldTime < holdThreshold)
            {
                Melee1Hand();
            }
            else
            {
                Melee2Hand();
            }

            isHolding = false;
        }
    }
    void Melee1Hand()
    {
        if (!isPunch)
        {
            isPunch = true;
            if (canDrainStamina)
            {
                DrainStamina(staminaCost);
            }
            _player.animator.SetTrigger("MeleeAttack1hand");
            Invoke(nameof(FinishedPunch), 0.2f);
        }
        
    }
    void Melee2Hand()
    {
        if (!isPunch)
        {
            isPunch = true;

            if (canDrainStamina)
            {
                DrainStamina(staminaCost * staminaCostMultiplyer);
            }
            _player.animator.SetTrigger("MeleeAttack2hand");
            Invoke(nameof(FinishedPunch), 0.5f);
        }
            
    }
    void PrepareCombat()
    {
       _player.animator.SetBool("PrepareCombat",true);

    }
    void FinishedPrepareCombat()
    {
       _player.animator.SetBool("PrepareCombat", false);
    }
    void FinishedPunch()
    {
        if (isPunch)
        {
            isPunch = false;
            lastPunchTime = Time.time;
            _player.animator.speed = 1f;
        }
        
    }
}

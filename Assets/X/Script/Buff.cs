using NaughtyAttributes;
using UnityEngine;

public class Buff : PlayerExtension, IUseStamina
{
    [Header("Properties")]
    public KeyCode activateKey = KeyCode.G;
    public float cooldownTime = 1f;

    [SerializeField]
    float heldTime;
    private float lastBuffTime = 0f;
    private bool isBuff = false;

    private bool isReadyToBuff => Time.time >= lastBuffTime + cooldownTime;
    private bool CanBuff => isReadyToBuff && _player.currentstamina >= staminaCost;

    public bool useStamina;
    [ShowIf("useStamina")] public float staminaCost = 2f;
    [ShowIf("useStamina")][Range(1, 10)] public int staminaCostMultiplyer;
    public bool isUsingStamina => useStamina && isBuff;
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
        if (Input.GetKeyUp(activateKey) && CanBuff)
        {
            UseBuff();
        }
    }
    
    void UseBuff()
    {
        if (!isBuff)
        {
            isBuff = true;
            if (canDrainStamina)
            {
                DrainStamina(staminaCost);
            }
            _player.animator.SetTrigger("Buff");
            Invoke(nameof(FinishedBuff), 0.2f);
        }
        
    }
    void FinishedBuff()
    {
        if (isBuff)
        {
            isBuff = false;
            lastBuffTime = Time.time;
            _player.animator.speed = 1f;
        }
    }
}

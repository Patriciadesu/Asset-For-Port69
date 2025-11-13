using NaughtyAttributes;
using UnityEngine;

public class Block : PlayerExtension, IUseStamina
{
    [Header("Properties")]
    public KeyCode activateKey = KeyCode.Mouse1;

    public float blockCooldown = 1;
    [Range(0,100)]public float damageReductionPercentage;
    public bool useStamina;
    [ShowIf("useStamina")] public float staminaCost = 5f;
    private float blockCooldownTimer = 0;
    private bool isBlocking = false;
    private bool canBlock => Time.time >= blockCooldownTimer;
    public bool isUsingStamina => useStamina;
    public bool canDrainStamina => _player.Stat.currentstamina >= staminaCost && useStamina;
    public void DrainStamina(float amount)
    {
        if (canDrainStamina)
        {
            _player.Stat.currentstamina = Mathf.Max(_player.Stat.currentstamina - amount, 0f);
        }
    }

    void Update()
    {
        if (Input.GetKeyDown(activateKey) && canBlock && canDrainStamina)
        {
            StartBlocking();
        }
        else if (Input.GetKeyUp(activateKey) && _player.Stat.canHit == false)
        {
            StopBlocking();
        }

        if (_player.Stat.canHit && isBlocking)
        {
            StopBlocking();
        }

    }

    void StartBlocking()
    {
        isBlocking = true;
        _player.Stat.canHit = false;
        _player.animator.SetBool("isBlocking", true);
    }

    void StopBlocking()
    {
        blockCooldownTimer = Time.time + blockCooldown;
        isBlocking = false;
        _player.Stat.canHit = true;
        _player.animator.SetBool("isBlocking", false);
    }
}
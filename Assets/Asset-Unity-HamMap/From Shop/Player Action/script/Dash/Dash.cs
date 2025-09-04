using System.Security.Cryptography;
using NaughtyAttributes;
using UnityEngine;

public class Dash : PlayerExtension, IUseStamina, IInteruptPlayerMovement
{
    [Header("UI")]
    public bool enableDashUI = true;

    [Header("Properties")]
    public KeyCode activateKey = KeyCode.R;
    public float dashSpeed = 5f;
    public float cooldownTime = 1f;

    private float lastDashTime = 0f;
    // Expose dashing state for stamina regen check
    private bool isDashing = false;
    public bool isPerforming => isDashing;
    private bool isReadyToDash => Time.time >= lastDashTime + cooldownTime;
    private bool CanDash => _player.canMove && _player.isGrounded && _player.canApplyGravity && isReadyToDash && _player.currentstamina >= staminaCost;
    private float dashAnimSpeed => dashSpeed / _player.GetAnimationLength("dash");


    public bool useStamina;
    [ShowIf("useStamina")] public float staminaCost = 10f;
    public bool isUsingStamina => useStamina && isDashing;
    public bool canDrainStamina => _player.currentstamina >= staminaCost && useStamina;


    public void DrainStamina(float amount)
    {
        if (canDrainStamina)
        {
            _player.currentstamina = Mathf.Max(_player.currentstamina - amount, 0f);
        }
    }
    public override void OnStart(Player player)
    {
        base.OnStart(player);
        lastDashTime = -cooldownTime; // Set initial time to allow immediate use
    }

    protected void Update()
    {

        if (Input.GetKeyDown(activateKey) && CanDash)
        {
            StartDash();
        }
    }

    private void StartDash()
    {
        // Begin dashing state
        isDashing = true;
        if (canDrainStamina)
        {
            DrainStamina(staminaCost);
        }
        float inputX = Input.GetAxis("Horizontal");
        float inputZ = Input.GetAxis("Vertical");
        Vector3 dashDirection = new Vector3(inputX, 0, inputZ).normalized;
        if (dashDirection.magnitude < 0.1f)
        {
            dashDirection = _player.transform.forward;
        }
        _player.rigidbody.linearVelocity = new Vector3(dashDirection.x * dashSpeed, _player.rigidbody.linearVelocity.y, dashDirection.z * dashSpeed);
        _player.animator.speed = dashAnimSpeed;
        _player.animator.SetTrigger("dash");
        Invoke(nameof(FinishDash), 0.2f);
    }

    private void FinishDash()
    {
        _player.rigidbody.linearVelocity = new Vector3(0, _player.rigidbody.linearVelocity.y, 0);
        // End dashing state
        isDashing = false;
        lastDashTime = Time.time;
        _player.animator.speed = 1f;
    }
}

public partial class PlayerUIManager : Singleton<PlayerUIManager>
{
    public bool enableDashCooldownUI = true;
    public void UpdateDashCooldown(float timeSinceLastDash, float cooldownTime)
    {
        if (!enableDashCooldownUI) return;
        bool isOnCooldown = timeSinceLastDash < cooldownTime;
        dashCooldownUI.gameObject.SetActive(isOnCooldown);
        if (isOnCooldown)
            dashCooldownUI.value = timeSinceLastDash / cooldownTime;
    }

}
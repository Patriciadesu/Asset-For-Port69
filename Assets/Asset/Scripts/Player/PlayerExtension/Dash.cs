using UnityEngine;

public class Dash : PlayerExtension
{
    [Header("UI")]
    public bool enableDashUI = true;
    private PlayerUIManager uiManager;

    [Header("Properties")]
    public KeyCode activateKey = KeyCode.R;
    public float dashSpeed = 5f;
    public float cooldownTime = 1f;
    public bool useStamina = true; // Toggle stamina consumption during dash
    public float staminaCost = 15f; // Stamina consumed per dash
    private float lastDashTime = 0f;
    // Expose dashing state for stamina regen check
    private bool isDashing = false;
    public bool IsDashing => isDashing;
    private bool isReadyToDash => Time.time >= lastDashTime + cooldownTime;
    private bool CanDash => _player.canMove && _player.isGrounded && _player.canApplyGravity && isReadyToDash && _player.currentstamina >= staminaCost;
    private float dashAnimSpeed => dashSpeed / _player.GetAnimationLength("dash");
    

    public override void OnStart(Player player)
    {
        base.OnStart(player);
        lastDashTime = -cooldownTime; // Set initial time to allow immediate use
        if (enableDashUI)
            uiManager = Object.FindAnyObjectByType<PlayerUIManager>();
    }

    protected void Update()
    {
        if (enableDashUI && uiManager != null)
            uiManager.UpdateDashCooldown(Time.time - lastDashTime, cooldownTime);
            
        if (Input.GetKeyDown(activateKey) && CanDash)
        {
            StartDash();
        }
    }

    private void StartDash()
    {
        // Begin dashing state
        isDashing = true;
        _player.canMove = false;
        if (useStamina)
        {
            _player.currentstamina -= staminaCost;
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
        _player.canMove = true;
        // End dashing state
        isDashing = false;
        lastDashTime = Time.time;
        _player.animator.speed = 1f;
    }
}
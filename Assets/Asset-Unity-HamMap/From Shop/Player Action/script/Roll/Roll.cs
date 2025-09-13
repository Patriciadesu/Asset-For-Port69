
using NaughtyAttributes;
using UnityEngine;

public class Roll : PlayerExtension, IUseStamina,IInteruptPlayerMovement
{
    [Header("UI")]
    public bool enableRollUI = true;
    private PlayerUIManager uiManager;

    [Header("Properties")]
    public KeyCode activateKey = KeyCode.Q;
    public float rollSpeed = 1f;
    public float rollDuration = 0.15f;
    public float cooldownTime = 1f;
    public bool isPerforming  => isRolling;



    public bool useStamina = true; // Toggle stamina consumption during roll
    [ShowIf("useStamina")] public float staminaCost = 15f;
    public bool isUsingStamina => useStamina && isRolling;
    public bool canDrainStamina => _player.Stat.currentstamina >= staminaCost && useStamina;



    private float lastRollTime = 0f;
    private bool isReadyToRoll => Time.time >= lastRollTime + cooldownTime;
    private Vector3 rollDirection;
    private float rollAnimSpeed => rollSpeed / _player.GetAnimationLength("Slide");
    private bool isRolling = false;
    private bool CanRoll => _player.canMove && _player.Movement.isGrounded && _player.canApplyGravity && isReadyToRoll && _player.Stat.currentstamina >= staminaCost;

    public override void OnStart(Player player)
    {
        base.OnStart(player);
        lastRollTime = -cooldownTime; // Set initial time to allow immediate use
        if (enableRollUI)
            uiManager = Object.FindAnyObjectByType<PlayerUIManager>();
    }

    protected void Update()
    {

        if (enableRollUI && uiManager != null)
            uiManager.UpdateRollCooldown(Time.time - lastRollTime, cooldownTime);

        if (isRolling)
        {
            Debug.Log("Player is Rolling");
            Debug.Log("PLayer's Rigidbody is null?" + _player.rigidbody == null);
            Vector3 rollVelocity = rollDirection * rollSpeed * _player.Movement.Speed;
            _player.rigidbody.linearVelocity = new Vector3(rollVelocity.x, _player.rigidbody.linearVelocity.y, rollVelocity.z);
        }
        else if (Input.GetKeyDown(activateKey) && CanRoll)
        {
            StartRoll();
        }
    }

    public void DrainStamina(float amount)
    {
        if (canDrainStamina)
        {
            _player.Stat.currentstamina = Mathf.Max(_player.Stat.currentstamina - amount, 0f);
        }
    }

    private void StartRoll()
    {
        isRolling = true;
        _player.canRotateCamera = false;
        if (canDrainStamina)
        {
            DrainStamina(staminaCost);
        }
        if (_player.Cam.cameraType == CameraType.FirstPerson)
        {
            _player.camera.transform.SetParent(_player.fpsCameraPivot);
        }
        CapsuleCollider collider = _player.capsuleCollider;
        collider.height /= 2;
        collider.center = new Vector3(collider.center.x, collider.center.y / 2, collider.center.z);
        rollDirection = _player.transform.forward;
        _player.animator.speed = rollAnimSpeed;
        _player.animator.SetTrigger("Slide");
        Invoke(nameof(StopRoll), rollDuration + 0.25f);
    }

    void StopRoll()
    {
        // Modify collider back instead of controller
        isRolling = false;
        if (_player.Cam.cameraType == CameraType.FirstPerson)
        {
            _player.camera.transform.SetParent(_player.transform);
        }
        CapsuleCollider collider = _player.capsuleCollider;
        if (collider != null)
        {
            collider.height *= 2;
            collider.center = new Vector3(collider.center.x, collider.center.y * 2, collider.center.z);
        }
        _player.canRotateCamera = true;
        _player.animator.speed = 1;
        lastRollTime = Time.time; // Reset the last roll time
    }

    void IUseStamina.DrainStamina(float amount)
    {
        throw new System.NotImplementedException();
    }
}

public partial class PlayerUIManager : Singleton<PlayerUIManager>
{
    public bool enableRollCooldownUI = true;
    public void UpdateRollCooldown(float timeSinceLastRoll, float cooldownTime)
    {
        if (!enableRollCooldownUI) return;
        bool isOnCooldown = timeSinceLastRoll < cooldownTime;
        rollCooldownUI.gameObject.SetActive(isOnCooldown);
        if (isOnCooldown)
            rollCooldownUI.value = timeSinceLastRoll / cooldownTime;
    }
}

public class RollUISetter : IPlayerUISetter
{
    public void OnStart(PlayerUIManager playerUI)
    {
        if (playerUI.enableRollCooldownUI) playerUI.rollCooldownUI.gameObject.SetActive(false);
    }
}


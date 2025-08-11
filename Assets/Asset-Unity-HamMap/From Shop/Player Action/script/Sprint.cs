using NaughtyAttributes;
using UnityEngine;

public class Sprint : PlayerExtension
{
    [Header("UI")]
    public bool enableSprintUI = true;
    private PlayerUIManager uiManager;

    [Header("Properties")]
    public KeyCode activateKey = KeyCode.LeftShift;
    public float sprintSpeed = 8f;
    public bool useStamina = true; // Toggle stamina consumption during sprint
    [ShowIf("useStamina")]public float sprintCost = 10f; // Stamina consumed per second
    private bool isSprinting = false;
    public bool IsSprinting => isSprinting;

    

    public override void OnStart(Player player)
    {
        base.OnStart(player);
        if (enableSprintUI)
            uiManager = Object.FindAnyObjectByType<PlayerUIManager>();
    }

    protected void Update()
    {
        Player.Instance.canGenerateStamina = isSprinting;
        bool canSprint = _player.canMove && _player.currentstamina > 0;
        if (Input.GetKey(activateKey) && canSprint && !isSprinting)
        {
            StartSprint();
        }
        else if (Input.GetKeyUp(activateKey) || !canSprint)
        {
            StopSprint();
        }

        if (isSprinting)
        {
            if (useStamina)
            {
                _player.currentstamina -= sprintCost * Time.deltaTime;
            }
            if (_player.currentstamina <= 0)
            {
                StopSprint();
            }
        }

        if (enableSprintUI && uiManager != null)
            uiManager.UpdateSprint(isSprinting);
    }

    private void StartSprint()
    {
        isSprinting = true;
        _player.additionalSpeed += sprintSpeed;
        _player.animator.SetBool("isRunning", true);
    }

    private void StopSprint()
    {
        if (isSprinting)
        {
            isSprinting = false;
            _player.additionalSpeed -= sprintSpeed;
            _player.animator.SetBool("isRunning", false);
        }
    }
}
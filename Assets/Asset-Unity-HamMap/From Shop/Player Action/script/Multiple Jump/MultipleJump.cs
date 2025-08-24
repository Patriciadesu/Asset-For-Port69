using UnityEngine;

public class MultipleJump : PlayerExtension
{
    [Header("UI")]
    public bool enableMultipleJumpUI = true;
    private PlayerUIManager uiManager;

    [Header("Properties")]
    public KeyCode activateKey = KeyCode.Space;
    public int maxJumps = 3;
    private int jumpCount;

    public override void OnStart(Player player)
    {
        base.OnStart(player);
        if (enableMultipleJumpUI)
            uiManager = Object.FindAnyObjectByType<PlayerUIManager>();
    }

    protected void Update()
    {
        if (_player.isGrounded)
        {
            jumpCount = 1;
        }
        else if (Input.GetKeyDown(activateKey) && jumpCount < maxJumps && _player.canApplyGravity)
        {
            _player.Jump();
            jumpCount++;
        }

        if (enableMultipleJumpUI && uiManager != null)
            uiManager.UpdateMultipleJump(jumpCount, maxJumps, _player.isGrounded);
    }
}
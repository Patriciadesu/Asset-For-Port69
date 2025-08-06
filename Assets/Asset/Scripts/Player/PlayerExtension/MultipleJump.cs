using UnityEngine;

public class MultipleJump : PlayerExtension
{
    public KeyCode activateKey = KeyCode.Space;
    public int maxJumps = 2;
    private int jumpCount;

    protected void Update()
    {
        if (_player.isGrounded) {
            jumpCount = 0;
        }
        else if(Input.GetKeyDown(activateKey) && jumpCount < maxJumps && _player.canApplyGravity)
        {
            _player.Jump();
            jumpCount++;
        }
    }
}

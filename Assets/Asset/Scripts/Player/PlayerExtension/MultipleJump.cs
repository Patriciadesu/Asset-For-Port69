using UnityEngine;

public class MultipleJump : PlayerExtension
{
    public KeyCode activateKey = KeyCode.Space;
    public int maxJumps = 2;
    private int jumpCount;

    public void Update()
    {
        if (_player.isGrounded)
            jumpCount = 0;

        if (Input.GetKeyDown(activateKey) && jumpCount < maxJumps)
        {
            _player.Jump();
            jumpCount++;
        }
    }
}

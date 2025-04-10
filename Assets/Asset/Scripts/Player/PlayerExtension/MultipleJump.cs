using UnityEngine;

public class MultipleJump : PlayerExtension
{
    public KeyCode activateKey = KeyCode.Space;
    public int maxJumps = 2;
    private int jumpCount;

    public override void OnUpdate(PlayerController player)
    {
        if (player.isGrounded)
            jumpCount = 0;

        if (Input.GetKeyDown(activateKey) && jumpCount < maxJumps)
        {
            player.Jump();
            jumpCount++;
        }
    }
}

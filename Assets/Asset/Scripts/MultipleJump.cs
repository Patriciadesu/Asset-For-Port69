using UnityEngine;

public class MultipleJump : PlayerExtension
{
    public KeyCode activateKey = KeyCode.Space;
    public int maxJumps = 2;
    private int jumpCount;

    public override void Apply(PlayerController player)
    {
        if (player.isGrounded)
            jumpCount = 0;

        if (Input.GetKeyDown(activateKey) && jumpCount < maxJumps)
        {
            if (jumpCount == 0) { 
                player.Jump();
            }
            jumpCount++;
        }
    }
}

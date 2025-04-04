using UnityEngine;

public class Sprint : PlayerExtension
{
    PlayerController _player;
    public KeyCode activateKey = KeyCode.LeftShift;
    public float sprintSpeed = 8f;

    public override void Apply(PlayerController player)
    {
        _player = player;
        if (Input.GetKey(activateKey))
        {
            player.speed = sprintSpeed;
            player.anim.SetBool("isRunning", true);
        }
        else
        {
            player.speed = player.initialSpeed;
            player.anim.SetBool("isRunning", false);
        }
    }
}

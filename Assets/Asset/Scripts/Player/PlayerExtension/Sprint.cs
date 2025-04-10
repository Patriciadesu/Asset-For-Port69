using UnityEngine;

public class Sprint : PlayerExtension
{
    PlayerController _player;
    public KeyCode activateKey = KeyCode.LeftShift;
    public float sprintSpeed = 8f;
    public override void OnUpdate(PlayerController player)
    {
        if (Input.GetKey(activateKey))
        {
            player.additionalSpeed += sprintSpeed;
            player.animator.SetBool("isRunning", true);
        }
        else
        {
            player.additionalSpeed -= sprintSpeed;
            player.animator.SetBool("isRunning", false);
        }
    }
}

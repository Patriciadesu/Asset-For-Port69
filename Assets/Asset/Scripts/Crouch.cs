using UnityEngine;

public class Crouch : PlayerExtension
{
    PlayerController _player;
    public float crouchSpeed = 2f;

    public override void Apply(PlayerController player)
    {
        _player = player;
        if (Input.GetKeyDown(KeyCode.C))
        {
            ToggleCrouch();
        }
    }
    public void ToggleCrouch()
    {
        _player.isCrouching = !_player.isCrouching;
        _player.anim.SetBool("isCrouching", _player.isCrouching);
        if (_player.isCrouching)
        {
            _player.speed = crouchSpeed;
        }
        else
        {
            _player.speed = _player.initialSpeed;
        }
    }
}

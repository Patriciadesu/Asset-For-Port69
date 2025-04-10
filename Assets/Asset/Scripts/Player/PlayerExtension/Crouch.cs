using UnityEngine;

public class Crouch : PlayerExtension
{
    PlayerController _player;
    public float crouchSpeed = 2f;
    public override void OnStart(PlayerController player)
    {
        _player = player;
    }
    public override void OnUpdate(PlayerController player)
    {
        if (Input.GetKeyDown(KeyCode.C))
        {
            ToggleCrouch();
        }
    }
    public void ToggleCrouch()
    {
        _player.isCrouching = !_player.isCrouching;
        _player.animator.SetBool("isCrouching", _player.isCrouching);
        if (_player.isCrouching)
        {
            _player.additionalSpeed += crouchSpeed;
            _player.GetComponent<CapsuleCollider>().height /= 2;
            _player.GetComponent<CapsuleCollider>().center = new Vector3(_player.GetComponent<CapsuleCollider>().center.x, _player.GetComponent<CapsuleCollider>().center.y / 2, _player.GetComponent<CapsuleCollider>().center.z);
        }
        else
        {
            _player.additionalSpeed -= crouchSpeed;
            _player.GetComponent<CapsuleCollider>().height *= 2;
            _player.GetComponent<CapsuleCollider>().center = new Vector3(_player.GetComponent<CapsuleCollider>().center.x, _player.GetComponent<CapsuleCollider>().center.y * 2, _player.GetComponent<CapsuleCollider>().center.z);
        }
    }
}

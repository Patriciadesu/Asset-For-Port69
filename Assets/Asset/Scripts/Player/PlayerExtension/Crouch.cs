using UnityEngine;

public class Crouch : PlayerExtension
{
    public float crouchSpeed = 2f;
    public KeyCode activateKey = KeyCode.C;
    public void Update()
    {
        if (Input.GetKeyDown(activateKey) &&_player.CanCrouch)
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
            _player.additionalSpeed -= crouchSpeed;
            _player.GetComponent<CapsuleCollider>().height /= 2;
            _player.GetComponent<CapsuleCollider>().center = new Vector3(_player.GetComponent<CapsuleCollider>().center.x, _player.GetComponent<CapsuleCollider>().center.y / 2, _player.GetComponent<CapsuleCollider>().center.z);
        }
        else
        {
            _player.additionalSpeed += crouchSpeed;
            _player.GetComponent<CapsuleCollider>().height *= 2;
            _player.GetComponent<CapsuleCollider>().center = new Vector3(_player.GetComponent<CapsuleCollider>().center.x, _player.GetComponent<CapsuleCollider>().center.y * 2, _player.GetComponent<CapsuleCollider>().center.z);
        }
    }
}

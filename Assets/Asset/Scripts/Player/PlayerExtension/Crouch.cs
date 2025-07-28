using UnityEngine;

public class Crouch : PlayerExtension
{
    public float crouchSpeed = 2f;
    public KeyCode activateKey = KeyCode.C;
    public bool isCrouching = false;
    public bool CanCrouch
    {
        get
        {
            return _player.canMove && _player.isGrounded && _player.canApplyGravity;
        }
    }
    protected override void OnUpdate()
    {
        if (Input.GetKeyDown(activateKey) && CanCrouch)
        {
            ToggleCrouch();
        }
    }
    public void ToggleCrouch()
    {
        isCrouching = !isCrouching;
        _player.animator.SetBool("isCrouching", isCrouching);
        if (isCrouching)
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

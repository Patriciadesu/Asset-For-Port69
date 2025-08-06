using UnityEngine;

public class Dash : PlayerExtension
{
    public KeyCode activateKey = KeyCode.LeftShift;
    public float dashSpeed = 5f;
    public float cooldownTime = 1f;
    private float dashAnimSpeed => dashSpeed / _player.GetAnimationLength("dash");
    private float lastDashTime = 0f;
    private bool isReadyToDash => Time.time >= lastDashTime + cooldownTime;
    private bool CanDash => _player.canMove && _player.isGrounded && _player.canApplyGravity && isReadyToDash;
    protected void Update()
    {
        

        if (Input.GetKeyDown(activateKey) && CanDash)
        {
            _player.canMove = false;
            //Dash from Input direction
            float inputX = Input.GetAxis("Horizontal");
            float inputZ = Input.GetAxis("Vertical");
            Vector3 dashDirection = new Vector3(inputX, 0, inputZ).normalized;
            if (dashDirection.magnitude < 0.1f)
            {
                dashDirection = _player.transform.forward; // Default to forward if no input
            }

            // Apply the dash effect
            _player.rigidbody.linearVelocity = new Vector3(dashDirection.x * dashSpeed, _player.rigidbody.linearVelocity.y, dashDirection.z * dashSpeed);
            _player.animator.speed = dashAnimSpeed;
            _player.animator.SetTrigger("dash");

            // Reset velocity after a short duration
            this.Invoke("FinishDash", 0.2f);
        }
    }
    void FinishDash()
    {
        _player.rigidbody.linearVelocity = new Vector3(0, _player.rigidbody.linearVelocity.y, 0);
        _player.canMove = true;
        lastDashTime = Time.time; // Reset the last dash time
        _player.animator.speed = 1f; // Reset animation speed
    }


}

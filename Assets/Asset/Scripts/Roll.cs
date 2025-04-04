using UnityEngine;

public class Roll : PlayerExtension
{
    PlayerController _player;
    public KeyCode activateKey = KeyCode.Q;
    public float slideSpeed = 10f;
    public float slideDuration = 0.5f;
    private Vector3 slideDirection;
    private float slideAnimSpeed;

    public override void Apply(PlayerController player)
    {
        if (_player == null) _player = player;
        if (player.isSliding)
        {
            // Changed from controller.Move to Rigidbody movement
            Vector3 slideVelocity = (slideDirection * slideSpeed) * player.speed;
            _player.rb.linearVelocity = new Vector3(slideVelocity.x, _player.rb.linearVelocity.y, slideVelocity.z);
        }
        else
        {
            if (Input.GetKeyDown(activateKey))
            {
                slideAnimSpeed = slideSpeed / player.GetAnimationLength("Slide");
                player.isSliding = true;

                // Modify collider instead of controller properties
                CapsuleCollider collider = player.GetComponent<CapsuleCollider>();
                if (collider != null)
                {
                    collider.height /= 2;
                    collider.center = new Vector3(collider.center.x, collider.center.y / 2, collider.center.z);
                }

                slideDirection = player.transform.forward; // Lock slide direction
                player.anim.speed = slideAnimSpeed;
                player.anim.SetTrigger("Slide");
                this.Invoke("StopSlide", slideDuration + 0.25f);
            }
        }
    }

    void StopSlide()
    {
        // Modify collider back instead of controller
        CapsuleCollider collider = _player.GetComponent<CapsuleCollider>();
        if (collider != null)
        {
            collider.height *= 2;
            collider.center = new Vector3(collider.center.x, collider.center.y * 2, collider.center.z);
        }

        _player.isSliding = false;
        _player.anim.speed = 1;
    }
}
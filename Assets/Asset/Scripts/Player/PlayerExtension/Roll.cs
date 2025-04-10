using UnityEngine;

public class Roll : PlayerExtension
{
    PlayerController _player;
    public KeyCode activateKey = KeyCode.Q;
    public float slideSpeed = 2f;
    public float slideDuration = 0.5f;
    private Vector3 slideDirection;
    private float slideAnimSpeed;
    public override void OnStart(PlayerController player)
    {
        _player = player;
    }
    public override void OnUpdate(PlayerController player)
    {
        if (player.isSliding)
        {
            Vector3 slideVelocity = (slideDirection * slideSpeed) * player.Speed;
            _player.rigidbody.linearVelocity = new Vector3(slideVelocity.x, _player.rigidbody.linearVelocity.y, slideVelocity.z);
        }
        else
        {
            if (Input.GetKeyDown(activateKey))
            {
                slideAnimSpeed = slideSpeed / player.GetAnimationLength("Slide");
                player.isSliding = true;

                // Modify collider instead of controller properties
                CapsuleCollider collider = _player.capsuleCollider;
                if (collider != null)
                {
                    collider.height /= 2;
                    collider.center = new Vector3(collider.center.x, collider.center.y / 2, collider.center.z);
                }

                slideDirection = player.transform.forward; // Lock slide direction
                player.animator.speed = slideAnimSpeed;
                player.animator.SetTrigger("Slide");
                this.Invoke("StopSlide", slideDuration + 0.25f);
            }
        }
    }

    void StopSlide()
    {
        // Modify collider back instead of controller
        CapsuleCollider collider = _player.capsuleCollider;
        if (collider != null)
        {
            collider.height *= 2;
            collider.center = new Vector3(collider.center.x, collider.center.y * 2, collider.center.z);
        }

        _player.isSliding = false;
        _player.animator.speed = 1;
    }
}
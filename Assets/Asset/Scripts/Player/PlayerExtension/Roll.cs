using UnityEngine;

public class Roll : PlayerExtension
{
    public KeyCode activateKey = KeyCode.Q;
    public float slideSpeed = 2f;
    public float slideDuration = 0.5f;
    private Vector3 slideDirection;
    private float slideAnimSpeed;
    public void Update()
    {
        if (_player.isSliding)
        {
            Vector3 slideVelocity = (slideDirection * slideSpeed) * _player.Speed;
            _player.rigidbody.linearVelocity = new Vector3(slideVelocity.x, _player.rigidbody.linearVelocity.y, slideVelocity.z);
        }
        else
        {
            if (Input.GetKeyDown(activateKey) && _player.CanSlide)
            {
                if (_player.cameraType == PlayerController.CameraType.FirstPerson)
                {
                    _player.camera.transform.SetParent(_player.fpsCamera);
                }
                slideAnimSpeed = slideSpeed / _player.GetAnimationLength("Slide");
                _player.isSliding = true;

                // Modify collider instead of controller properties
                CapsuleCollider collider = _player.capsuleCollider;
                if (collider != null)
                {
                    collider.height /= 2;
                    collider.center = new Vector3(collider.center.x, collider.center.y / 2, collider.center.z);
                }

                slideDirection = _player.transform.forward; // Lock slide direction
                _player.animator.speed = slideAnimSpeed;
                _player.animator.SetTrigger("Slide");
                this.Invoke("StopSlide", slideDuration + 0.25f);
            }
        }
    }

    void StopSlide()
    {
        // Modify collider back instead of controller
        if(_player.cameraType == PlayerController.CameraType.FirstPerson)
        {
            _player.camera.transform.SetParent(_player.transform);
        }
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
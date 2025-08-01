using System.Threading.Tasks;
using UnityEngine;

public class Roll : PlayerExtension
{
    public KeyCode activateKey = KeyCode.Q;
    public float rollSpeed = 2f;
    public float rollDuration = 0.5f;
    public float cooldownTime = 1f;
    private float lastRollTime = 0f;
    private bool isReadyToRoll => Time.time >= lastRollTime + cooldownTime;
    private Vector3 rollDirection;
    private float rollAnimSpeed => rollSpeed / _player.GetAnimationLength("roll");
    private bool isRolling = false;
    private bool CanRoll=> _player.canMove && _player.isGrounded && _player.canApplyGravity && isReadyToRoll;
        
    
    protected override void OnUpdate()
    {
        if (isRolling)
        {
            Vector3 rollVelocity = (rollDirection * rollSpeed) * _player.Speed;
            _player.rigidbody.linearVelocity = new Vector3(rollVelocity.x, _player.rigidbody.linearVelocity.y, rollVelocity.z);
        }
        else
        {
            if (Input.GetKeyDown(activateKey) && CanRoll)
            {
                if (_player.cameraType == Player.CameraType.FirstPerson)
                {
                    _player.camera.transform.SetParent(_player.fpsCameraPivot);
                }
                isRolling = true;

                // Modify collider instead of controller properties
                CapsuleCollider collider = _player.capsuleCollider;
                if (collider != null)
                {
                    collider.height /= 2;
                    collider.center = new Vector3(collider.center.x, collider.center.y / 2, collider.center.z);
                }

                rollDirection = _player.transform.forward; // Lock roll direction
                _player.animator.speed = rollAnimSpeed;
                _player.animator.SetTrigger("roll");
                this.Invoke("Stoproll", rollDuration + 0.25f);
            }
        }
    }

    void Stoproll()
    {
        // Modify collider back instead of controller
        if (_player.cameraType == Player.CameraType.FirstPerson)
        {
            _player.camera.transform.SetParent(_player.transform);
        }
        CapsuleCollider collider = _player.capsuleCollider;
        if (collider != null)
        {
            collider.height *= 2;
            collider.center = new Vector3(collider.center.x, collider.center.y * 2, collider.center.z);
        }

        isRolling = false;
        _player.animator.speed = 1;
        lastRollTime = Time.time; // Reset the last roll time
    }
}
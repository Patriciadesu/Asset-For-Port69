using UnityEngine;
using NaughtyAttributes;

public class WallRun : PlayerExtension
{
    [Foldout("Wall Run Settings"), SerializeField, Range(0.5f, 10f)] private float wallRunSpeed = 7f;
    [Foldout("Wall Run Settings"), SerializeField, Range(0.1f, 2f)] private float wallRunSpeedAcceleration = 0.2f;
    [Foldout("Wall Run Settings"), SerializeField, Range(0f, 50f)] private float wallJumpForce = 12f;
    [Foldout("Wall Run Settings"), SerializeField, Range(0f, 50f)] private float wallJumpUpwardForce = 5f;
    [Foldout("Wall Run Settings"), SerializeField, Range(0f, 50f)] private float wallJumpDirectionForce = 8f;
    [Foldout("Wall Run Settings"), SerializeField, Range(0f, 90f)] private float maxWallRunAngle = 30f;
    [Foldout("Wall Run Settings"), SerializeField] private LayerMask wallLayer;

    [Foldout("Wall Run Duration"), SerializeField, Range(0.5f, 20f)] private float maxWallRunTime = 4f;
    [Foldout("Wall Run Duration"), SerializeField, Range(0f, 5f)] private float wallRunCooldown = 0.5f;

    [Foldout("Camera Effects"), SerializeField, Range(0f, 20f)] private float wallRunCameraTilt = 15f;
    [Foldout("Camera Effects"), SerializeField, Range(0.1f, 5f)] private float cameraTiltTime = 0.5f;

    private Vector3 wallNormal;
    private Vector3 wallForward;
    private float currentWallRunTime = 0f;
    private float lastWallRunTime = -10f;
    private float currentTilt = 0f;
    private int wallContactCount = 0;
    private Collision currentWallCollision;
    private float currentWallRunSpeed;
    private Vector3 lastWallVelocity;

    public override void OnStart(PlayerController player)
    {
        base.OnStart(player);
    }

    private void Update()
    {
        if (!_player) return;

        if (_player.isWallRunning)
        {
            WallRunningMovement();

            float targetTilt = wallNormal.x > 0 ? wallRunCameraTilt : -wallRunCameraTilt;
            currentTilt = Mathf.Lerp(currentTilt, targetTilt, Time.deltaTime / cameraTiltTime);
            _player.camera.transform.localRotation = Quaternion.Euler(
                _player.camera.transform.localRotation.eulerAngles.x,
                _player.camera.transform.localRotation.eulerAngles.y,
                currentTilt);

            if (Input.GetButtonDown("Jump"))
            {
                WallJump();
            }

            currentWallRunTime += Time.deltaTime;
            if (currentWallRunTime >= maxWallRunTime || _player.isGrounded)
            {
                StopWallRun();
            }
        }
        else
        {
            currentTilt = Mathf.Lerp(currentTilt, 0f, Time.deltaTime / cameraTiltTime);
            _player.camera.transform.localRotation = Quaternion.Euler(
                _player.camera.transform.localRotation.eulerAngles.x,
                _player.camera.transform.localRotation.eulerAngles.y,
                currentTilt);
        }

        if (_player.isWallRunning && wallContactCount <= 0)
        {
            StopWallRun();
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        HandleWallCollision(collision, true);
    }

    private void OnCollisionStay(Collision collision)
    {
        HandleWallCollision(collision, false);
    }

    private void OnCollisionExit(Collision collision)
    {
        if (_player.isWallRunning && currentWallCollision == collision)
        {
            wallContactCount--;
        }
    }

    private void HandleWallCollision(Collision collision, bool isEnter)
    {
        if (((1 << collision.gameObject.layer) & wallLayer) == 0) return;
        if (_player.isGrounded || Time.time < lastWallRunTime + wallRunCooldown) return;

        ContactPoint contact = collision.GetContact(0);
        Vector3 incomingNormal = contact.normal;
        float wallAngle = Vector3.Angle(incomingNormal, Vector3.up);

        if (wallAngle < 90f - maxWallRunAngle || wallAngle > 90f + maxWallRunAngle) return;

        Vector3 incomingForward = Vector3.Cross(incomingNormal, Vector3.up);
        Vector3 playerFacing = Vector3.ProjectOnPlane(_player.transform.forward, Vector3.up).normalized;
        if (Vector3.Dot(incomingForward, playerFacing) < 0) incomingForward = -incomingForward;

        // Check if this is a new wall face while wall running
        if (_player.isWallRunning && currentWallCollision != null)
        {
            float normalDot = Vector3.Dot(wallNormal, incomingNormal);
            if (normalDot < 0.95f) // Allow transition to perpendicular faces
            {
                TryTransitionToNewFace(collision, incomingNormal, incomingForward);
                return;
            }
            // If same wall face, just update contact count
            if (isEnter && collision.collider == currentWallCollision.collider)
            {
                wallContactCount++;
            }
            return;
        }

        // Initial wall contact
        wallNormal = incomingNormal;
        wallForward = incomingForward;

        if (isEnter)
        {
            wallContactCount++;
            currentWallCollision = collision;
        }

        if (!_player.isWallRunning)
        {
            StartWallRun();
        }
    }

    private void TryTransitionToNewFace(Collision newWall, Vector3 newNormal, Vector3 newForward)
    {
        Vector3 previousVelocity = _player.rigidbody.linearVelocity;

        // Check if this is part of the same object (cube)
        if (newWall.gameObject == currentWallCollision.gameObject)
        {
            // Smooth transition for cube faces
            wallNormal = Vector3.Lerp(wallNormal, newNormal, Time.deltaTime * 10f);
            wallForward = newForward;
            currentWallCollision = newWall;
            wallContactCount = Mathf.Max(wallContactCount, 1);

            // Project previous velocity onto new wall direction
            currentWallRunSpeed = Vector3.Project(previousVelocity, newForward).magnitude * 0.9f;
            currentWallRunSpeed = Mathf.Max(currentWallRunSpeed, wallRunSpeed);
            lastWallVelocity = previousVelocity;
            currentWallRunTime = Mathf.Min(currentWallRunTime, maxWallRunTime * 0.5f); // Extend wall run time slightly
        }
        else
        {
            // Different object transition
            wallNormal = newNormal;
            wallForward = newForward;
            currentWallCollision = newWall;
            wallContactCount = 1;
            currentWallRunSpeed = Mathf.Max(Vector3.Project(previousVelocity, newForward).magnitude * 0.85f, wallRunSpeed);
            lastWallVelocity = previousVelocity;
            currentWallRunTime = 0f;
        }

        _player.animator.SetBool("_player.isWallRunning", true);
    }

    private void StartWallRun()
    {
        if (_player.isWallRunning) return;

        _player.isWallRunning = true;
        currentWallRunTime = 0f;
        currentWallRunSpeed = Mathf.Max(_player.Speed, wallRunSpeed);
        _player.rigidbody.useGravity = true;
        lastWallVelocity = _player.rigidbody.linearVelocity;

        _player.animator.SetBool("_player.isWallRunning", true);
    }

    private void WallRunningMovement()
    {
        _player.rigidbody.useGravity = false;
        _player.rigidbody.AddForce(-wallNormal * 5f, ForceMode.Force);

        Vector3 playerFacing = Vector3.ProjectOnPlane(_player.transform.forward, Vector3.up).normalized;
        float facingAlignment = Vector3.Dot(playerFacing, wallForward);

        if (facingAlignment > 0.1f)
        {
            currentWallRunSpeed += wallRunSpeedAcceleration * Time.deltaTime * facingAlignment;
            currentWallRunSpeed = Mathf.Min(currentWallRunSpeed, wallRunSpeed * 1.5f);
        }
        else
        {
            currentWallRunSpeed = Mathf.Lerp(currentWallRunSpeed, wallRunSpeed, Time.deltaTime * 2f);
        }

        Vector3 targetVelocity = wallForward * currentWallRunSpeed;
        Vector3 currentVelocity = Vector3.Lerp(lastWallVelocity, targetVelocity, Time.deltaTime * 5f);
        _player.rigidbody.linearVelocity = new Vector3(
            currentVelocity.x,
            _player.rigidbody.linearVelocity.y,
            currentVelocity.z
        );
        lastWallVelocity = currentVelocity;
    }

    private void StopWallRun()
    {
        if (!_player.isWallRunning) return;

        _player.isWallRunning = false;
        lastWallRunTime = Time.time;
        wallContactCount = 0;
        currentWallCollision = null;

        _player.animator.SetBool("_player.isWallRunning", false);
    }

    private void WallJump()
    {
        if (!_player.isWallRunning) return;

        Vector3 playerFacing = Vector3.ProjectOnPlane(_player.transform.forward, Vector3.up).normalized;
        Vector3 jumpDirection = (wallNormal + playerFacing + Vector3.up).normalized;

        _player.rigidbody.linearVelocity = Vector3.zero;
        _player.rigidbody.AddForce(wallNormal * wallJumpDirectionForce, ForceMode.Impulse);
        _player.rigidbody.AddForce(Vector3.up * wallJumpUpwardForce, ForceMode.Impulse);
        _player.rigidbody.AddForce(playerFacing * wallJumpForce, ForceMode.Impulse);

        _player.animator.SetTrigger("jump");
        StopWallRun();
    }

    private void OnDisable()
    {
        if (_player != null && _player.camera != null)
        {
            _player.camera.transform.localRotation = Quaternion.Euler(
                _player.camera.transform.localRotation.eulerAngles.x,
                _player.camera.transform.localRotation.eulerAngles.y,
                0f);
        }

        StopWallRun();
    }

    private void OnDrawGizmosSelected()
    {
        if (!Application.isPlaying || !_player.isWallRunning) return;

        Debug.DrawRay(transform.position, wallNormal * 2f, Color.blue);
        Debug.DrawRay(transform.position, wallForward * 2f, Color.green);
    }
}
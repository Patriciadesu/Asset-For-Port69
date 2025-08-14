using UnityEngine;

public class WallRun : PlayerExtension,ICancleGravity
{
    [Header("UI")]
    public bool enableWallRunUI = true;
    private PlayerUIManager uiManager;

    [Header("Properties")]
    public float wallRideSpeed = 4f;
    public float wallJumpForce = 20f;
    private GameObject currentWall;
    private Vector3 direction;
    private Vector3 wallNormal;
    private bool isWallRunning;
    public bool canApplyGravity { get; set; } = true;

    public override void OnStart(Player player)
    {
        base.OnStart(player);
        if (enableWallRunUI)
            uiManager = Object.FindAnyObjectByType<PlayerUIManager>();
    }

    protected void Update()
    {
        if (isWallRunning)
        {
            WallRide();
        }
        if (enableWallRunUI && uiManager != null)
            uiManager.UpdateWallRun(isWallRunning);
    }

    protected void OnCollisionStay(Collision collision)
    {
        foreach (ContactPoint point in collision.contacts)
        {
            if (IsWall(point) && !isWallRunning && Input.GetButton("Jump"))
            {
                currentWall = collision.gameObject;
                StartWallRide(point);
            }
        }
    }

    protected void OnCollisionExit(Collision collision)
    {
        if (collision.gameObject == currentWall)
        {
            currentWall = null;
            EndWallRide();
        }
    }

    private void StartWallRide(ContactPoint point)
    {
        isWallRunning = true;
        direction = GetWallParallelDirection(point.normal);
        wallNormal = point.normal;
        _player.canMove = false;
        canApplyGravity = false;
        _player.OnUpdate -= _player.JumpHandler;
        _player.animator.SetBool("isWallRiding", true);
        float side = Vector3.Dot(_player.transform.right, wallNormal);
        _player.animator.SetTrigger(side < 0 ? "isWallRiding_R" : "isWallRiding_L");
    }

    private void WallRide()
    {
        _player.rigidbody.MovePosition(_player.rigidbody.position + direction * wallRideSpeed * Time.fixedDeltaTime);
        if (Input.GetButtonUp("Jump") || _player.isGrounded)
        {
            if (Input.GetButtonUp("Jump"))
            {
                WallJump();
            }
            else
            {
                EndWallRide();
            }
        }
    }

    private void WallJump()
    {
        Vector3 jumpDirection = wallNormal + Vector3.up;
        _player.rigidbody.linearVelocity = Vector3.zero;
        _player.rigidbody.AddForce(jumpDirection.normalized * wallJumpForce, ForceMode.Impulse);
        _player.animator.SetTrigger("jump");
        EndWallRide();
    }

    private void EndWallRide()
    {
        if (isWallRunning)
        {
            isWallRunning = false;
            _player.canMove = true;
            canApplyGravity = true;
            _player.OnUpdate += _player.JumpHandler;
            _player.animator.SetBool("isWallRiding", false);
        }
    }

    private bool IsWall(ContactPoint point)
    {
        return Mathf.Abs(90f - Vector3.Angle(Vector3.up, point.normal)) < 0.1f;
    }

    private Vector3 GetWallParallelDirection(Vector3 wallNormal)
    {
        Vector3 forward = _player.transform.forward;
        Vector3 projected = Vector3.ProjectOnPlane(forward, wallNormal);
        return projected.normalized;
    }
}
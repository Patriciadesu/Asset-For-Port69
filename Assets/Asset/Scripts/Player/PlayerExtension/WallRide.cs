using UnityEngine;
using NaughtyAttributes;
using System;
public class WallRun : PlayerExtension
{
    public float wallRideSpeed;
    public float wallJumpForce;
    private GameObject currentWall;
    private Vector3 direction;
    private Vector3 wallNormal;

    public override void OnStart(PlayerController player)
    {
        base.OnStart(player);
    }

    private void Update()
    {
        if (_player.isWallRunning)
        {
            WallRide();
        }
    }

    private void OnCollisionStay(Collision collision)
    {
        foreach (ContactPoint point in collision.contacts)
        {
            if (IsWall(point) && 
                !_player.isWallRunning && 
                _player.CanRideWall && 
                Input.GetButton("Jump"))
            {
                currentWall = collision.gameObject;
                StartWallRide(point);
            }
        }
    }
    private void OnCollisionExit(Collision collision)
    {
        if (collision.gameObject == currentWall)
        {
            currentWall = null;
            EndWallRide();
        }
    }
    private void StartWallRide(ContactPoint point)
    {
         _player.isWallRunning = true;
        direction = GetWallParallelDirection(point.normal);
        wallNormal = point.normal;

    }
    private void WallRide()
    {
        _player.rigidbody.MovePosition(_player.rigidbody.position + direction * wallRideSpeed * Time.fixedDeltaTime);
        if (Input.GetButtonUp("Jump"))
        {
            WallJump();
        }

        if (_player.isGrounded)
        {
            EndWallRide();
        }

    }
    private void WallJump()
    {
        Vector3 jumpDirection = wallNormal + Vector3.up; // Away from wall + upward
        _player.rigidbody.linearVelocity = Vector3.zero; // Optional: reset vertical/horizontal speed
        _player.rigidbody.AddForce(jumpDirection.normalized * wallJumpForce, ForceMode.Impulse);

        EndWallRide();
    }

    private void EndWallRide()
    {
        if (_player.isWallRunning)
        {
            _player.isWallRunning = false;
        }
    }
    private bool IsWall(ContactPoint point)
    {
        return Math.Abs(90f - Vector3.Angle(Vector3.up, point.normal)) < 0.1f;
    }
    private Vector3 GetWallParallelDirection(Vector3 wallNormal)
    {
        // Remove the component of player's forward that's perpendicular to the wall
        Vector3 forward = _player.transform.forward;
        Vector3 projected = Vector3.ProjectOnPlane(forward, wallNormal);
        return projected.normalized;
    }
}
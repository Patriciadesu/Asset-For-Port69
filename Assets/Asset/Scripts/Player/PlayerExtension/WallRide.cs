using UnityEngine;
using NaughtyAttributes;
using System;
using UnityEngine.Rendering;
using UnityEngine.Timeline;
using Unity.VisualScripting;
public class WallRun : PlayerExtension
{
    public float wallRideSpeed = 4;
    public float wallJumpForce = 20;
    private GameObject currentWall;
    private Vector3 direction;
    private Vector3 wallNormal;
    public bool isWallRunning;


    protected override void OnUpdate()
    {
        if (isWallRunning)
        {
            WallRide();
        }
    }

    protected override void OnCollisionStayEvent(Collision collision)
    {
        foreach (ContactPoint point in collision.contacts)
        {
            if (IsWall(point) && 
                !isWallRunning && 
                Input.GetButton("Jump"))
            {
                currentWall = collision.gameObject;
                StartWallRide(point);
            }
        }
    }
    protected override void OnCollisionExitEvent(Collision collision)
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
        _player.canApplyGravity = false;
        _player.OnUpdate -= _player.JumpHandler;
        _player.animator.SetBool("isWallRiding", true);
        float side = Vector3.Dot(_player.transform.right, wallNormal);
        if (side < 0)
        {
            _player.animator.SetTrigger("isWallRiding_R");
        }
        else
        {
            _player.animator.SetTrigger("isWallRiding_L");
        }
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
        _player.animator.SetTrigger("jump");
        EndWallRide();
    }

    private void EndWallRide()
    {
        //_player.camera.transform.Rotate(0, 0, -30);
        if (isWallRunning)
        {
            _player.canMove = true;
            _player.canApplyGravity = true;
            _player.OnUpdate += _player.JumpHandler;
            _player.animator.SetBool("isWallRiding", false);
            isWallRunning = false;
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
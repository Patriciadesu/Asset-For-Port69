using NaughtyAttributes;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[ExecuteAlways]
public class PlayerController : MonoBehaviour
{
    #region Properties

    // Enums
    public enum CameraType
    {
        FirstPerson,
        ThirdPerson
    }

    // Components
    [HideInInspector] public Rigidbody rigidbody => GetComponent<Rigidbody>();
    [HideInInspector] public Animator animator => GetComponent<Animator>();
    [HideInInspector] public CapsuleCollider capsuleCollider => GetComponent<CapsuleCollider>();

    // Capsule Info
    private float capsuleHeight => capsuleCollider.height;
    private float capsuleRadius => capsuleCollider.radius;

    // Camera Settings
    [Foldout("Camera", true)] public CameraType cameraType;
    [Foldout("Camera", true), SerializeField, Range(30, 120)] float cameraFOV = 60f;
    [Foldout("Camera", true), ShowIf("cameraType", CameraType.ThirdPerson), Range(-2, 2)] public float cameraOffsetX = 0f;
    [Foldout("Camera", true), ShowIf("cameraType", CameraType.ThirdPerson), Range(-2, 2)] public float cameraOffsetY = 0.5f;
    [Foldout("Camera", true), ShowIf("cameraType", CameraType.ThirdPerson), Range(0, 2)] public float cameraLookUp = 0.5f;
    [Foldout("Camera", true), ShowIf("cameraType", CameraType.ThirdPerson), Range(1, 10)] public float cameraDistance = 4f;
    [Foldout("Camera", true), ShowIf("cameraType", CameraType.ThirdPerson), Range(0, 1)] public float cameraSmoothness = 0.1f;
    [Foldout("Camera", true), ShowIf("cameraType", CameraType.ThirdPerson)] public bool freeLookCamera = true;

    // Movement Settings
    public float Speed => (speed + additionalSpeed) * speedMultiplier;
    [Foldout("Movement Settings", true), SerializeField] private float speed = 5f;
    [Foldout("Movement Settings", true)] public float jumpForce = 10f;
    [Foldout("Movement Settings", true)] public float fallMultiplier = 3f;
    [Foldout("Movement Settings", true)] public float gravityMultiplier = 2.5f;
    [HideInInspector] public float speedMultiplier = 1f;
    [HideInInspector] public float additionalSpeed = 0;

    // Movement Timers
    private float coyoteTime = 0.01f;
    private float jumpBufferTime = 0.05f;
    private float lastGroundedTime;
    private float lastJumpPressedTime;

    // Ground Check
    private float groundCheckDistance = 0.5f;

    // Mouse Look Settings
    [Foldout("Mouse Look Settings", true)] public float mouseSensitivity = 2f;

    [Foldout("DO NOT TOUCH")] public Camera camera;
    [Foldout("DO NOT TOUCH")] public Transform fpsCamera;
    [Foldout("DO NOT TOUCH")] public Transform tpsCamera;
    [Foldout("DO NOT TOUCH")] public Transform tpsCameraPivot;

    // Player States
    [HideInInspector] public bool isSliding = false;
    [HideInInspector] public bool isGrounded = true;
    [HideInInspector] public bool isCrouching = false;
    [HideInInspector] public bool isWallRunning = false;
    public bool CanSlide => isGrounded && !isWallRunning;
    public bool CanJump => isGrounded && !isWallRunning;
    public bool CanMove => !isWallRunning && !isSliding;
    public bool CanCrouch => !isWallRunning && !isSliding;
    public bool CanRideWall => !isGrounded && !isSliding;

    // Internal State
    [HideInInspector] public Vector3 velocity;
    [HideInInspector] public Vector3 lastCheckpoint;
    [HideInInspector] public Vector3 spawnPoint;
    private float xRotation = 0f;
    private float tpsYaw = 0f;
    private float tpsPitch = 10f;
    private Vector3 smoothCameraVelocity;

    // Extensions
    private PlayerExtension[] extensions;

    #endregion

    #region Unity Methods

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        SetUpCamera();
        spawnPoint = transform.position;
        RefreshExtension();
        foreach (var extension in extensions)
        {
            extension.OnStart(this);
        }
        if(cameraType == CameraType.ThirdPerson&&freeLookCamera) camera.transform.parent = null;
    }

    private void FixedUpdate()
    {
        if (Application.isPlaying)
        {
            Move();
            ApplyGravity();
        }
    }

    void Update()
    {
        if (Application.isPlaying)
        {
            CheckGrounded();
            JumpHandler();
            HandleMouseLook();
            UpdateCameraPosition();
            if (isGrounded)
            {
                lastGroundedTime = Time.time;
            }
            if (cameraType == CameraType.FirstPerson)
            {
                camera.transform.position = fpsCamera.transform.position;
            }
        }
        else
        {
            SetUpCamera();
            foreach (GameObject cam in GameObject.FindGameObjectsWithTag("MainCamera"))
            {
                if (cam != camera.gameObject)
                {
                    DestroyImmediate(cam);
                }
            }
        }
    }

    #endregion

    #region User Defined Methods

    void ApplyGravity()
    {
        float _fallMultiplier = isWallRunning ? 1 : fallMultiplier;
        Vector3 velocity = rigidbody.linearVelocity;

        // Apply gravity modifiers
        if (velocity.y <= 0)
        {
            velocity += Vector3.up * Physics.gravity.y * (_fallMultiplier - 1) * Time.deltaTime;
        }
        else if (velocity.y > 0 && !Input.GetButton("Jump"))
        {
            velocity += Vector3.up * Physics.gravity.y * (gravityMultiplier - 1) * Time.deltaTime;
        }

        // Stick to ground slightly when grounded
        if (isGrounded && velocity.y < 0)
        {
            velocity.y = 0f;
        }

        rigidbody.linearVelocity = velocity;
    }

    void SetUpCamera()
    {
        switch (cameraType)
        {
            case CameraType.FirstPerson:
                camera.transform.position = fpsCamera.position;
                camera.transform.rotation = Quaternion.Euler(transform.forward);
                break;
            case CameraType.ThirdPerson:
                tpsCameraPivot.transform.localPosition = new Vector3(0, cameraLookUp, 0);
                camera.fieldOfView = cameraFOV;
                break;
        }
    }

    void Move()
    {
        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");

        if (CanMove)
        {
            // Calculate movement direction relative to camera
            Vector3 inputDir = new Vector3(horizontal, 0, vertical).normalized;
            Vector3 moveDir = Quaternion.Euler(0, tpsYaw, 0) * inputDir;

            // Set horizontal velocity (preserve vertical velocity)
            Vector3 targetVelocity = moveDir * Speed;
            Vector3 newVelocity = Vector3.Lerp(
                new Vector3(rigidbody.linearVelocity.x, 0, rigidbody.linearVelocity.z),
                targetVelocity,
                10f * Time.fixedDeltaTime
            );
            rigidbody.linearVelocity = new Vector3(newVelocity.x, rigidbody.linearVelocity.y, newVelocity.z);

            // Rotate player to face movement direction if moving
            if (inputDir.magnitude > 0)
            {
                float targetAngle = Mathf.Atan2(moveDir.x, moveDir.z) * Mathf.Rad2Deg;
                transform.rotation = Quaternion.Slerp(
                    transform.rotation,
                    Quaternion.Euler(0, targetAngle, 0),
                    10f * Time.fixedDeltaTime
                );
            }

            animator.SetFloat("MoveX", horizontal);
            animator.SetFloat("MoveY", vertical);
            animator.SetBool("isRun", horizontal != 0 || vertical != 0);
        }
        else
        {
            // Stop horizontal movement if cannot move
            rigidbody.linearVelocity = new Vector3(0, rigidbody.linearVelocity.y, 0);
        }
    }

    void JumpHandler()
    {
        if (Input.GetButtonDown("Jump") && CanJump)
        {
            lastJumpPressedTime = Time.time;
        }
        if (Time.time - lastJumpPressedTime <= jumpBufferTime && Time.time - lastGroundedTime <= coyoteTime)
        {
            Jump();
            lastJumpPressedTime = -999f;
        }
    }

    public void Jump()
    {
        animator.SetTrigger("jump");
        rigidbody.linearVelocity = new Vector3(rigidbody.linearVelocity.x, 0f, rigidbody.linearVelocity.z);
        rigidbody.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
    }

    void HandleMouseLook()
    {
        if (cameraType == CameraType.ThirdPerson && !Input.GetMouseButton(1)) return;

        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

        if (cameraType == CameraType.FirstPerson)
        {
            transform.Rotate(Vector3.up * mouseX);
            xRotation -= mouseY;
            xRotation = Mathf.Clamp(xRotation, -90f, 90f);
            camera.transform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
        }
        else if (cameraType == CameraType.ThirdPerson)
        {
            tpsYaw += mouseX;
            tpsPitch -= mouseY;
            tpsPitch = Mathf.Clamp(tpsPitch, -20f, 60f);
        }
        
    }

    void UpdateCameraPosition()
    {
        if (cameraType != CameraType.ThirdPerson) return;

        // Calculate camera rotation
        Quaternion camRotation = Quaternion.Euler(tpsPitch, tpsYaw, 0);

        // Calculate look-at target
        Vector3 lookAtPoint = tpsCameraPivot.position;
        Vector3 pivotOffset = new Vector3(cameraOffsetX, cameraOffsetY + cameraLookUp, 0);

        // Raycast from pivot + offset
        Vector3 rayOrigin = lookAtPoint + pivotOffset;
        Vector3 desiredDirection = camRotation * Vector3.back; // back relative to rotation
        Vector3 desiredPosition = rayOrigin + desiredDirection * cameraDistance;

        // Check for obstructions
        float targetDistance = cameraDistance;
        if (Physics.Raycast(rayOrigin, desiredDirection, out RaycastHit hit, cameraDistance, ~LayerMask.GetMask("Player")))
        {
            targetDistance = hit.distance - 0.1f; // Tiny buffer
            targetDistance = Mathf.Clamp(targetDistance, 0.5f, cameraDistance); // Avoid too close
        }

        // Set camera position instantly (no smoothing)
        Vector3 finalPosition = rayOrigin + desiredDirection * targetDistance;
        camera.transform.position = finalPosition;

        // Orient camera to look at pivot
        camera.transform.LookAt(lookAtPoint);
    }



    void CheckGrounded()
    {
        Vector3 center = transform.position + capsuleCollider.center;
        float radius = capsuleCollider.radius * 0.95f;
        float height = capsuleCollider.height * 0.5f - radius;

        Vector3 point1 = center + Vector3.up * height;
        Vector3 point2 = center - Vector3.up * height;

        Vector3 direction = Vector3.down;
        float distance = 0.2f;

        RaycastHit[] hits = Physics.CapsuleCastAll(
            point1, point2, radius, direction, distance,
            ~0,
            QueryTriggerInteraction.Ignore
        );

        isGrounded = false;

        foreach (RaycastHit hit in hits)
        {
            if (hit.collider != capsuleCollider)
            {
                isGrounded = true;
                break;
            }
        }
    }

    public void Respawn()
    {
        rigidbody.linearVelocity = Vector3.zero;
        Debug.Log("Respawning");
        if (lastCheckpoint == Vector3.zero)
        {
            Debug.Log("Last Checkpoint is null");
            this.transform.position = spawnPoint;
        }
        else this.transform.position = lastCheckpoint;
    }

    public float GetAnimationLength(string animationName)
    {
        foreach (AnimationClip clip in animator.runtimeAnimatorController.animationClips)
        {
            if (clip.name == animationName)
            {
                return clip.length;
            }
        }
        return 0f;
    }

    public void RefreshExtension()
    {
        extensions = GetComponents<PlayerExtension>();
    }

    public void JumpToPosition(Vector3 targetPosition, float arcHeight)
    {
        rigidbody.linearVelocity = Vector3.zero;
        Vector3 direction = (targetPosition - transform.position).normalized;
        float distance = Vector3.Distance(transform.position, targetPosition);
        float speed = Speed * 2f;
        float timeToTarget = distance / speed;
        float gravity = Physics.gravity.y * gravityMultiplier;
        float verticalVelocity = (arcHeight - 0.5f * gravity * timeToTarget * timeToTarget) / timeToTarget;
        rigidbody.AddForce(direction * speed + Vector3.up * verticalVelocity, ForceMode.VelocityChange);
        animator.SetTrigger("jump");
    }

    #endregion

    #region Gizmos

    void OnDrawGizmosSelected()
    {
        Vector3 start = transform.position + capsuleCollider.center;
        float radius = capsuleRadius * 0.95f;
        float height = capsuleHeight * 0.5f - radius;

        Vector3 point1 = start + Vector3.up * height;
        Vector3 point2 = start - Vector3.up * height - Vector3.up * groundCheckDistance;

        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(point1 - Vector3.up * groundCheckDistance, radius);
        Gizmos.DrawWireSphere(point2, radius);
        Gizmos.DrawLine(point1 - Vector3.up * groundCheckDistance + Vector3.left * radius, point2 + Vector3.left * radius);
        Gizmos.DrawLine(point1 - Vector3.up * groundCheckDistance + Vector3.right * radius, point2 + Vector3.right * radius);
    }

    #endregion
}
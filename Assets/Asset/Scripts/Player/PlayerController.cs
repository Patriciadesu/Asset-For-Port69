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
    [Foldout("Camera", true), SerializeField, Range(30, 120)] float cameraFOV;
    [Foldout("Camera", true), ShowIf("cameraType", CameraType.ThirdPerson), Range(-2, 2)] public float cameraOffsetX;
    [Foldout("Camera", true), ShowIf("cameraType", CameraType.ThirdPerson), Range(-2, 2)] public float cameraOffsetY;
    [Foldout("Camera", true), ShowIf("cameraType", CameraType.ThirdPerson), Range(0, 2)] public float cameraLookUp;




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
    public bool CanSlide
    {
        get
        {
            List<bool> states = new List<bool>()
            {
                isGrounded,
                !isWallRunning
            };
            return states.All(x => x == true);
        }
    }
    public bool CanJump
    {
        get
        {
            List<bool> states = new List<bool>()
            {
                isGrounded,
                !isWallRunning,
            };
            return states.All(x => x == true);
        }
    }
    public bool CanMove
    {
        get
        {
            List<bool> states = new List<bool>()
            {
                !isWallRunning,
                !isSliding
            };
            return states.All(x => x == true);
        }
    }
    public bool CanCrouch
    {
        get
        {
            List<bool> states = new List<bool>()
            {
                ! isWallRunning,
                ! isSliding
            };
            return states.All(x => x == true);
        }
    }
    public bool CanRideWall
    {
        get
        {
            List<bool> states = new List<bool>()
            {
                !isGrounded,
                !isSliding
            };
            return states.All(x => x == true);
        }
    }

    // Internal State
    [HideInInspector] public Vector3 velocity;
    [HideInInspector] public Vector3 lastCheckpoint;
    [HideInInspector] public Vector3 spawnPoint;
    private float xRotation = 0f;
    private float tpsYaw = 0f;
    private float tpsPitch = 10f;


    // Extensions
    private PlayerExtension[] extensions;

    #endregion

    #region Unity Method

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

    #region User Define Method
    //Done
    void ApplyGravity()
    {
        float _fallMultiplier = isWallRunning ? 1 : fallMultiplier;
        if (rigidbody.linearVelocity.y <= 0)



        {
            rigidbody.linearVelocity += Vector3.up * Physics.gravity.y * (_fallMultiplier - 1) * Time.deltaTime;
        }
        else if (rigidbody.linearVelocity.y > 0 && !Input.GetButton("Jump"))
        {
            rigidbody.linearVelocity += Vector3.up * Physics.gravity.y * (gravityMultiplier - 1) * Time.deltaTime;
        }
        if (isGrounded && rigidbody.linearVelocity.y < 0)


        {
            velocity.y = -2f;
        }


    }
    //Done
    void SetUpCamera()
    {
        switch (cameraType)
        {
            case CameraType.FirstPerson:
                camera.transform.position = fpsCamera.position;
                camera.transform.rotation = Quaternion.Euler(transform.forward);
                break;
            case CameraType.ThirdPerson:
                tpsCamera.transform.localPosition = new Vector3(0, 3.5f + cameraOffsetY, -3 + cameraOffsetX);
                camera.transform.position = tpsCamera.position;
                camera.transform.LookAt(tpsCameraPivot);
                tpsCameraPivot.transform.localPosition = new Vector3(0, cameraLookUp, 0);

                break;
        }
        camera.fieldOfView = cameraFOV;
    }
    //Done
    void Move()
    {
        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");

        Vector3 move;
        if (CanMove)
        {
            move = (transform.right * horizontal + transform.forward * vertical).normalized;
            rigidbody.MovePosition(rigidbody.position + move * Speed * Time.fixedDeltaTime);
            animator.SetFloat("MoveX", horizontal);
            animator.SetFloat("MoveY", vertical);
            animator.SetBool("isRun", horizontal != 0 || vertical != 0);
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
            lastJumpPressedTime = -999f; // Reset to prevent double fire
        }
    }
    //Done
    public void Jump()
    {
        animator.SetTrigger("jump");
        rigidbody.linearVelocity = new Vector3(rigidbody.linearVelocity.x, 0f, rigidbody.linearVelocity.z);
        rigidbody.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
    }
    //Done
    void HandleMouseLook()
    {


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
            tpsCameraPivot.rotation = Quaternion.Euler(tpsPitch, tpsYaw, 0f);
            transform.rotation = Quaternion.Euler(0f, tpsYaw, 0f);
        }

    }
    //Done


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
            ~0, // Everything
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
    //Done
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
    //Done
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
    //Done
    public void RefreshExtension()
    {
        extensions = GetComponents<PlayerExtension>();
    }

    public void JumpToPosition(Vector3 targetPosition, float arcHeight)
    {
        // Cancel current velocity
        rigidbody.linearVelocity = Vector3.zero;

        // Calculate the direction and distance
        Vector3 direction = (targetPosition - transform.position).normalized;
        float distance = Vector3.Distance(transform.position, targetPosition);

        // Estimate time to reach target (simplified)
        float speed = Speed * 2f; // Use player speed or adjust as needed
        float timeToTarget = distance / speed;

        // Apply an upward impulse for the arc
        float gravity = Physics.gravity.y * gravityMultiplier;
        float verticalVelocity = (arcHeight - 0.5f * gravity * timeToTarget * timeToTarget) / timeToTarget;

        // Apply force
        rigidbody.AddForce(direction * speed + Vector3.up * verticalVelocity, ForceMode.VelocityChange);

        // Update animator if needed
        animator.SetTrigger("jump");
    }

    #endregion

    #region Gizmos
    //Done
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
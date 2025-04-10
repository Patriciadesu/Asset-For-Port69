using NaughtyAttributes;
using System.Text.RegularExpressions;
using UnityEngine;

[ExecuteAlways]
public class PlayerController : MonoBehaviour
{
    public enum CameraType
    {
        FirstPerson,
        ThirdPerson
    }
    [HideInInspector] public Rigidbody rigidbody => GetComponent<Rigidbody>();
    [HideInInspector] public Animator animator => GetComponent<Animator>();
    [HideInInspector] public CapsuleCollider capsuleCollider => GetComponent<CapsuleCollider>();
    private float capsuleHeight => capsuleCollider.height;
    private float capsuleRadius => capsuleCollider.radius;

    [Header("Camera")]
    [SerializeField] CameraType cameraType;
    [SerializeField,Range(30, 120)] float cameraFOV;
    [Range(-2, 2)] public float cameraOffsetX;
    [Range(-2, 2)] public float cameraOffsetY;
    [Range(0, 2)] public float cameraLookUp;

    [Header("Movement Settings")]
    public float Speed => (speed+additionalSpeed) * speedMultiplier;

    [SerializeField] private float speed = 5f;
    [HideInInspector] public float speedMultiplier = 1f;
    [HideInInspector] public float additionalSpeed = 0;
    public float jumpForce = 10f;
    public float fallMultiplier = 3f;
    public float gravityMultiplier = 2.5f;
    private float coyoteTime = 0.01f;
    private float jumpBufferTime = 0.05f;
    private float lastGroundedTime;
    private float lastJumpPressedTime;

    [Header("Mouse Look Settings")]
    public float mouseSensitivity = 2f;

    [Header("DO NOT TOUCH")]
    [HideInInspector] public Camera camera;
    [HideInInspector] public Transform fpsCamera;
    [HideInInspector] public Transform tpsCamera;
    [HideInInspector] public Transform tpsCameraPivot;

    [HideInInspector] public Vector3 velocity;
    [HideInInspector] public Vector3 lastCheckpoint;
    [HideInInspector] public Vector3 spawnPoint;
    private float xRotation = 0f;
    private float tpsYaw = 0f;
    private float tpsPitch = 10f;
    private PlayerExtension[] extensions;
    public float groundCheckDistance = 0.1f;
    // Player States
    [HideInInspector] public bool isSliding = false;
    [HideInInspector]public bool isGrounded = true;
    [HideInInspector] public bool isReflecting = false;
    [HideInInspector] public bool isCrouching = false;

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        SetUpCamera();
        //rigidbody = GetComponent<Rigidbody>();
        //animator = GetComponent<Animator>();
        if (cameraType == CameraType.FirstPerson)
        {
            camera.transform.SetParent(fpsCamera);
            camera.transform.localPosition = Vector3.zero;
        }
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
            if (isGrounded)
            {
                lastGroundedTime = Time.time;
            }
            Move();
            ApplyGravity();
            CheckGrounded();
        }
    }
    void Update()
    {
        if (Application.isPlaying)
        {
            HandleMouseLook();
            foreach (var extension in extensions)
            {
                extension.OnUpdate(this);
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
    void OnCollisionEnter(Collision collision)
    {
        foreach (var extension in extensions)
        {
            extension.OnEnterCollision(this);
        }
    }
    void OnCollisionStay(Collision collision)
    {
        foreach (var extension in extensions)
        {
            extension.OnStayCollision(this);
        }
    }
    private void OnCollisionExit(Collision collision)
    {
        foreach (var extension in extensions)
        {
            extension.OnExitCollision(this);
        }
    }
    public void OnTriggerEnter(Collider other)
    {
        foreach (var extension in extensions)
        {
            extension.OnEnterTrigger(this);
        }
    }
    public void OnTriggerStay(Collider other)
    {
        foreach (var extension in extensions)
        {
            extension.OnStayTrigger(this);
        }
    }
    public void OnTriggerExit(Collider other)
    {
        foreach (var extension in extensions)
        {
            extension.OnExitTrigger(this);
        }
    }

    //Done
    void ApplyGravity()
    {
        if (rigidbody.linearVelocity.y <= 0)
        {
            rigidbody.linearVelocity += Vector3.up * Physics.gravity.y * (fallMultiplier - 1) * Time.deltaTime;
        }
        else if (rigidbody.linearVelocity.y > 0 && !Input.GetButton("Jump"))
        {
            rigidbody.linearVelocity += Vector3.up * Physics.gravity.y * (gravityMultiplier - 1) * Time.deltaTime;
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
        if (isGrounded && rigidbody.linearVelocity.y < 0)
        {
            velocity.y = -2f;
        }

        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");

        Vector3 move;
        if (!isSliding)
        {
            move = (transform.right * horizontal + transform.forward * vertical).normalized;
            rigidbody.MovePosition(rigidbody.position + move * Speed* Time.fixedDeltaTime);
        }
        if (Input.GetButtonDown("Jump"))
        {
            lastJumpPressedTime = Time.time;
        }
        if (Time.time - lastJumpPressedTime <= jumpBufferTime && Time.time - lastGroundedTime <= coyoteTime)
        {
            Jump();
            lastJumpPressedTime = -999f; // Reset to prevent double fire
        }
        if (!isSliding)
        {
            animator.SetFloat("MoveX", horizontal);
            animator.SetFloat("MoveY", vertical);
            animator.SetBool("isRun", horizontal != 0 || vertical != 0);
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
}
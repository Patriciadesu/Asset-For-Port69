using UnityEngine;

[ExecuteAlways]
public class PlayerController : MonoBehaviour
{
    public enum CameraType
    {
        FirstPerson,
        ThirdPerson
    }
    [HideInInspector] public Rigidbody rb;
    [HideInInspector] public Animator anim;

    [SerializeField] CameraType cameraType;
    [HideInInspector] public bool cameraShake = true;
    [Range(30, 90)] public float cameraFOV;
    [HideInInspector][Range(-2, 2)] public float cameraOffsetX;
    [HideInInspector][Range(-2, 2)] public float cameraOffsetY;
    [HideInInspector][Range(0, 2)] public float cameraLookUp;

    [Header("Movement Settings")]
    public float speed = 5f;
    [HideInInspector] public float initialSpeed;
    public float jumpHeight = 1.5f;
    public float gravity = -9.81f;
    public float speedMultiplier = 1f;

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

    [SerializeReference] public PlayerExtension[] extensions;

    // Player States
    [HideInInspector] public bool isSliding = false;
    [HideInInspector] public bool isGrounded = true;
    [HideInInspector] public bool isReflecting = false;
    [HideInInspector] public bool isCrouching = false;

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        SetUpCamera();
        rb = GetComponent<Rigidbody>();
        anim = GetComponent<Animator>();
        initialSpeed = speed;
        speedMultiplier = 1f; // Initialize to no effect
        if (cameraType == CameraType.FirstPerson)
        {
            if (cameraShake)
            {
                camera.transform.SetParent(fpsCamera);
                camera.transform.localPosition = Vector3.zero;
            }
        }
        spawnPoint = transform.position;
    }

    void FixedUpdate()
    {
        if (Application.isPlaying)
        {
            HandleMovement();
            HandleMouseLook();
            if (!cameraShake && cameraType == CameraType.FirstPerson)
            {
                camera.transform.position = fpsCamera.position;
            }
            foreach (var extension in extensions)
            {
                extension.Apply(this);
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

    void HandleMovement()
    {
        isGrounded = Physics.Raycast(transform.position, Vector3.down, 1.1f);
        if (isGrounded && rb.linearVelocity.y < 0)
        {
            velocity.y = -2f;
        }

        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");

        Vector3 move;
        if (!isSliding)
        {
            move = (transform.right * horizontal + transform.forward * vertical).normalized;
            // Apply speedMultiplier to the movement speed
            rb.MovePosition(rb.position + move * speed * speedMultiplier * Time.fixedDeltaTime);
        }

        if (Input.GetButtonDown("Jump") && isGrounded)
        {
            Jump();
        }

        velocity.y += gravity * Time.fixedDeltaTime;
        rb.AddForce(velocity * Time.fixedDeltaTime, ForceMode.VelocityChange);

        if (!isSliding)
        {
            anim.SetFloat("MoveX", horizontal);
            anim.SetFloat("MoveY", vertical);
            anim.SetBool("isRun", horizontal != 0 || vertical != 0);
        }
    }

    public void Jump()
    {
        anim.SetTrigger("jump");
        rb.linearVelocity = new Vector3(rb.linearVelocity.x, Mathf.Sqrt(jumpHeight * -2f * gravity), rb.linearVelocity.z);
    }

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
                tpsCameraPivot.transform.position = new Vector3(tpsCameraPivot.transform.position.x, cameraLookUp, tpsCameraPivot.transform.position.z);
                break;
        }
        camera.fieldOfView = cameraFOV;
    }

    void OnCollisionEnter(Collision collision)
    {
        Debug.Log("Collided with: " + collision.gameObject.name);
    }

    void OnCollisionStay(Collision collision)
    {
    }

    public float GetAnimationLength(string animationName)
    {
        foreach (AnimationClip clip in anim.runtimeAnimatorController.animationClips)
        {
            if (clip.name == animationName)
            {
                return clip.length;
            }
        }
        return 0f;
    }

    public void Respawn()
    {
        rb.linearVelocity = Vector3.zero;
        Debug.Log("Respawning");
        if (lastCheckpoint == Vector3.zero)
        {
            Debug.Log("Last Checkpoint is null");
            this.transform.position = spawnPoint;
        }
        else this.transform.position = lastCheckpoint;
    }

    public void OnTriggerEnter(Collider other)
    {
        if (other.TryGetComponent<Bullet>(out Bullet bullet))
        {
            if (isReflecting)
            {
                bullet.Reverse(gameObject);
            }
            else
            {
                Respawn();
            }
        }
    }
}
using UnityEngine;

public class Cammovement : MonoBehaviour
{
    [Header("Mouse look settings")]
    public float mouseSensitivity = 100f;
    public float pitchMin = -30f;
    public float pitchMax = 30f;  
    public float yawMin = -90f; 
    public float yawMax = 90f;
    public bool invertY = false;

    [Header("Optional smoothing")]
    public bool smooth = false;
    public float smoothSpeed = 10f;

    float yaw = 0f;
    float pitch = 0f;

    void OnEnable()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        Vector3 angles = transform.localEulerAngles;
        yaw = angles.y;
        pitch = angles.x;
    }

    void OnDisable()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    void Update()
    {
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity * Time.deltaTime;

        yaw += mouseX;
        if (invertY)
            pitch += mouseY;
        else
            pitch -= mouseY;

        // Clamp both axes
        yaw = Mathf.Clamp(yaw, yawMin, yawMax);
        pitch = Mathf.Clamp(pitch, pitchMin, pitchMax);

        Quaternion targetRot = Quaternion.Euler(pitch, yaw, 0f);

        if (smooth)
        {
            transform.localRotation = Quaternion.Slerp(transform.localRotation, targetRot, smoothSpeed * Time.deltaTime);
        }
        else
        {
            transform.localRotation = targetRot;
        }
    }
}

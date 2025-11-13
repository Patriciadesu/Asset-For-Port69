using UnityEngine;
using System.Collections;

public class TPSCameraController : MonoBehaviour
{
    public Transform target;
    public Vector3 offset = new Vector3(2, 8, -35);
    public float mouseSensitivity = 2f;
    public float minPitch = -30f;
    public float maxPitch = 60f;
    public float smoothSpeed = 10f;

    private float yaw = 0f;
    private float pitch = 10f;
    private Vector3 currentVelocity;

    // Camera Shake
    private Vector3 _shakeOffset = Vector3.zero;
    public float shakeDuration = 0.2f;
    public float shakeMagnitude = 10f;
    private bool rotateRequested = false;

    void Start()
    {
        yaw = target.eulerAngles.y;
    }

    void LateUpdate()
    {
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

        yaw += mouseX;
        pitch -= mouseY;
        pitch = Mathf.Clamp(pitch, minPitch, maxPitch);

        Quaternion rotation = Quaternion.Euler(pitch, yaw, 0f);
        Vector3 targetPosition = target.position + Vector3.up * 1.5f;

        // Camera collision
        Vector3 desiredCameraPos = targetPosition + rotation * offset;
        Vector3 direction = desiredCameraPos - targetPosition;
        float distance = direction.magnitude;

        RaycastHit hit;
        Vector3 finalPosition = desiredCameraPos;

        if (Physics.Raycast(targetPosition, direction.normalized, out hit, distance))
        {
            finalPosition = hit.point - direction.normalized * 0.2f;
        }

        // Smooth move (ใช้ unscaledDeltaTime)
        Vector3 smoothedPosition = Vector3.SmoothDamp(
            transform.position,
            finalPosition,
            ref currentVelocity,
            1f / smoothSpeed,
            Mathf.Infinity,
            Time.unscaledDeltaTime
        );
        transform.position = smoothedPosition + _shakeOffset;

        // LookAt
        transform.LookAt(targetPosition);

        // หมุนตัวละครเฉพาะเมื่อกดปุ่ม
        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");

        if ((horizontal != 0 || vertical != 0) || Input.GetKey(KeyCode.Space) ||!rotateRequested)
        {
            rotateRequested = true;
            StartCoroutine(RotateCharacterAfterDelay(0.2f));
        }
  

        UpdateShake();
    }

    IEnumerator Delayafterstopattack()
    {
        yield return new WaitForSecondsRealtime(10f); // ✅ ใช้ unscaled
        rotateRequested = false;

    }

    IEnumerator RotateCharacterAfterDelay(float delay)
    {
        yield return new WaitForSecondsRealtime(delay); // ✅ ใช้ unscaled

        float targetYaw = yaw;
        float currentYaw = target.eulerAngles.y;

        float elapsed = 0f;
        float rotateDuration = 0.2f;
        while (elapsed < rotateDuration)
        {
            elapsed += Time.unscaledDeltaTime; // ✅ ใช้ unscaled

            float newYaw = Mathf.LerpAngle(currentYaw, targetYaw, elapsed / rotateDuration);
            Vector3 characterRotation = target.eulerAngles;
            characterRotation.y = newYaw;
            target.eulerAngles = characterRotation;

            yield return null;
        }

        rotateRequested = false;
    }

    void UpdateShake()
    {
        if (shakeDuration > 0)
        {
            _shakeOffset = new Vector3(
                Random.Range(-1f, 1f) * shakeMagnitude,
                Random.Range(-1f, 1f) * shakeMagnitude,
                0);

            shakeDuration -= Time.unscaledDeltaTime; // ✅ ใช้ unscaled
        }
        else
        {
            _shakeOffset = Vector3.zero;
        }
    }

    // เรียกจากข้างนอกเพื่อให้กล้องสั่น
    public void ShakeCamera(float duration, float magnitude)
    {
        shakeDuration = duration;
        shakeMagnitude = magnitude;
    }
}
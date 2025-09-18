using UnityEngine;

public class CameraShakeEffect : ObjectEffect
{
    [Header("Camera Shake Settings")]
    [SerializeField] private float shakeDuration = 0.5f;
    [SerializeField] private float shakeIntensity = 1f;
    [SerializeField] private AnimationCurve shakeCurve = AnimationCurve.EaseInOut(0, 1, 1, 0);
    
    [Header("Cooldown Settings")]
    [SerializeField] private float cooldownTime = 1f;
    private float lastActivationTime = -999f;
    
    
    public override void ApplyEffect(Player player)
    {
        // Check cooldown
        if (Time.time - lastActivationTime < cooldownTime)
        {
            Debug.Log($"{gameObject.name} is on cooldown for {cooldownTime - (Time.time - lastActivationTime):F1} more seconds");
            return;
        }
        
        if (player != null)
        {
            // Update last activation time
            lastActivationTime = Time.time;
            
            // Handle different camera types
            if (player.Cam.cameraType == CameraType.FirstPerson)
            {
                Camera fpsCamera = player.camera;
                if (fpsCamera != null)
                {
                    player.StartCoroutine(ShakeCamera(fpsCamera.transform));
                    Debug.Log($"{gameObject.name} triggered FPS camera shake effect!");
                }
            }
            else if (player.Cam.cameraType == CameraType.ThirdPerson)
            {
                Transform tpsCameraPivot = player.tpsCameraPivot;
                if (tpsCameraPivot != null)
                {
                    player.StartCoroutine(ShakeCamera(tpsCameraPivot));
                    Debug.Log($"{gameObject.name} triggered TPS camera shake effect!");
                }
            }
        }
    }
    
    private System.Collections.IEnumerator ShakeCamera(Transform cameraTransform)
    {
        Vector3 originalPosition = cameraTransform.localPosition;
        float elapsedTime = 0f;
        
        while (elapsedTime < shakeDuration)
        {
            elapsedTime += Time.deltaTime;
            float strength = shakeCurve.Evaluate(elapsedTime / shakeDuration) * shakeIntensity;
            
            // Generate random shake offset
            Vector3 shakeOffset = new Vector3(
                Random.Range(-1f, 1f) * strength,
                Random.Range(-1f, 1f) * strength,
                0f
            );
            
            cameraTransform.localPosition = originalPosition + shakeOffset;
            yield return null;
        }
        
        // Reset camera position
        cameraTransform.localPosition = originalPosition;
    }
}

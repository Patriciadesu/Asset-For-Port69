using UnityEngine;

public class ShrinkSizeEffect : ObjectEffect
{
    [Header("Shrink Size Settings")]
    [SerializeField] private float sizeMultiplier = 0.5f;
    [SerializeField] private float duration = 5f;
    [SerializeField] private bool isPermanent = false;
    
    [Header("Cooldown Settings")]
    [SerializeField] private float cooldownTime = 2f;
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
            Transform playerTransform = player.transform;
            Vector3 currentScale = playerTransform.localScale;
            Vector3 targetScale = currentScale * sizeMultiplier;
            
            // Store original collider values
            CapsuleCollider capsuleCollider = player.capsuleCollider;
            float originalHeight = capsuleCollider.height;
            Vector3 originalCenter = capsuleCollider.center;
            float originalRadius = capsuleCollider.radius;
            
            // Apply immediate size change
            playerTransform.localScale = targetScale;
            
            // Adjust collider to match the new scale
            capsuleCollider.height = originalHeight * sizeMultiplier;
            capsuleCollider.center = originalCenter * sizeMultiplier;
            capsuleCollider.radius = originalRadius * sizeMultiplier;
            
            // Update last activation time
            lastActivationTime = Time.time;
            
            Debug.Log($"{gameObject.name} triggered shrink size effect - {player.gameObject.name} size shrunk!");
            
            // If not permanent, revert after duration
            if (!isPermanent && duration > 0)
            {
                player.StartCoroutine(RevertSizeAfterDelay(playerTransform, currentScale, duration,
                    originalHeight, originalCenter, originalRadius));
            }
        }
    }
    
    private System.Collections.IEnumerator RevertSizeAfterDelay(Transform playerTransform, Vector3 originalScale, float delay,
        float originalHeight, Vector3 originalCenter, float originalRadius)
    {
        yield return new WaitForSeconds(delay);
        
        if (playerTransform != null)
        {
            playerTransform.localScale = originalScale;
            
            // Revert collider to original values
            Player player = playerTransform.GetComponent<Player>();
            if (player != null && player.capsuleCollider != null)
            {
                player.capsuleCollider.height = originalHeight;
                player.capsuleCollider.center = originalCenter;
                player.capsuleCollider.radius = originalRadius;
            }
            
            Debug.Log($"Player size and collider reverted to normal after {delay} seconds");
        }
    }
}

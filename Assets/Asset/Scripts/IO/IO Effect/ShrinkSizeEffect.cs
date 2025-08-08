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
    
    public override void ApplyEffect(Collision playerCollision)
    {
        Player player = playerCollision.gameObject.GetComponent<Player>();
        if (player != null)
        {
            ApplyEffect(playerCollision, player);
        }
    }
    
    public override void ApplyEffect(Collision playerCollision, Player player)
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
            
            // Apply immediate size change
            playerTransform.localScale = targetScale;
            
            // Update last activation time
            lastActivationTime = Time.time;
            
            Debug.Log($"{gameObject.name} triggered shrink size effect - {player.gameObject.name} size shrunk!");
            
            // If not permanent, revert after duration
            if (!isPermanent && duration > 0)
            {
                player.StartCoroutine(RevertSizeAfterDelay(playerTransform, currentScale, duration));
            }
        }
    }
    
    private System.Collections.IEnumerator RevertSizeAfterDelay(Transform playerTransform, Vector3 originalScale, float delay)
    {
        yield return new WaitForSeconds(delay);
        
        if (playerTransform != null)
        {
            playerTransform.localScale = originalScale;
            Debug.Log($"Player size reverted to normal after {delay} seconds");
        }
    }
}

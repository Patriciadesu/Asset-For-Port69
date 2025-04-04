using UnityEngine;

public class FreezeEffect : ObjectEffect
{
    [SerializeField] private float freezeDuration = 2f;
    [SerializeField] private bool debugMode = false;

    private Coroutine activeCoroutine;
    private float currentElapsedTime = -1f;

    public override void ApplyEffect(Collision playerCollision)
    {
        PlayerController player = playerCollision.gameObject.GetComponent<PlayerController>();
        if (player != null)
        {
            if (activeCoroutine == null)
            {
                activeCoroutine = player.StartCoroutine(ApplyFreezeEffect(player, freezeDuration));
                Debug.Log($"{gameObject.name} triggered freeze effect (duration: {freezeDuration}s)");
            }
            else
            {
                currentElapsedTime = 0f;
                if (debugMode)
                {
                    Debug.Log($"{gameObject.name} freeze effect timer reset (remaining time refreshed to {freezeDuration}s)");
                }
            }
        }
        else if (debugMode)
        {
            Debug.LogWarning($"Freeze effect on {gameObject.name} failed - No PlayerController found on {playerCollision.gameObject.name}!");
        }
    }

    private System.Collections.IEnumerator ApplyFreezeEffect(PlayerController player, float duration)
    {
        float originalMultiplier = player.speedMultiplier;
        float newMultiplier = 0f;

        if (debugMode)
        {
            Debug.Log($"Speed multiplier changed: {originalMultiplier} to {newMultiplier}");
        }

        player.speedMultiplier = newMultiplier;
        currentElapsedTime = 0f;

        while (currentElapsedTime < duration)
        {
            currentElapsedTime += Time.deltaTime;

            if (debugMode && currentElapsedTime % 0.5f < 0.1f)
            {
                Debug.Log($"Freeze effect: {(duration - currentElapsedTime):F1}s remaining");
            }

            yield return null;
        }

        player.speedMultiplier = originalMultiplier;
        activeCoroutine = null;
        currentElapsedTime = -1f;

        if (debugMode)
        {
            Debug.Log($"Freeze effect ended, multiplier reset to {originalMultiplier}");
        }
    }
}
using UnityEngine;

public class SlowEffect : ObjectEffect
{
    [SerializeField] private float slowMultiplier = 0.5f;
    [SerializeField] private float duration = 2f;
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
                activeCoroutine = player.StartCoroutine(ApplySlowEffect(player, slowMultiplier, duration));
                Debug.Log($"{gameObject.name} triggered slow effect (multiplier: {slowMultiplier}x, duration: {duration}s)");
            }
            else
            {
                currentElapsedTime = 0f;
                if (debugMode)
                {
                    Debug.Log($"{gameObject.name} slow effect timer reset (remaining time refreshed to {duration}s)");
                }
            }
        }
        else if (debugMode)
        {
            Debug.LogWarning($"Slow effect on {gameObject.name} failed - No PlayerController found on {playerCollision.gameObject.name}!");
        }
    }

    private System.Collections.IEnumerator ApplySlowEffect(PlayerController player, float multiplier, float duration)
    {
        float originalMultiplier = player.speedMultiplier;
        float newMultiplier = originalMultiplier * multiplier;

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
                Debug.Log($"Slow effect: {(duration - currentElapsedTime):F1}s remaining");
            }

            yield return null;
        }

        player.speedMultiplier = originalMultiplier;
        activeCoroutine = null;
        currentElapsedTime = -1f;

        if (debugMode)
        {
            Debug.Log($"Slow effect ended, multiplier reset to {originalMultiplier}");
        }
    }
}
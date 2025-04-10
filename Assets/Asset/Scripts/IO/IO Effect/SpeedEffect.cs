using UnityEngine;

public class SpeedEffect : ObjectEffect
{
    [SerializeField] private float speedMultiplier = 2f;
    [SerializeField] private float duration = 3f;
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
                activeCoroutine = player.StartCoroutine(ApplySpeedBoost(player, speedMultiplier, duration));
                Debug.Log($"{gameObject.name} triggered speed effect (multiplier: {speedMultiplier}x, duration: {duration}s)");
            }
            else
            {
                currentElapsedTime = 0f;
                if (debugMode)
                {
                    Debug.Log($"{gameObject.name} speed effect timer reset (remaining time refreshed to {duration}s)");
                }
            }
        }
        else if (debugMode)
        {
            Debug.LogWarning($"Speed effect on {gameObject.name} failed - No PlayerController found on {playerCollision.gameObject.name}!");
        }
    }

    private System.Collections.IEnumerator ApplySpeedBoost(PlayerController player, float multiplier, float duration)
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
                Debug.Log($"Speed effect: {(duration - currentElapsedTime):F1}s remaining");
            }

            yield return null;
        }

        player.speedMultiplier = originalMultiplier;
        activeCoroutine = null;
        currentElapsedTime = -1f;

        if (debugMode)
        {
            Debug.Log($"Speed effect ended, multiplier reset to {originalMultiplier}");
        }
    }
}
using UnityEngine;
using System.Collections;
public class StraightRushEffect : ObjectEffect
{
    [SerializeField] private float rushMultiplier = 2f;
    [SerializeField] private float rushForce = 20f;
    [SerializeField] private float duration = 2f;
    [SerializeField] private bool lockDirection = true;
    [SerializeField] private bool debugMode = false;
    private Coroutine activeCoroutine;
    public override void ApplyEffect(Player player)
    {
        if (player != null)
        {
            if (activeCoroutine != null)
            {
                player.StopCoroutine(activeCoroutine);
            }
            activeCoroutine = player.StartCoroutine(ApplyStraightRush(player));
            if (debugMode)
            {
                Debug.Log($"{gameObject.name} applied straight rush to {player.gameObject.name} for {duration}s");
            }
        }
    }
    private IEnumerator ApplyStraightRush(Player player)
    {
        float originalMultiplier = player.speedMultiplier;
        Vector3 rushDirection = player.transform.forward;
        Rigidbody playerRb = player.GetComponent<Rigidbody>();
        player.speedMultiplier *= rushMultiplier;
        if (playerRb != null)
        {
            playerRb.AddForce(rushDirection * rushForce, ForceMode.Impulse);
        }
        float elapsed = 0f;
        while (elapsed < duration)
        {
            if (lockDirection && playerRb != null)
            {
                Vector3 currentVel = playerRb.velocity;
                Vector3 forwardVel = Vector3.Project(currentVel, rushDirection);
                Vector3 newVel = new Vector3(forwardVel.x, currentVel.y, forwardVel.z);
                playerRb.velocity = newVel;
            }
            elapsed += Time.deltaTime;
            yield return null;
        }
        player.speedMultiplier = originalMultiplier;
        activeCoroutine = null;
        if (debugMode)
        {
            Debug.Log($"Straight rush ended for {player.gameObject.name}, speed reset to {originalMultiplier}x");
        }
    }
}
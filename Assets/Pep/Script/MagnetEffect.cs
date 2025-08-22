using UnityEngine;
using System.Collections;

public class MagnetEffect : ObjectEffect
{
    [SerializeField] private float magnetRange = 10f;
    [SerializeField] private float magnetForce = 15f;
    [SerializeField] private float duration = 5f;
    [SerializeField] private string[] collectibleTags = { "Coin", "Gem", "PowerUp" };
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
            activeCoroutine = player.StartCoroutine(ApplyMagnetEffect(player));
            if (debugMode)
            {
                Debug.Log($"{gameObject.name} applied magnet effect to {player.gameObject.name} for {duration}s");
            }
        }
    }
    private IEnumerator ApplyMagnetEffect(Player player)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            foreach (string tag in collectibleTags)
            {
                GameObject[] collectibles = GameObject.FindGameObjectsWithTag(tag);
                foreach (GameObject collectible in collectibles)
                {
                    float distance = Vector3.Distance(player.transform.position, collectible.transform.position);
                    if (distance <= magnetRange)
                    {
                        Rigidbody collectibleRb = collectible.GetComponent<Rigidbody>();
                        if (collectibleRb != null)
                        {
                            Vector3 direction = (player.transform.position - collectible.transform.position).normalized;
                            float forceAmount = magnetForce * (1f - distance / magnetRange);
                            collectibleRb.AddForce(direction * forceAmount, ForceMode.Force);
                        }
                    }
                }
            }
            elapsed += Time.deltaTime;
            yield return null;
        }
        activeCoroutine = null;
        if (debugMode)
        {
            Debug.Log($"Magnet effect ended for {player.gameObject.name}");
        }
    }
}
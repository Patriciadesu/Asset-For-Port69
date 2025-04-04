using UnityEngine;

public class InteractableObject : MonoBehaviour
{
    private ObjectEffect[] effects;

    void Start()
    {
        effects = GetComponents<ObjectEffect>();
        EnsureColliderExists();
    }

    void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            PlayerController player = collision.gameObject.GetComponent<PlayerController>();
            if (player != null)
            {
                Debug.Log($"Player Hit: {collision.gameObject.name} (has PlayerController)");
                HandlePlayerCollision(collision);
            }
            else
            {
                Debug.LogWarning($"Player Hit: {collision.gameObject.name} tagged as 'Player' but missing PlayerController!");
            }
        }
    }

    protected virtual void HandlePlayerCollision(Collision playerCollision)
    {
        foreach (ObjectEffect effect in effects)
        {
            effect.ApplyEffect(playerCollision);
        }
    }

    private void EnsureColliderExists()
    {
        MeshRenderer meshRenderer = GetComponent<MeshRenderer>();
        if (meshRenderer != null && GetComponent<Collider>() == null)
        {
            MeshFilter meshFilter = GetComponent<MeshFilter>();
            if (meshFilter != null && meshFilter.sharedMesh != null)
            {
                MeshCollider meshCollider = gameObject.AddComponent<MeshCollider>();
                meshCollider.sharedMesh = meshFilter.sharedMesh;
                meshCollider.isTrigger = false;
                Debug.Log($"Added MeshCollider to {gameObject.name}");
            }
            else
            {
                gameObject.AddComponent<BoxCollider>();
                Debug.Log($"Added BoxCollider to {gameObject.name} (no valid mesh found)");
            }
        }
    }

    public void RefreshEffects()
    {
        effects = GetComponents<ObjectEffect>();
    }
}

public abstract class ObjectEffect : MonoBehaviour
{
    public abstract void ApplyEffect(Collision playerCollision);
}
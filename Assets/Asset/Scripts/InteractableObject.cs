using Unity.VisualScripting;
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
        // Check for any existing Collider first
        if (GetComponent<Collider>() == null)
        {
            // Handle regular MeshRenderer case
            MeshFilter meshFilter = GetComponent<MeshFilter>();
            if (meshFilter != null && meshFilter.sharedMesh != null)
            {
                var meshCollider = gameObject.AddComponent<MeshCollider>();
                meshCollider.sharedMesh = meshFilter.sharedMesh;
                meshCollider.convex = false;
                meshCollider.isTrigger = false;
                Debug.Log($"Added MeshCollider to {gameObject.name}");
            }
            // Handle LODGroup fallback
            else if (TryGetComponent<LODGroup>(out LODGroup lodGroup))
            {
                foreach (Transform child in lodGroup.transform)
                {
                    if (child.TryGetComponent<MeshRenderer>(out _) && !child.TryGetComponent<Collider>(out _))
                    {
                        var childMeshFilter = child.GetComponent<MeshFilter>();
                        if (childMeshFilter != null && childMeshFilter.sharedMesh != null)
                        {
                            var lodCollider = child.gameObject.AddComponent<MeshCollider>();
                            lodCollider.sharedMesh = childMeshFilter.sharedMesh;
                            lodCollider.convex = false;
                            lodCollider.isTrigger = false;
                            Debug.Log($"Added MeshCollider to LOD child: {child.name}");
                            break; // Just apply to the first LOD
                        }
                    }
                }
            }
            // Fallback to BoxCollider if mesh is not available
            else
            {
                gameObject.AddComponent<BoxCollider>();
                Debug.Log($"Added BoxCollider to {gameObject.name} (no mesh found)");
            }
        }

        // Ensure a Rigidbody exists
        if (!TryGetComponent<Rigidbody>(out Rigidbody rb))
        {
            rb = gameObject.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            Debug.Log($"Added Rigidbody to {gameObject.name} (set to kinematic)");
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
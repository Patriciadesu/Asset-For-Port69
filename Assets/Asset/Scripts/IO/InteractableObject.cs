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

        bool hasCollider = false;
        // Check for any existing Collider first
        if (GetComponent<Collider>() == null)
        {
            MeshFilter meshFilter = GetComponent<MeshFilter>();
            if (meshFilter != null && meshFilter.sharedMesh != null)
            {
                var meshCollider = gameObject.AddComponent<MeshCollider>();
                meshCollider.sharedMesh = meshFilter.sharedMesh;
                meshCollider.convex = false;
                meshCollider.isTrigger = false;
                Debug.Log($"Added MeshCollider to {gameObject.name}");
            }
            else if (TryGetComponent<LODGroup>(out LODGroup lodGroup))
            {
                foreach (Transform lodChild in lodGroup.transform)
                {
                    if (lodChild.TryGetComponent<MeshRenderer>(out _) &&
                        !lodChild.TryGetComponent<Collider>(out _) &&
                        lodChild.TryGetComponent<MeshFilter>(out MeshFilter lodMeshFilter) &&
                        lodMeshFilter.sharedMesh != null)
                    {
                        var lodCollider = lodChild.gameObject.AddComponent<MeshCollider>();
                        lodCollider.sharedMesh = lodMeshFilter.sharedMesh;
                        lodCollider.convex = false;
                        lodCollider.isTrigger = false;
                        Debug.Log($"Added MeshCollider to LOD child: {lodChild.name}");
                        break;
                    }
                }
            }
            else
            {
                foreach (Transform child in transform)
                {
                    MeshFilter childMeshFilter = child.GetComponent<MeshFilter>();
                    Collider childCollider = child.GetComponent<Collider>();

                    if (childMeshFilter != null && childMeshFilter.sharedMesh != null && childCollider == null)
                    {
                        MeshCollider meshCollider = child.gameObject.AddComponent<MeshCollider>();
                        meshCollider.sharedMesh = childMeshFilter.sharedMesh;
                        meshCollider.convex = false;
                        meshCollider.isTrigger = false;

                        Debug.Log($"Added MeshCollider to child: {child.name}");
                        hasCollider = true;
                    }
                }
            }
        }
        else { hasCollider = true; }
        if (!hasCollider)
        {
            Debug.LogError("No Collider Can Be Added Please Add It Manually.");
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
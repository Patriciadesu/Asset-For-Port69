using NaughtyAttributes;
using UnityEngine;
[RequireComponent(typeof(Rigidbody))]
public class InteractableObject : MonoBehaviour
{
    [SerializeField] bool isTrigger = false;
    [SerializeField, HideIf("isTrigger")] bool usePhysic = true;
    [SerializeField,HideIf("isTrigger")] bool useGravity = true;
    
    private ObjectEffect[] effects;
    void Start()
    {
        if (isTrigger)
        {
            useGravity = false;
            usePhysic = false;
        }
        GetComponent<Rigidbody>().isKinematic = !usePhysic;   
        GetComponent<Rigidbody>().useGravity = useGravity;
        effects = GetComponents<ObjectEffect>();
        EnsureColliderExists();
        EnsureRigidbodyExists();
        EnsureIsTrigger();
    }
    private void EnsureColliderExists()
    {
        bool hasCollider = false;
        if (GetComponent<Collider>() == null)
        {
            MeshFilter meshFilter = GetComponent<MeshFilter>();
            if (meshFilter != null && meshFilter.sharedMesh != null)
            {
                var meshCollider = gameObject.AddComponent<MeshCollider>();
                meshCollider.sharedMesh = meshFilter.sharedMesh;
                meshCollider.convex = false;
                meshCollider.isTrigger = isTrigger;
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
                        lodCollider.convex = true;
                        lodCollider.isTrigger = isTrigger;
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
                        meshCollider.convex = true;
                        meshCollider.isTrigger = isTrigger;
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
    }
    private void EnsureIsTrigger()
    {
        Collider collider = GetComponent<Collider>();
        if (collider != null)
        {
            collider.isTrigger = isTrigger;
            Debug.Log($"Set {gameObject.name} collider as trigger.");
        }
        foreach (Transform child in transform)
        {
            Collider childCollider = child.GetComponent<Collider>();
            if (childCollider != null)
            {
                childCollider.isTrigger = isTrigger;
                Debug.Log($"Set {child.name} collider as trigger.");
            }
        }
    }
    private void EnsureRigidbodyExists()
    {
        if (GetComponent<Rigidbody>() == null)
        {
            gameObject.AddComponent<Rigidbody>();
        }
    }
    public void RefreshEffects()
    {
        effects = GetComponents<ObjectEffect>();
    }
}
public abstract class ObjectEffect : MonoBehaviour
{

    public void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.TryGetComponent<Player>(out Player player))
        {
            ApplyEffect(collision, player);
        }
    }
    public void OnTriggerEnter(Collider other)
    {
        if (other.TryGetComponent<Player>(out Player player))
        {
            ApplyEffect(player);
        }
    }

    public virtual void ApplyEffect(Player player) { }
    public virtual void ApplyEffect(Collision collision, Player player) => ApplyEffect(player);
}
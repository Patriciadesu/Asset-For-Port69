using Unity.VisualScripting;
using UnityEngine;

public class PlayerDieWhenTouch : MonoBehaviour
{
    private void Start()
    {
        if(TryGetComponent<MeshCollider>(out MeshCollider meshCollider))
        {
            meshCollider.convex = true;
            meshCollider.isTrigger = true;
        }
        else if(TryGetComponent<Collider>(out Collider collider))
        {
            collider.isTrigger = true;
        }
        else if(TryGetComponent<MeshRenderer>(out MeshRenderer meshRenderer))
            {
            this.AddComponent<MeshCollider>();
            this.GetComponent<MeshCollider>().convex = true;
            this.GetComponent<MeshCollider>().isTrigger = true;
        } 
        else
        {
            Debug.LogError("Missing Component of type Collider");
        }
    }
    private void OnTriggerEnter(Collider collision)
    {
        if (collision.gameObject.tag == "Player")
        {
            collision.gameObject.GetComponent<PlayerController>().Respawn();
        }
    }
}

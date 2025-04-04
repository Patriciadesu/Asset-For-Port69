using UnityEngine;

public class Bullet : MonoBehaviour
{
    [HideInInspector] public GameObject owner;
    private void OnTriggerEnter(Collider other)
    {
        if(other.tag == "player" && owner.tag!="Player")
        {
            PlayerController player = other.GetComponent<PlayerController>();
            if(player.isReflecting)
            {
                owner = player.gameObject;
                
            }
            else
            {
                player.Respawn();
            }
        }
    }
    public void Reverse(GameObject newOwner)
    {
        owner = newOwner;
        Rigidbody rb = this.GetComponent<Rigidbody>();
        rb.linearVelocity*=-2;
    }
}

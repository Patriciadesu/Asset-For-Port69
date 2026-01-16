using UnityEngine;

public class Bullet : MonoBehaviour
{
    [SerializeField] private float lifetime = 5f;
    
    private void Start()
    {
        Destroy(gameObject, lifetime);
    }
    
    private void OnTriggerEnter(Collider other)
    {
        HandleImpact(other.gameObject);
    }

    private void OnCollisionEnter(Collision collision)
    {
        HandleImpact(collision.gameObject);
    }

    private void HandleImpact(GameObject target)
    {
        if (target.CompareTag("Enemy"))
        {
            // Destroy the enemy
            Destroy(target);
            // Destroy the bullet itself
            Destroy(gameObject);
        }
    }
}
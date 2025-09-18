using UnityEngine;

public class MagicBullet : MonoBehaviour
{
    public float damage;
    public void SetUp(float _damage)
    {
        damage = _damage;
    }
    public void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.TryGetComponent<IEnemy>(out IEnemy enemy))
        {
            enemy.TakeDamage(damage);
            Destroy(this.gameObject);
        }
    }
    public void OnTriggerEnter(Collider other)
    {
        if (other.TryGetComponent<IEnemy>(out IEnemy enemy))
        {
            enemy.TakeDamage(damage);
            Destroy(this.gameObject);
        }
    }
}
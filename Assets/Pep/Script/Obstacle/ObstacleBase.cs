using UnityEngine;

public abstract class ObstacleBase : MonoBehaviour
{
    protected Rigidbody rb;
    protected bool isActive;
    public virtual void Init(Vector3 direction, float speed)
    {
        rb = GetComponent<Rigidbody>();
        isActive = true;
        gameObject.SetActive(true);
        Launch(direction, speed);
    }
    protected abstract void Launch(Vector3 direction, float speed);
    public virtual void Deactivate()
    {
        isActive = false;
        gameObject.SetActive(false);
    }
    protected virtual void OnHitPlayer(GameObject player)
    {
        player.GetComponent<Player>().Respawn();
    }
    private void OnTriggerEnter(Collider other)
    {
        if (!isActive) return;
        if (other.CompareTag("Player"))
        {
            OnHitPlayer(other.gameObject);
            Deactivate();
        }
    }
    private void OllisionEnter(Collision collision)
    {
        if (!isActive) return;
        if (collision.gameObject.CompareTag("Player"))
        {
            OnHitPlayer(collision.gameObject);
            Deactivate();
        }
    }
}
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
        Debug.Log($"{name} hit the player!");
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
}
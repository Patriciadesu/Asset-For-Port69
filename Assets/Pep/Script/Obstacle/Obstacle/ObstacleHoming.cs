using UnityEngine;

public class ObstacleHoming : ObstacleBase
{
    private Transform target;
    private float speed;

    protected override void Launch(Vector3 direction, float speed)
    {
        this.speed = speed;
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            target = player.transform;
        }
        else
        {

            rb.linearVelocity = direction.normalized * speed;
        }
    }

    private void FixedUpdate()
    {
        if (!isActive) return;

        if (target != null)
        {
            Vector3 dir = (target.position - transform.position).normalized;
            rb.linearVelocity = dir * speed;
        }
    }

    protected override void OnHitPlayer(GameObject player)
    {
        Debug.Log($"{name} homed in on the player!");
    }
}
using UnityEngine;

public class ObstacleHoming : ObstacleBase
{
    private Transform target;
    private float speed;

    protected override void Launch(Vector3 direction, float speed)
    {
        this.speed = speed;
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null) target = player.transform;
    }

    private void FixedUpdate()
    {
        if (!isActive || target == null) return;
        Vector3 dir = (target.position - transform.position).normalized;
        rb.velocity = dir * speed;
    }
}

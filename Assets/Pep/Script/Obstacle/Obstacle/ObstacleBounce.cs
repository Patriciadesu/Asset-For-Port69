using UnityEngine;


public class ObstacleBounce : ObstacleBase
{
    protected override void Launch(Vector3 direction, float speed)
    {
        rb.linearVelocity = direction.normalized * speed;
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (!isActive) return;
        rb.linearVelocity = Vector3.Reflect(rb.linearVelocity, collision.contacts[0].normal);
    }
}

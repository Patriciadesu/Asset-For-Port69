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

        Vector3 normal = collision.contacts[0].normal;

        rb.linearVelocity = Vector3.Reflect(rb.linearVelocity, normal);
    }

}
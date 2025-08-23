using UnityEngine;

public class ObstacleRolling : ObstacleBase
{
    protected override void Launch(Vector3 direction, float speed)
    {
        rb.velocity = direction.normalized * speed;
        rb.angularVelocity = Random.insideUnitSphere * 5f;
    }
}

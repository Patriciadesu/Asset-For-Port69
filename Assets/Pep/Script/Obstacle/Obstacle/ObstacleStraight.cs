using UnityEngine;

public class ObstacleStraight : ObstacleBase
{
    protected override void Launch(Vector3 direction, float speed)
    {
        rb.linearVelocity = direction.normalized * speed;
    }

    protected override void OnHitPlayer(GameObject player)
    {
        Debug.Log($"{name} hit the player straight on!");
    }
}
using UnityEngine;

public class ObstacleDecoy : ObstacleBase
{
    protected override void Launch(Vector3 direction, float speed)
    {
        rb.linearVelocity = direction.normalized * speed;
    }

    protected override void OnHitPlayer(GameObject player)
    {
        Debug.Log("Decoy vanished, no damage.");

    }
}
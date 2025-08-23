using UnityEngine;

public class ObstacleDecoy : ObstacleBase
{
    protected override void Launch(Vector3 direction, float speed)
    {
        rb.velocity = direction.normalized * speed;
    }

    protected override void OnHitPlayer(GameObject player)
    {
        Debug.Log("Decoy vanished, no damage.");
        Deactivate();
    }
}

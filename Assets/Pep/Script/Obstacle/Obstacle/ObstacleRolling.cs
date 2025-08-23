using UnityEngine;

public class ObstacleRolling : ObstacleBase
{
    protected override void Launch(Vector3 direction, float speed)
    {
        rb.linearVelocity = direction.normalized * speed;
        rb.angularVelocity = Random.insideUnitSphere * 5f;
    }

    protected override void OnHitPlayer(GameObject player)
    {
        Debug.Log($"{name} rolled into the player!");
    }
}
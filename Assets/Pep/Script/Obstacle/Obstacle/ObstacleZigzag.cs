using UnityEngine;

public class ObstacleZigzag : ObstacleBase
{
    private Vector3 moveDir;
    private float speed;
    [SerializeField] private float zigzagStrength = 3f;
    [SerializeField] private float frequency = 5f;

    protected override void Launch(Vector3 direction, float speed)
    {
        this.moveDir = direction.normalized;
        this.speed = speed;
    }

    private void FixedUpdate()
    {
        if (!isActive) return;

        Vector3 perpendicular = Vector3.Cross(moveDir, Vector3.up).normalized;
        Vector3 offset = perpendicular * Mathf.Sin(Time.time * frequency) * zigzagStrength;

        rb.linearVelocity = (moveDir * speed) + offset;
    }

    protected override void OnHitPlayer(GameObject player)
    {
        Debug.Log($"{name} zigzagged into the player!");
    }
}
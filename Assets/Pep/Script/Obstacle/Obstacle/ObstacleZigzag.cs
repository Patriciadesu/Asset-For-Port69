using UnityEngine;

public class ObstacleZigzag : ObstacleBase
{
    private Vector3 moveDir;
    private float speed;
    public float zigzagStrength = 3f;
    public float frequency = 5f;

    protected override void Launch(Vector3 direction, float speed)
    {
        this.moveDir = direction.normalized;
        this.speed = speed;
    }

    private void FixedUpdate()
    {
        if (!isActive) return;
        Vector3 offset = Vector3.right * Mathf.Sin(Time.time * frequency) * zigzagStrength;
        rb.velocity = (moveDir + offset).normalized * speed;
    }
}

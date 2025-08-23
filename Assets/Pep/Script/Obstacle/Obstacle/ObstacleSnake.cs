using UnityEngine;

public class ObstacleSnake : ObstacleBase
{
    private Vector3 moveDir;
    private float speed;
    private float frequency = 3f;
    private float amplitude = 2f;

    protected override void Launch(Vector3 direction, float speed)
    {
        moveDir = direction.normalized;
        this.speed = speed;
    }

    private void FixedUpdate()
    {
        if (!isActive) return;
        Vector3 snakeOffset = Vector3.up * Mathf.Sin(Time.time * frequency) * amplitude;
        rb.velocity = (moveDir + snakeOffset).normalized * speed;
    }
}

using UnityEngine;

public class ObstacleSnake : ObstacleBase
{
    private Vector3 moveDir;
    private float speed;
    [SerializeField] private float frequency = 3f;
    [SerializeField] private float amplitude = 2f;

    protected override void Launch(Vector3 direction, float speed)
    {
        moveDir = direction.normalized;
        this.speed = speed;
    }

    private void FixedUpdate()
    {
        if (!isActive) return;

        Vector3 perpendicular = Vector3.Cross(moveDir, Vector3.up).normalized;
        Vector3 snakeOffset = perpendicular * Mathf.Sin(Time.time * frequency) * amplitude;

        rb.linearVelocity = (moveDir * speed) + snakeOffset;
    }

    protected override void OnHitPlayer(GameObject player)
    {
        Debug.Log($"{name} slithered into the player!");
    }
}
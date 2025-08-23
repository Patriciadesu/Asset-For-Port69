using UnityEngine;

public class ObstacleSplit : ObstacleBase
{
    public ObstacleBase splitPrefab;
    public int splitCount = 3;

    protected override void Launch(Vector3 direction, float speed)
    {
        rb.velocity = direction.normalized * speed;
    }

    public override void Deactivate()
    {
        base.Deactivate();
        for (int i = 0; i < splitCount; i++)
        {
            Vector3 dir = Random.onUnitSphere;
            ObstacleBase child = Instantiate(splitPrefab, transform.position, Quaternion.identity);
            child.Init(dir, 3f);
        }
    }
}

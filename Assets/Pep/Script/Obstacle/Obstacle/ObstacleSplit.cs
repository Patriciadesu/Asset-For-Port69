using UnityEngine;

public class ObstacleSplit : ObstacleBase
{
    [SerializeField] private ObstacleBase splitPrefab;
    [SerializeField] private int splitCount = 3;
    [SerializeField] private float splitSpeed = 3f;

    protected override void Launch(Vector3 direction, float speed)
    {
        rb.linearVelocity = direction.normalized * speed;
    }

    protected override void OnHitPlayer(GameObject player)
    {
        Debug.Log($"{name} split when hitting the player!");
    }

    public override void Deactivate()
    {

        if (splitPrefab != null && splitCount > 0)
        {
            for (int i = 0; i < splitCount; i++)
            {
                float angle = (360f / splitCount) * i;
                Vector3 dir = Quaternion.Euler(0, angle, 0) * Vector3.forward;

                ObstacleBase child = Instantiate(splitPrefab, transform.position, Quaternion.identity);
                child.Init(dir, splitSpeed);

                StartCoroutine(DestroyAfterTime(child, 3f));
            }
        }

        base.Deactivate();
    }

    private System.Collections.IEnumerator DestroyAfterTime(ObstacleBase obstacle, float time)
    {
        yield return new WaitForSeconds(time);
        if (obstacle != null)
        {
            obstacle.Deactivate();
            Destroy(obstacle.gameObject);
        }
    }
}
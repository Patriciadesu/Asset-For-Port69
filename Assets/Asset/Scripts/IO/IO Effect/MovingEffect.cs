using UnityEngine;

public class MovingEffect : ObjectEffect
{
    [SerializeField] private Vector3 targetPosition;
    [SerializeField] private float moveSpeed = 2f;
    private Vector3 originalPosition;

    private void Start()
    {
        originalPosition = transform.position;
        StartCoroutine(MoveLoopRoutine());
    }

    private System.Collections.IEnumerator MoveLoopRoutine()
    {
        while (true)
        {
            while (Vector3.Distance(transform.position, targetPosition) > 0.1f)
            {
                transform.position = Vector3.Lerp(transform.position, targetPosition,
                    moveSpeed * Time.deltaTime);
                yield return null;
            }

            yield return new WaitForSeconds(1f);

            while (Vector3.Distance(transform.position, originalPosition) > 0.1f)
            {
                transform.position = Vector3.Lerp(transform.position, originalPosition,
                    moveSpeed * Time.deltaTime);
                yield return null;
            }

            yield return new WaitForSeconds(1f);
        }
    }

    public override void ApplyEffect(Collision playerCollision)
    {
        
    }
}

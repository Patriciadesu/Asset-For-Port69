using UnityEngine;
using System.Collections;
using NaughtyAttributes;
using System.Collections.Generic;

public class MovingEffect : ObjectEffect
{
    public enum MovingType
    {
        FromCurrentPosition,
        ToDestinatePosition
    }
    public MovingType type;
    [SerializeField,ShowIf("type",MovingType.ToDestinatePosition)] private Vector3 destinatePosition;
    [SerializeField, ShowIf("type", MovingType.FromCurrentPosition)] private Vector3 offsetPosition;
    private Vector3 startPosition;
    private Vector3 targetPosition
    {
        get
        {
            if (type == MovingType.FromCurrentPosition)
            {
                return startPosition + offsetPosition;
            }
            else 
            {
                return destinatePosition;
            }
        }
    }
    [SerializeField] private float moveSpeed = 2f;
    private Vector3 originalPosition;
    private HashSet<GameObject> playersOnPlatform = new HashSet<GameObject>();

    private void Start()
    {
        startPosition = transform.position;
        originalPosition = transform.position;
        StartCoroutine(MoveLoopRoutine());
    }

    private IEnumerator MoveLoopRoutine()
    {
        while (true)
        {
            while (Vector3.Distance(transform.position, targetPosition) > 0.1f)
            {
                Vector3 previousPos = transform.position;
                transform.position = Vector3.Lerp(transform.position, targetPosition,
                    moveSpeed * Time.deltaTime);
                Vector3 delta = transform.position - previousPos;
                foreach (var player in playersOnPlatform)
                {
                    if (player != null)
                    {
                        player.transform.position += delta;
                    }
                }
                yield return null;
            }

            yield return new WaitForSeconds(1f);

            while (Vector3.Distance(transform.position, originalPosition) > 0.1f)
            {
                Vector3 previousPos = transform.position;
                transform.position = Vector3.Lerp(transform.position, originalPosition,
                    moveSpeed * Time.deltaTime);
                Vector3 delta = transform.position - previousPos;
                foreach (var player in playersOnPlatform)
                {
                    if (player != null)
                    {
                        player.transform.position += delta;
                    }
                }
                yield return null;
            }

            yield return new WaitForSeconds(1f);
        }
    }

    
       public override void ApplyEffect(GameObject player)
    {
        playersOnPlatform.Add(player);
    }

    public void OnTriggerExit(Collider other)
    {
        if(other.tag == "Player")
        {
            playersOnPlatform.Remove(other.gameObject);
        }
    }
    public void OnCollisionExit(Collision collision)
    {
        if(collision.gameObject.tag == "Player")
        {
            playersOnPlatform.Remove(collision.gameObject);
        }
    }

    public void OnDrawGizmos()
    {
        Vector3 center = Vector3.zero;
        if (type == MovingType.FromCurrentPosition)
        {
            center =  transform.position + offsetPosition;
        }
        else 
        {
            center = destinatePosition;
        }
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(center, 2);
    }
}

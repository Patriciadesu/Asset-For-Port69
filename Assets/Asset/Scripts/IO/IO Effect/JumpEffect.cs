using UnityEngine;

public class JumpEffect : ObjectEffect
{
    [SerializeField] private float jumpForce = 10f;

    public override void ApplyEffect(Collision playerCollision)
    {
        Player player = playerCollision.gameObject.GetComponent<Player>();
        if (player != null /*&& player.isGrounded*/)
        {
            player.rigidbody.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);

            Debug.Log($"{gameObject.name} triggered jump effect");
        }
    }
    
    public override void ApplyEffect(Collision playerCollision, Player player)
    {
        if (player != null /*&& player.isGrounded*/)
        {
            player.rigidbody.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);

            Debug.Log($"{gameObject.name} triggered jump effect on {player.gameObject.name}");
        }
    }
}
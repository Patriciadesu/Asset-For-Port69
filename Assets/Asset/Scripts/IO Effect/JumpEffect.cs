using UnityEngine;

public class JumpEffect : ObjectEffect
{
    [SerializeField] private float jumpForce = 10f;

    public override void ApplyEffect(Collision playerCollision)
    {
        PlayerController player = playerCollision.gameObject.GetComponent<PlayerController>();
        if (player != null /*&& player.isGrounded*/)
        {
            player.rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);

            Debug.Log($"{gameObject.name} triggered jump effect");
        }
    }
}
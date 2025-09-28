using UnityEngine;

[System.Serializable]
public class MovementModule : PlayerModule
{
    #region Movement Settings
    public float Speed => (baseSpeed + additionalSpeed) * speedMultiplier;

    [SerializeField, Range(0, 100)] private float baseSpeed = 5f;
    [Range(0, 20)] public float jumpForce = 10f;
    [Range(0, 10)] public float fallMultiplier = 3f;
    [Range(0, 20)] public float gravityMultiplier = 2.5f;

    [HideInInspector] public float speedMultiplier = 1f;
    [HideInInspector] public float additionalSpeed = 0f;
    #endregion

    #region Movement Buffer
    public float coyoteTime => 0.1f;
    public float jumpBufferTime => 0.1f;
    [HideInInspector]public float lastGroundedTime;
    [HideInInspector]public float lastJumpPressedTime;
    #endregion

    #region Ground Check
    [HideInInspector]public bool isGrounded = true;
    private float groundCheckDistance = 0.2f;

    private float CapsuleHeight => capsuleCollider ? capsuleCollider.height : 2f;
    private float CapsuleRadius => capsuleCollider ? capsuleCollider.radius : 0.5f;
    #endregion

    public MovementModule(Player owner) : base(owner) {}

    public override void Update()
    {
        base.Update();
        if (!Application.isPlaying || player == null) return;
CheckGrounded();
        JumpHandler();

        if (isGrounded)
        {
            lastGroundedTime = Time.time;
            RegenerateStamina();
        }
    }

    public override void FixedUpdate()
    {
        base.FixedUpdate();
        if (!Application.isPlaying || player == null) return;

        
        ApplyGravity();
        
        Move();
    }

    private void Move()
    {
        float h = Input.GetAxis("Horizontal");
        float v = Input.GetAxis("Vertical");

        if (player.canMove)
        {
            Vector3 move = (player.transform.right * h + player.transform.forward * v).normalized;

            // NOTE: Project uses custom Rigidbody.linearVelocity; keep to avoid breaking.
            rigidbody.linearVelocity = new Vector3(
                move.x * Speed,
                rigidbody.linearVelocity.y,
                move.z * Speed
            );

            if (animator != null)
            {
                animator.SetFloat("MoveX", h);
                animator.SetFloat("MoveY", v);
                animator.SetBool("isRun", h != 0 || v != 0);
            }
        }
        else if (animator != null)
        {
            animator.SetFloat("MoveX", 0);
            animator.SetFloat("MoveY", 0);
            animator.SetBool("isRun", false);
        }
    }

    public void JumpHandler()
    {
        if (Input.GetButtonDown("Jump"))
            lastJumpPressedTime = Time.time;

        if ((Time.time - lastJumpPressedTime) <= jumpBufferTime &&
            (Time.time - lastGroundedTime) <= coyoteTime &&
            isGrounded)
        {
            Jump();
            lastJumpPressedTime = -999f; // prevent double fire
        }
    }

    public void Jump()
    {
        if (animator) animator.SetTrigger("jump");

        // Reset vertical speed before impulse
        rigidbody.linearVelocity = new Vector3(
            rigidbody.linearVelocity.x, 0f, rigidbody.linearVelocity.z);

        rigidbody.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
        isGrounded = false;
    }

    private void ApplyGravity()
    {
        // Extra gravity for better feel
        if (rigidbody.linearVelocity.y <= 0f)
        {
            rigidbody.linearVelocity += Vector3.up * Physics.gravity.y * (fallMultiplier - 1f) * Time.deltaTime;
        }
        else if (rigidbody.linearVelocity.y > 0f && !Input.GetButton("Jump"))
        {
            rigidbody.linearVelocity += Vector3.up * Physics.gravity.y * (gravityMultiplier - 1f) * Time.deltaTime;
        }

        // Stick to ground when grounded
        if (isGrounded && rigidbody.linearVelocity.y < 0f)
        {
            rigidbody.linearVelocity = new Vector3(
                rigidbody.linearVelocity.x, -2f, rigidbody.linearVelocity.z);
        }
    }

    private void CheckGrounded()
    {
        if (capsuleCollider == null || player == null)
        {
            isGrounded = true;
            return;
        }

        Vector3 center = player.transform.position + capsuleCollider.center;
        float radius   = CapsuleRadius * 0.95f;
        float half     = CapsuleHeight * 0.5f - radius;

        Vector3 p1 = center + Vector3.up * half;
        Vector3 p2 = center - Vector3.up * half;

        var hits = Physics.CapsuleCastAll(
            p1, p2, radius, Vector3.down, groundCheckDistance,
            ~0, QueryTriggerInteraction.Ignore);

        isGrounded = false;
        foreach (var hit in hits)
        {
            if (hit.collider != capsuleCollider)
            {
                isGrounded = true;
                break;
            }
        }
    }

    private void RegenerateStamina()
    {
        if (!player.canGenerateStamina) return;

        var stat = player.Stat;
        if (stat == null) return;

        if (stat.currentstamina < stat.maxstamina)
        {
            stat.currentstamina += stat.staminaRegenRate * Time.deltaTime;
            if (stat.currentstamina > stat.maxstamina)
                stat.currentstamina = stat.maxstamina;
        }
    }

    public override void OnDrawGizmosSelected()
    {
        base.OnDrawGizmosSelected();
        if (capsuleCollider == null || player == null) return;

        Vector3 start  = player.transform.position + capsuleCollider.center;
        float radius   = CapsuleRadius * 0.95f;
        float half     = CapsuleHeight * 0.5f - radius;

        Vector3 p1 = start + Vector3.up * half;
        Vector3 p2 = start - Vector3.up * half - Vector3.up * groundCheckDistance;

        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(p1 - Vector3.up * groundCheckDistance, radius);
        Gizmos.DrawWireSphere(p2, radius);
    }
}

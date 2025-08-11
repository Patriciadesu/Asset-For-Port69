using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class EnemyAI : MonoBehaviour
{
    [Header("Target")]
    [HideInInspector] string playerTag = "Player";
    [HideInInspector] public float recheckTargetInterval = 0.25f;

    [Header("Ranges")]
    public float sightRange = 18f;
    public float attackRange = 2f;

    [Header("Patrol (radius roam)")]
    public float patrolRadius = 12f;
    [HideInInspector] public float patrolPointReachThreshold = 1f;
    [HideInInspector] public float patrolMinDistance = 2f;
    [HideInInspector] public float idleTime = 2f;

    [Header("Combat")]
    public int attackDamage = 10;
    [HideInInspector] public float attackWindup = 0f;
    public float attackCooldown = 1.1f;

    [Header("Animation")]
    public Animator anim; // ใส้เปล่าไว้ หากไม่เซ็ตใน Inspector จะหาอัตโนมัติ
    static readonly int HashSpeed     = Animator.StringToHash("Speed");
    static readonly int HashIsMoving  = Animator.StringToHash("IsMoving");
    static readonly int HashIsChasing = Animator.StringToHash("IsChasing");
    static readonly int HashAttack    = Animator.StringToHash("Attack");

    [Header("Debug")]
    [HideInInspector] public bool drawGizmos = true;

    [HideInInspector] public NavMeshAgent agent;
    [HideInInspector] public Transform player;
    [HideInInspector] public int patrolIndex;

    public StateMachine fsm;

    public EnemyIdleState  Idle       { get; private set; }
    public PatrolState     Patrol     { get; private set; }
    public ChaseState      Chase      { get; private set; }
    public AttackIdleState AttackIdle { get; private set; }
    public AttackState     Attack     { get; private set; }

    private float nextTargetCheckTime;
    private const float turnSpeed = 20f;

    [HideInInspector] public Vector3 homePos;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        agent.stoppingDistance = Mathf.Max(attackRange * 0.8f, 0.1f);

        // หา Animator อัตโนมัติถ้าไม่ได้อ้าง
        if (!anim) anim = GetComponentInChildren<Animator>();

        homePos = transform.position;

        fsm = new StateMachine();
        Idle       = new EnemyIdleState(this, fsm);
        Patrol     = new PatrolState(this, fsm);
        Chase      = new ChaseState(this, fsm);
        AttackIdle = new AttackIdleState(this, fsm);
        Attack     = new AttackState(this, fsm);
    }

    private void Start()
    {
        FindPlayer();
        fsm.SetState(Idle);
    }

    private void Update()
    {
        if (Time.time >= nextTargetCheckTime)
        {
            nextTargetCheckTime = Time.time + recheckTargetInterval;
            if (!player) FindPlayer();
        }
        fsm.Tick();

        // อัปเดตพารามิเตอร์อนิเมชันของการเคลื่อนที่ (Idle/Walk/Run Blend Tree)
        if (anim && agent)
        {
            float speed01 = agent.speed > 0.01f ? agent.velocity.magnitude / agent.speed : 0f;
            anim.SetFloat(HashSpeed, Mathf.Clamp01(speed01));
            anim.SetBool(HashIsMoving, agent.velocity.sqrMagnitude > 0.05f);
        }
    }

    #region Helpers
    public bool HasPatrol => patrolRadius > 0.1f;

    public Vector3 GetRoamPoint()
    {
        for (int i = 0; i < 8; i++)
        {
            Vector2 rnd = Random.insideUnitCircle * patrolRadius;
            Vector3 candidate = homePos + new Vector3(rnd.x, 0f, rnd.y);

            if (Vector3.Distance(transform.position, candidate) < patrolMinDistance)
                continue;

            if (NavMesh.SamplePosition(candidate, out NavMeshHit hit, 2.0f, NavMesh.AllAreas))
                return hit.position;
        }
        return homePos;
    }

    public bool Reached(Vector3 pos) =>
        Vector3.Distance(transform.position, pos) <= patrolPointReachThreshold;

    public bool CanSee(Transform t) =>
        t && Vector3.Distance(transform.position, t.position) <= sightRange;

    public bool InAttackRange(Transform t) =>
        t && Vector3.Distance(transform.position, t.position) <= attackRange;

    public void FaceTowards(Vector3 worldPos)
    {
        Vector3 dir = worldPos - transform.position; dir.y = 0f;
        if (dir.sqrMagnitude < 1e-4f) return;
        transform.rotation = Quaternion.Slerp(
            transform.rotation, Quaternion.LookRotation(dir), Time.deltaTime * 20f
        );
    }

    public void ApplyDamageToPlayer()
    {
        if (!player) return;
        player.SendMessage("TakeDamage", attackDamage, SendMessageOptions.DontRequireReceiver);
    }

    public void FindPlayer()
    {
        var obj = GameObject.FindGameObjectWithTag(playerTag);
        if (obj) player = obj.transform;
    }

    // ===== Animation helpers =====
    public void PlayAttackAnim()
    {
        if (anim) anim.SetTrigger(HashAttack);
    }
    public void SetChasingAnim(bool isChasing)
    {
        if (anim) anim.SetBool(HashIsChasing, isChasing);
    }
    // Animation Event (ใส่ในเฟรมโดนของคลิปโจมตี)
    public void AnimEvent_AttackHit()
    {
        ApplyDamageToPlayer();
    }

    private void OnDrawGizmosSelected()
    {
        if (!drawGizmos) return;
        Gizmos.color = Color.yellow; Gizmos.DrawWireSphere(transform.position, sightRange);
        Gizmos.color = Color.red;    Gizmos.DrawWireSphere(transform.position, attackRange);

        Vector3 center = Application.isPlaying ? homePos : transform.position;
        Gizmos.color = new Color(0f, 0.7f, 1f, 0.8f);
        if (patrolRadius > 0.1f)
            Gizmos.DrawWireSphere(center, patrolRadius);
    }
    #endregion
}

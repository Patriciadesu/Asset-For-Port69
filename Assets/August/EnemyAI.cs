using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class EnemyAI : MonoBehaviour
{
    [Header("Target")]
    public string playerTag = "Player";
    public float recheckTargetInterval = 0.25f;

    [Header("Ranges")]
    public float sightRange = 18f;   // ระยะตรวจจับ/เห็น
    public float attackRange = 2f;   // ระยะตี

    [Header("Patrol (optional)")]
    public Transform[] patrolPoints;
    public float patrolPointReachThreshold = 0.6f;
    public float idleTime = 2f;

    [Header("Combat")]
    public int   attackDamage  = 10;
    public float attackWindup  = 0.25f; // ใช้ใน AttackIdle
    public float attackCooldown = 1.1f; // หน่วงระหว่างการตี

    [Header("Debug")]
    public bool drawGizmos = true;

    [HideInInspector] public NavMeshAgent agent;
    [HideInInspector] public Transform player;
    [HideInInspector] public int patrolIndex;

    public StateMachine fsm;

    // โฮลด์อินสแตนซ์ของแต่ละสเตต (ชื่อเดิม)
    public EnemyIdleState  Idle       { get; private set; }
    public PatrolState     Patrol     { get; private set; }
    public ChaseState      Chase      { get; private set; }
    public AttackIdleState AttackIdle { get; private set; }
    public AttackState     Attack     { get; private set; }

    private float nextTargetCheckTime;
    private const float turnSpeed = 10f; // ใช้หมุนหาเป้าหมายแบบง่าย

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        agent.stoppingDistance = Mathf.Max(attackRange * 0.8f, 0.1f);

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
    }

    #region Helpers
    public bool HasPatrol => patrolPoints != null && patrolPoints.Length > 0;

    public Vector3 CurrentPatrolPoint =>
        HasPatrol && patrolPoints[patrolIndex] ? patrolPoints[patrolIndex].position : transform.position;

    public void AdvancePatrol()
    {
        if (HasPatrol) patrolIndex = (patrolIndex + 1) % patrolPoints.Length;
    }

    public bool Reached(Vector3 pos) =>
        Vector3.Distance(transform.position, pos) <= patrolPointReachThreshold;

    // เดิมชื่อ CanSee() แต่ย่อให้เหลือแค่เช็คระยะ
    public bool CanSee(Transform t) =>
        t && Vector3.Distance(transform.position, t.position) <= sightRange;

    public bool InAttackRange(Transform t) =>
        t && Vector3.Distance(transform.position, t.position) <= attackRange;

    public void FaceTowards(Vector3 worldPos)
    {
        Vector3 dir = worldPos - transform.position; dir.y = 0f;
        if (dir.sqrMagnitude < 1e-4f) return;
        transform.rotation = Quaternion.Slerp(
            transform.rotation, Quaternion.LookRotation(dir), Time.deltaTime * turnSpeed
        );
    }

    public void ApplyDamageToPlayer()
    {
        if (!player) return;
        // เรียกเมธอด TakeDamage(int) ถ้ามี (ไม่มีไม่พัง)
        player.SendMessage("TakeDamage", attackDamage, SendMessageOptions.DontRequireReceiver);
    }

    public void FindPlayer()
    {
        var obj = GameObject.FindGameObjectWithTag(playerTag);
        if (obj) player = obj.transform;
    }

    private void OnDrawGizmosSelected()
    {
        if (!drawGizmos) return;
        Gizmos.color = Color.yellow; Gizmos.DrawWireSphere(transform.position, sightRange);
        Gizmos.color = Color.red;    Gizmos.DrawWireSphere(transform.position, attackRange);
    }
    #endregion
}

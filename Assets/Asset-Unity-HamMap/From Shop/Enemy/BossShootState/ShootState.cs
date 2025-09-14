using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Playables;
using UnityEngine.Timeline;
using UnityEngine.UIElements;
using static NodeHelper.NodeUIHelpers;



[System.Serializable]
public class ShootState : BossState
{
    [Header("Projectile (3D)")]
    [SerializeField] private GameObject projectilePrefab;
    [SerializeField] private Transform  firePoint;
    [SerializeField, Min(0.01f)] private float projectileSpeed = 90f;
    [SerializeField, Min(0f)]    private float projectileDamage = 10f;
    [SerializeField, Min(0f)]    private float bulletLifetime   = 6f;

    [Header("Batch Settings")]
    [SerializeField, Min(1)] private int   bulletsPerBatch = 5;     // number of bullets in one batch
    [SerializeField, Min(0)] private float bulletInterval  = 0.18f; // time between bullets within the batch
    [SerializeField, Min(0)] private float batchCooldown   = 1.5f;  // cooldown after finishing the batch
    [SerializeField, Range(0f, 15f)] private float spreadDegrees   = 0f; // optional inaccuracy per bullet
    [SerializeField] private bool resampleTargetEachShot = true;    // re-aim every bullet

    [Header("Optional Timeline")]
    [SerializeField] private TimelineAsset timelinePlayable;

    // runtime
    private PlayableDirector director;
    private Coroutine batchCo;
    private bool isRunning;

    // player velocity snapshot
    private Vector3 _playerVel;
    private Vector3 _lastPos;
    private float   _lastTime;

    public ShootState(Boss bossInstance) : base("Shoot", bossInstance) { }

    public override void Enter()
    {
        base.Enter();

        // if on cooldown, do nothing and exit fast
        if (boss.shootInterval > 0f)
        {
            MarkFinished();
            return;
        }

        if (animator) animator.SetTrigger("Shoot");

        if (timelinePlayable != null)
        {
            director ??= boss.GetComponent<PlayableDirector>() ?? boss.gameObject.AddComponent<PlayableDirector>();
            if (director.playableAsset != timelinePlayable) director.playableAsset = timelinePlayable;
            director.time = 0;
            director.extrapolationMode = DirectorWrapMode.None;
            director.playOnAwake = false;
            director.Play();
        }

        // Start the whole batch and keep state alive until it completes
        isRunning = true;
        batchCo = boss.StartCoroutine(BatchCR());
    }

    public override void Update()
    {
        base.Update();
        UpdatePlayerVelocitySnapshot();
    }

    public override void Exit()
    {
        if (batchCo != null)
        {
            boss.StopCoroutine(batchCo);
            batchCo = null;
        }
        isRunning = false;

        if (director != null && director.state == PlayState.Playing)
            director.Stop();

        base.Exit();
    }

    // ----------------- Core -----------------
    private IEnumerator BatchCR()
    {
        var player = Player.Instance ? Player.Instance.transform : null;
        if (!player)
        {
            // no player: just finish
            StartCooldown();
            MarkFinished();
            yield break;
        }

        // fire N bullets with interval
        for (int i = 0; i < bulletsPerBatch; i++)
        {
            FireOne(player);

            if (i < bulletsPerBatch - 1 && bulletInterval > 0f)
                yield return new WaitForSeconds(bulletInterval);

            if (!resampleTargetEachShot)
                UpdatePlayerVelocitySnapshot(); // at least keep it fresh once
        }

        // after batch → cooldown
        StartCooldown();

        // end state after the batch actually finishes
        MarkFinished();
    }

    private void FireOne(Transform player)
    {
        Vector3 shooterPos = firePoint ? firePoint.position
                                       : boss.transform.position + boss.transform.forward * 1.2f;

        // lead: distance / bulletSpeed
        Vector3 playerPos = player.position;
        float   dist      = Vector3.Distance(shooterPos, playerPos);
        float   tLead     = projectileSpeed > 0.0001f ? dist / projectileSpeed : 0f;
        Vector3 predicted = playerPos + _playerVel * tLead;

        Vector3 dir = (predicted - shooterPos);
        if (dir.sqrMagnitude < 1e-6f) dir = boss.transform.forward;
        dir = dir.normalized;

        // optional spread
        if (spreadDegrees > 0f)
            dir = ApplySpread(dir, spreadDegrees);

        // spawn proj
        GameObject proj = projectilePrefab
            ? Object.Instantiate(projectilePrefab, shooterPos, Quaternion.LookRotation(dir))
            : CreateDefaultProjectile(shooterPos, dir);

        // launch
        if (!proj.TryGetComponent<Rigidbody>(out var rb))
            rb = proj.AddComponent<Rigidbody>();

        rb.useGravity = false;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.linearVelocity = dir * projectileSpeed;

        // if using EnemyProjectile API, arm it
        var ep = proj.GetComponent<EnemyProjectile>();
        if (ep != null)
            ep.Setup(dir, projectileSpeed, projectileDamage, bulletLifetime, boss);
        else
            proj.AddComponent<EnemyProjectile>().Setup(dir, projectileSpeed, projectileDamage, bulletLifetime, boss);
    }

    private Vector3 ApplySpread(Vector3 dir, float degrees)
    {
        // random yaw/pitch within ±degrees/2
        float half = degrees * 0.5f;
        float yaw   = Random.Range(-half, half);
        float pitch = Random.Range(-half, half);
        var q = Quaternion.AngleAxis(yaw, Vector3.up) * Quaternion.AngleAxis(pitch, Vector3.right);
        return (q * dir).normalized;
    }

    private void StartCooldown()
    {
        boss.StartShootCooldown(batchCooldown);
    }

    private void MarkFinished()
    {
        isFinished = true;
        boss.onAttackEnd?.Invoke();
        stage = StateStage.Exit;
    }

    private void UpdatePlayerVelocitySnapshot()
    {
        var p = Player.Instance;
        if (p == null) return;

        var t = p.transform;
        float now = Time.time;

        // Prefer Rigidbody velocity for accuracy
        if (t.TryGetComponent<Rigidbody>(out var rb3))
        {
            _playerVel = Vector3.Lerp(_playerVel, rb3.linearVelocity, 0.6f);
            _lastPos = t.position;
            _lastTime = now;
            return;
        }

        // else estimate from position
        if (_lastTime > 0f)
        {
            float dt = now - _lastTime;
            if (dt > 0.0001f)
            {
                var inst = (t.position - _lastPos) / dt;
                _playerVel = Vector3.Lerp(_playerVel, inst, 0.25f);
            }
        }
        _lastPos = t.position;
        _lastTime = now;
    }

    private GameObject CreateDefaultProjectile(Vector3 pos, Vector3 dir)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        go.name = "Bullet3D_Default";
        go.transform.SetPositionAndRotation(pos, Quaternion.LookRotation(dir));
        if (!go.TryGetComponent<Rigidbody>(out var rb))
            rb = go.AddComponent<Rigidbody>();
        rb.useGravity = false;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        go.GetComponent<SphereCollider>().isTrigger = true;
        var tr = go.AddComponent<TrailRenderer>();
        tr.time = 0.2f;
        tr.minVertexDistance = 0.01f;
        Object.Destroy(go, bulletLifetime > 0f ? bulletLifetime : 6f);
        return go;
    }

    // ---------------- Inspector UI (Editor only) ----------------
    public override void BuildInspectorUI(VisualElement container)
    {
#if UNITY_EDITOR
        base.BuildInspectorUI(container);
        container.Add(GameObjectField("Projectile Prefab", () => projectilePrefab, v => projectilePrefab = v));
        container.Add(TransformField ("Fire Point",        () => firePoint,       v => firePoint       = v));
        container.Add(FloatField     ("Projectile Speed",  () => projectileSpeed, v => projectileSpeed = Mathf.Max(0.01f, v)));
        container.Add(FloatField     ("Projectile Damage", () => projectileDamage,v => projectileDamage= Mathf.Max(0f, v)));
        container.Add(FloatField     ("Bullet Lifetime",   () => bulletLifetime,  v => bulletLifetime  = Mathf.Max(0f, v)));

        container.Add(IntField       ("Bullets / Batch",   () => bulletsPerBatch, v => bulletsPerBatch = Mathf.Max(1, v)));
        container.Add(FloatField     ("Bullet Interval",   () => bulletInterval,  v => bulletInterval  = Mathf.Max(0f, v)));
        container.Add(FloatField     ("Batch Cooldown",    () => batchCooldown,   v => batchCooldown   = Mathf.Max(0f, v)));
        container.Add(FloatField     ("Spread (deg)",      () => spreadDegrees,   v => spreadDegrees   = Mathf.Clamp(v, 0f, 15f)));
        container.Add(Toggle         ("Resample Each Shot",() => resampleTargetEachShot, v => resampleTargetEachShot = v));

        container.Add(TimelineField  ("Timeline (optional)", () => timelinePlayable, v => timelinePlayable = v));
#else
        _ = container;
#endif
    }
}

// -------------------- Boss helpers (cooldown timer) --------------------
public partial class Boss : MonoBehaviour
{
    [HideInInspector] public float shootInterval = 0f;
    private Coroutine shootCooldownCo;

    public void StartShootCooldown(float duration, UnityAction onReady = null)
    {
        if (duration <= 0f)
        {
            shootInterval = 0f;
            onReady?.Invoke();
            return;
        }
        if (shootCooldownCo != null)
            StopCoroutine(shootCooldownCo);
        shootCooldownCo = StartCoroutine(ShootCooldownCR(duration, onReady));
    }

    private IEnumerator ShootCooldownCR(float duration, UnityAction onReady)
    {
        shootInterval = duration;
        while (shootInterval > 0f)
        {
            shootInterval -= Time.deltaTime;
            yield return null;
        }
        shootInterval = 0f;
        onReady?.Invoke();
        shootCooldownCo = null;
    }
}

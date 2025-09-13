using UnityEngine;

[DisallowMultipleComponent]
public class EnemyProjectile : MonoBehaviour
{
    [Header("Basics")]
    public float damage   = 10f;
    public float speed    = 12f;
    public float lifeTime = 6f;

    // runtime
    private Vector3 _dir3D = Vector3.forward;
    private Rigidbody _rb;
    private Transform _ownerRoot;
    private bool _armed;
    private bool _destroyed;

    // ----------------- Public API (used by ShootState) -----------------
    public void Setup(Vector3 dir, float speed, float damage, float lifeTime, Component owner)
    {
        _dir3D     = dir.sqrMagnitude > 1e-6f ? dir.normalized : Vector3.forward;
        this.speed = Mathf.Max(0.01f, speed);
        this.damage = damage;
        this.lifeTime = lifeTime > 0f ? lifeTime : this.lifeTime;
        _ownerRoot = owner ? owner.transform.root : null;
        
        Cache();
        Arm();
    }

    // Optional 2D overload (no harm keeping it)
    public void Setup(Vector2 dir, float speed, float damage, float lifeTime, Component owner)
    {
        Setup(new Vector3(dir.x, dir.y, 0f), speed, damage, lifeTime, owner);
    }

    // ----------------- Unity -----------------
    private void Awake()
    {
        Cache();
        // If user forgot Setup(), arm with current forward/fields
        if (!_armed)
            Arm();
    }

    private void Update()
    {
        // Manual move if no Rigidbody present
        if (_rb == null && !_destroyed)
            transform.position += _dir3D * (speed * Time.deltaTime);
    }

    // ------------- Collision (destroy on ANY hit) -------------
    private void OnCollisionEnter(Collision c)      { HandleHit(c.collider.gameObject); }
    private void OnTriggerEnter(Collider other)     { HandleHit(other.gameObject); }

    // (If you sometimes use 2D physics, these keep it compatible)
    private void OnCollisionEnter2D(Collision2D c)  { HandleHit(c.collider.gameObject); }
    private void OnTriggerEnter2D(Collider2D other) { HandleHit(other.gameObject); }

    // ----------------- Internals -----------------
    private void Cache()
    {
        if (_rb == null) _rb = GetComponent<Rigidbody>();
    }

    private void Arm()
    {
        // Lifetime
        if (lifeTime > 0f) Destroy(gameObject, lifeTime);

        // Physics
        if (_rb != null)
        {
            _rb.isKinematic = false;
            _rb.useGravity = false;
            _rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            _rb.interpolation = RigidbodyInterpolation.Interpolate;
            _rb.linearVelocity = _dir3D * speed;
        }

        // Ignore owner collisions (prevents instant self-hit)
        IgnoreOwnerColliders();

        _armed = true;
    }

    private void HandleHit(GameObject other)
    {
        if (_destroyed || other == null) return;

        // Ignore hitting the owner
        if (IsOwner(other.transform)) return;

        // If it's the player, call TakeDamage(float)
        // 1) Prefer component
        var playerComp = other.GetComponentInParent<Player>();
        if (playerComp != null)
        {
            playerComp.Stat.TakeDamage(damage);
        }
        else
        {
            // 2) Fallbacks: tagged or is child of Player.Instance
            if (other.CompareTag("Player") && Player.Instance != null)
                 Player.Instance.Stat.TakeDamage(damage);
            else if (Player.Instance != null)
            {
                var pr = Player.Instance.transform;
                if (other.transform == pr || other.transform.IsChildOf(pr))
                     Player.Instance.Stat.TakeDamage(damage);
            }
        }

        // Destroy on ANY hit
        _destroyed = true;
        Destroy(gameObject);
    }

    private bool IsOwner(Transform t)
    {
        if (!_ownerRoot || !t) return false;
        return t == _ownerRoot || t.IsChildOf(_ownerRoot);
    }

    private void IgnoreOwnerColliders()
    {
        if (!_ownerRoot) return;

        // 3D
        var my3 = GetComponentsInChildren<Collider>(true);
        var ow3 = _ownerRoot.GetComponentsInChildren<Collider>(true);
        foreach (var a in my3)
        foreach (var b in ow3)
            if (a && b) Physics.IgnoreCollision(a, b, true);

        // 2D (harmless if not used)
        var my2 = GetComponentsInChildren<Collider2D>(true);
        var ow2 = _ownerRoot.GetComponentsInChildren<Collider2D>(true);
        foreach (var a in my2)
        foreach (var b in ow2)
            if (a && b) Physics2D.IgnoreCollision(a, b, true);
    }
}

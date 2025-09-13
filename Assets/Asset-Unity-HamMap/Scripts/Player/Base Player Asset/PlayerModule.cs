using UnityEngine;
using UnityEngine.Serialization;

[System.Serializable]
public class PlayerModule
{
    [FormerlySerializedAs("enebleModule")]
    public bool enableModule = true;

    protected Player player;
    protected Rigidbody rigidbody;
    protected Animator animator;
    protected CapsuleCollider capsuleCollider;

    private bool _isBound;

    public PlayerModule(Player owner)
    {
        player         = owner;
        rigidbody      = player?.rigidbody;
        animator       = player?.animator;
        capsuleCollider= player?.capsuleCollider;
    }

    /// <summary>Bind this module to a player and hook delegates (idempotent).</summary>
    public virtual void Bind(Player p)
    {
        if (_isBound) return;
        player          = p;
        rigidbody       = player?.rigidbody;
        animator        = player?.animator;
        capsuleCollider = player?.capsuleCollider;

        if (!enableModule || player == null) return;

        player.onStart         += Start;
        player.onUpdate        += Update;
        player.onFixedUpdate   += FixedUpdate;
        player.onTriggerEnter  += OnTriggerEnter;
        player.onTriggerStay   += OnTriggerStay;
        player.onTriggerExit   += OnTriggerExit;
        player.onCollisionEnter+= OnCollisionEnter;
        player.onCollisionStay += OnCollisionStay;
        player.onCollisionExit += OnCollisionExit;

        _isBound = true;
    }

    /// <summary>Unhook delegates (safe to call multiple times).</summary>
    public virtual void Unbind()
    {
        if (!_isBound || player == null) return;

        player.onStart          -= Start;
        player.onUpdate         -= Update;
        player.onFixedUpdate    -= FixedUpdate;
        player.onTriggerEnter   -= OnTriggerEnter;
        player.onTriggerStay    -= OnTriggerStay;
        player.onTriggerExit    -= OnTriggerExit;
        player.onCollisionEnter -= OnCollisionEnter;
        player.onCollisionStay  -= OnCollisionStay;
        player.onCollisionExit  -= OnCollisionExit;

        _isBound = false;
    }

    public virtual void Start()         { if (!enableModule) return; }
    public virtual void OnValidate()     { if (!enableModule) return; }
    public virtual void Update()         { if (!enableModule) return; }
    public virtual void FixedUpdate()    { if (!enableModule) return; }

    public virtual void OnTriggerEnter(Collider other)   { if (!enableModule) return; }
    public virtual void OnTriggerStay(Collider other)    { if (!enableModule) return; }
    public virtual void OnTriggerExit(Collider other)    { if (!enableModule) return; }

    public virtual void OnCollisionEnter(Collision c)    { if (!enableModule) return; }
    public virtual void OnCollisionStay(Collision c)     { if (!enableModule) return; }
    public virtual void OnCollisionExit(Collision c)     { if (!enableModule) return; }

    public virtual void OnDrawGizmosSelected()           { if (!enableModule) return; }
}

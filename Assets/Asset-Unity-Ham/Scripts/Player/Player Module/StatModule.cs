using UnityEngine;

public class StatModule : PlayerModule
{
    public bool canHit; // Meaning preserved
    [SerializeField, Range(0, 1000)] public float maxhealth = 100f;
    [SerializeField, Range(0, 1000)] public float maxstamina = 100f;
    [SerializeField, Range(0, 50)]   public float staminaRegenRate = 20f;

    public float currenthealth;
    [HideInInspector] public float currentstamina;
    [HideInInspector] public float damageReductionPercentage = 0f;

    public StatModule(Player owner) : base(owner) {}

    public override void Start()
    {
        base.Start();
        currenthealth  = maxhealth;
        currentstamina = maxstamina;
    }

    public void TakeDamage(float amount)
    {
        // Preserve original side effects but remove duplicates
        if (!canHit)
        {
            canHit = true;
            if (animator) animator.SetBool("isBlocking", false);
        }

        float reduced   = amount * (1f - Mathf.Clamp01(damageReductionPercentage / 100f));
        float damage    = Mathf.Max(reduced, 0f);

        currenthealth  -= damage;
        if (animator) animator.SetTrigger("GetHit");

        Debug.Log($"Player took damage: {amount}, Current Health: {currenthealth}");

        if (currenthealth <= 0f)
        {
            currenthealth = 0f;
            player?.Respawn();
        }
    }
}

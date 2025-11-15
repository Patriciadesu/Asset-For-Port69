using System.Collections;
using UnityEngine;

[AddComponentMenu("Killer AI/Modules/Beam Module")]
[Tooltip("Hitscan beam that deals damage to the player.")]
public class BeamModule : EnemyModule
{
    [Header("Beam Settings")]
    [Tooltip("Maximum range of the beam")]
    public float Range = 20f;

    [Tooltip("Amount of damage dealt by the beam")]
    public float BeamDamage = 20f;

    [Tooltip("Fire beam when entering attack state")]
    public bool FireOnAttackEnter = false;
    [Tooltip("Allow beams to randomly fire while the AI is in the Chase state.")]
    public bool FireDuringChase = true;
    [Tooltip("Seconds of cooldown after each beam fire.")]
    public float BeamCooldown = 3f;

    [Tooltip("Layer mask for beam raycasting")]
    public LayerMask HitLayers = -1; // All layers by default

    [Header("Visuals")]
    [Tooltip("Optional line renderer to visualize the beam. If empty, one will be created automatically.")]
    [SerializeField] private LineRenderer beamRenderer;
    [SerializeField, Tooltip("How long the beam visual stays visible after firing.")]
    private float beamFlashDuration = 0.12f;

    [Header("Chance Settings")]
    [SerializeField] private RandomTriggerSettings chaseTrigger = new RandomTriggerSettings
    {
        TriggerChance = 0.25f,
        Interval = new Vector2(2.5f, 4.5f),
        InitialDelay = new Vector2(0.5f, 1.5f)
    };

    private Coroutine hideBeamRoutine;
    private bool ownsRenderer;
    private static Material defaultBeamMaterial;
    private float nextBeamTime;

    public override void Initialize(KillerAI killer)
    {
        base.Initialize(killer);
        EnsureBeamRenderer();
        chaseTrigger?.Prime();
    }

    public override void OnStateEnter(EnemyState newState)
    {
        if (!IsActive) return;
        if (FireOnAttackEnter && newState == EnemyState.Attack)
        {
            FireBeam();
        }
    }

    public override void OnStateUpdate(EnemyState currentState)
    {
        if (!IsActive || !FireDuringChase || killer == null)
            return;

        if (currentState != EnemyState.Chase)
            return;

        if (Time.time < nextBeamTime)
            return;

        chaseTrigger?.PrimeIfNeeded();
        if (chaseTrigger != null && chaseTrigger.TryConsumeTrigger())
        {
            FireBeam();
            chaseTrigger.BlockFor(BeamCooldown);
        }
    }

    public void FireBeam()
    {
        Vector3 origin = transform.position + Vector3.up * 1.5f;
        Vector3 dir = transform.forward;
        Vector3 hitPoint = origin + dir * Range;

        // Raycast to detect hits
        if (Physics.Raycast(origin, dir, out RaycastHit hit, Range, HitLayers))
        {
            hitPoint = hit.point;

            Player player = Player.Instance;
            if (player != null && player.Stat != null)
            {
                player.Stat.TakeDamage(BeamDamage);
                Debug.Log($"[BeamModule] Beam hit player for {BeamDamage} damage!");
            }
            else
            {
                Debug.Log("[BeamModule] Beam hit object but no player or stat found.");
            }
        }
        else
        {
            Debug.Log("[BeamModule] Beam missed");
        }

        Debug.DrawRay(origin, dir * Range, Color.magenta, beamFlashDuration);
        ShowBeam(origin, hitPoint);
        nextBeamTime = Time.time + BeamCooldown;
    }

    private void EnsureBeamRenderer()
    {
        if (beamRenderer == null)
        {
            beamRenderer = GetComponent<LineRenderer>();
        }

        if (beamRenderer == null)
        {
            beamRenderer = gameObject.AddComponent<LineRenderer>();
            ownsRenderer = true;
        }

        beamRenderer.enabled = false;
        beamRenderer.positionCount = 2;
        beamRenderer.widthMultiplier = 0.05f;

        if (beamRenderer.sharedMaterial == null || ownsRenderer)
        {
            if (defaultBeamMaterial == null)
            {
                defaultBeamMaterial = new Material(Shader.Find("Sprites/Default"))
                {
                    color = Color.magenta
                };
            }
            beamRenderer.sharedMaterial = defaultBeamMaterial;
        }

        beamRenderer.startColor = Color.magenta;
        beamRenderer.endColor = new Color(1f, 0f, 1f, 0.4f);
        beamRenderer.useWorldSpace = true;
    }

    private void ShowBeam(Vector3 start, Vector3 end)
    {
        EnsureBeamRenderer();
        if (beamRenderer == null) return;

        beamRenderer.enabled = true;
        beamRenderer.SetPosition(0, start);
        beamRenderer.SetPosition(1, end);

        if (hideBeamRoutine != null)
        {
            StopCoroutine(hideBeamRoutine);
        }
        hideBeamRoutine = StartCoroutine(HideBeamAfterDelay());
    }

    private IEnumerator HideBeamAfterDelay()
    {
        yield return new WaitForSeconds(beamFlashDuration);
        if (beamRenderer != null)
        {
            beamRenderer.enabled = false;
        }
    }
}

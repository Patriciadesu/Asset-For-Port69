using UnityEngine;
using NaughtyAttributes;
using System.Linq;
public partial class Boss : MonoBehaviour
{
    private bool hasAttackState => stateGraph != null && stateGraph.transitionNodes != null &&
                               stateGraph.transitionNodes.Any(t => t.nextStates != null &&
                                                                   t.nextStates.Any(s => s != null && s.state is AttackState));
    [ShowIf("hasAttackState")] public Collider[] attackCollider;
    private void Awake()
    {
        // Weapon colliders: triggers, disabled by default (enabled in Attack state)
        if (attackCollider != null)
        {
            for (int i = 0; i < attackCollider.Length; i++)
            {
                var c = attackCollider[i];
                if (c == null) continue;
                c.isTrigger = true;
                c.enabled = false;
            }
        }
    }
    private bool IsInAttackState()
        => stateGraph != null && stateGraph.currentState != null && stateGraph.currentState.state is AttackState;

    private float GetCurrentAttackDamage()
        => stateGraph != null && stateGraph.currentState != null && stateGraph.currentState.state is AttackState a
           ? a.Damage
           : 0f;

    private void OnTriggerEnter(Collider other)
    {
        if (!IsInAttackState()) return;
        if (!other.CompareTag(PlayerTag)) return;

        float dmg = GetCurrentAttackDamage();
        if (dmg <= 0f) return;

        var targetGO = other.attachedRigidbody != null ? other.attachedRigidbody.gameObject : other.gameObject;
        // Default simple signature; your Player can implement a different TakeDamage; SendMessage is flexible
        targetGO.SendMessage("TakeDamage", dmg, SendMessageOptions.DontRequireReceiver);
    }

}
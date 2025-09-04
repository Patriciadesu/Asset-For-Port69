using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Timeline;
using UnityEngine.Playables;
using Unity.VisualScripting;
using UnityEngine.AI;
using System;
using UnityEngine.UIElements;
using static NodeHelper.NodeUIHelpers;
[System.Serializable]
public class BossTeleportState : BossState
{
    public enum TeleportPosition
    {
        OnTopOfPlayer,
        RandomAroundPlayer,
        BehindPlayer,
        RandomAroundBoss,
        BackToInitialPosition
    }
    private TeleportPosition teleportPosition = TeleportPosition.OnTopOfPlayer;

    public BossTeleportState(string name, Boss bossInstance) : base("Teleport", bossInstance)
    {

    }

    public override void Enter()
    {
        base.Enter();
        if (animator != null) animator.SetTrigger("Teleport");

        Vector3 targetPosition = Vector3.zero;
        switch (teleportPosition)
        {
            case TeleportPosition.OnTopOfPlayer:
                targetPosition = Player.Instance.transform.position + Vector3.up * boss.transform.position.y; // Teleport directly above player
                break;
            case TeleportPosition.RandomAroundPlayer:
                targetPosition = Player.Instance.transform.position + UnityEngine.Random.insideUnitSphere * 5f;
                targetPosition.y = 0; // Keep on ground
                break;
            case TeleportPosition.BehindPlayer:
                var playerDir = (Player.Instance.transform.position - boss.transform.position).normalized;
                targetPosition = Player.Instance.transform.position - playerDir * 3f + Vector3.up * 2f;
                break;
            case TeleportPosition.RandomAroundBoss:
                targetPosition = boss.transform.position + UnityEngine.Random.insideUnitSphere * 5f;
                targetPosition.y = 0; // Keep on ground
                break;
            case TeleportPosition.BackToInitialPosition:
                boss.ResetTransform();
                targetPosition = boss.initialPosition;
                break;
        }

        boss.transform.position = targetPosition;
        isFinished = true; // Mark as finished immediately
        boss.onAttackEnd.Invoke(); // Trigger any attack end logic immediately after teleport
    }

    public override void BuildInspectorUI(VisualElement container)
    {
        base.BuildInspectorUI(container);
        container.Add(EnumField<TeleportPosition>("Teleport To", () => this.teleportPosition, v => this.teleportPosition = v));
    }

}

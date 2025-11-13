using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Playables;
using UnityEngine.Timeline;
using UnityEngine.UIElements;
using static NodeHelper.NodeUIHelpers;

[System.Serializable]
public class PhaseChangeState : BossState
{
    public float newAttackRange;
    public float newSightRange;
    public float newSpeed;
    public float newAttackAnimationSpeedMultiplier;
    public TimelineAsset timelinePlayable;

    private PlayableDirector director;

    public PhaseChangeState(Boss bossInstance) : base("Phase Change", bossInstance) { }

    public override void Enter()
    {
        base.Enter();

        if (animator) animator.SetTrigger("PhaseChange");

        if (timelinePlayable != null)
        {
            director ??= boss.GetComponent<PlayableDirector>() ?? boss.gameObject.AddComponent<PlayableDirector>();
            if (director.playableAsset != timelinePlayable) director.playableAsset = timelinePlayable;
            director.time = 0;
            director.extrapolationMode = DirectorWrapMode.None;
            director.playOnAwake = false;
            director.Play();
            boss.StartCoroutine(ChangePhase());
        }
        else
        {
            boss.onAttackEnd.Invoke();
        }
    }
    public override void Update()
    {
        base.Update();
    }

    public IEnumerator ChangePhase()
    {
        yield return new WaitForSeconds((float)director.playableAsset.duration);
        boss.onAttackEnd.Invoke();
    }

}
using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Playables;
using UnityEngine.Timeline;

[System.Serializable]
public class ShootState : BossState
{
    [Header("Shoot Settings")]
    public GameObject projectilePrefab;   // Prefab of projectile
    public Transform shootPoint;          // Spawn position
    public float projectileSpeed = 15f;
    public float shootCooldown = 2;
    public TimelineAsset timelinePlayable; // Optional animation timeline

    private PlayableDirector director;
    private bool hasShot;
    private bool endedOnce;

    public ShootState(Boss bossInstance) : base("Shoot", bossInstance) { }

    public override void Enter()
    {
        base.Enter();

        // Initialize sentinel the first time
        if (boss.shootInterval == -999) boss.shootInterval = shootCooldown;

        // If interval <= 0, we can shoot now; otherwise we already shot and are cooling down
        hasShot = boss.shootInterval > 0f;
        endedOnce = false;

        if (animator != null)
            animator.SetTrigger("Shoot");

        // Optional timeline (anim)
        director = boss.GetComponent<PlayableDirector>();
        if (director == null) director = boss.gameObject.AddComponent<PlayableDirector>();
        if (timelinePlayable != null)
        {
            director.playOnAwake = false;
            director.extrapolationMode = DirectorWrapMode.None;
            director.playableAsset = timelinePlayable;
            director.RebuildGraph();
            director.Evaluate();
            director.Play();
        }
    }

    public override void Update()
    {
        base.Update();

        if (!hasShot)
        {
            hasShot = true;
            boss.shootInterval = shootCooldown;

            ShootProjectile();

            // Start ticking cooldown; at the end allow next shot on next entry
            boss.StartCoroutine(boss.ShootCooldown(() => hasShot = false));

            OnShootEnd(); // finish this state immediately after one shot
        }
        else
        {
            // We are still cooling down on this entry -> finish state without spamming events
            OnShootEnd();
        }
    }

    private void ShootProjectile()
    {
        Transform player = Player.Instance != null ? Player.Instance.transform : null;
        if (player == null) return;

        Vector3 shootPos = shootPoint ? shootPoint.position : boss.transform.position + (boss.transform.forward * 2);
        Vector3 dir = (player.position - shootPos).normalized;

        GameObject proj;
        if (projectilePrefab)
        {
            proj = GameObject.Instantiate(projectilePrefab, shootPos, Quaternion.LookRotation(dir));
        }
        else
        {
            proj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            proj.transform.position = shootPos;
            proj.transform.rotation = Quaternion.LookRotation(dir);
        }

        if (proj.TryGetComponent<Rigidbody>(out Rigidbody rb))
        {
            rb.AddForce(dir * projectileSpeed, ForceMode.Impulse);
        }
    }

    private void OnShootEnd()
    {
        if (endedOnce) return;
        endedOnce = true;

        isFinished = true;
        boss.onAttackEnd?.Invoke();
        stage = StateStage.Exit; // <-- important: stop updating this state
    }

    public override void Exit()
    {
        if (director != null && director.state == PlayState.Playing)
            director.Stop();

        base.Exit();
    }
}



public partial class Boss : MonoBehaviour
{
    public float shootInterval = -999;

    public IEnumerator ShootCooldown(UnityAction callback)
    {
        while (shootInterval > 0)
        {
            shootInterval -= 0.1f;
            yield return new WaitForSeconds(0.1f);
        }
        callback.Invoke();
    }
}
using UnityEngine;

[System.Serializable]
public class RotateToPlayerState : BossState
{
    [Header("Rotation Settings")]
    public float rotationSpeed = 5f;   // How fast boss turns to player
    public float angleThreshold = 5f;  // How close (in degrees) is considered "facing player"

    private Transform _self;
    private Transform _player;
    private bool endedOnce;

    public RotateToPlayerState(Boss bossInstance) : base("RotateToPlayer", bossInstance) { }

    public override void BindRuntime(Boss bossInstance)
    {
        base.BindRuntime(bossInstance);
        _self = boss != null ? boss.transform : null;
        _player = Player.Instance != null ? Player.Instance.transform : null;
    }

    public override void Enter()
    {
        base.Enter();
        isFinished = false;
        endedOnce = false;
    }

    public override void Update()
    {
        base.Update();
        if (_self == null || _player == null) return;

        // Direction to player (ignore Y so boss rotates only horizontally)
        Vector3 dir = (_player.position - _self.position).normalized;
        dir.y = 0f;

        if (dir.sqrMagnitude < 0.0001f) return;

        // Smoothly rotate toward target
        Quaternion targetRot = Quaternion.LookRotation(dir, Vector3.up);
        _self.rotation = Quaternion.Slerp(_self.rotation, targetRot, rotationSpeed * Time.deltaTime);

        // Check if rotation is close enough
        float angle = Quaternion.Angle(_self.rotation, targetRot);
        if (angle <= angleThreshold)
        {
            OnRotationComplete();
        }
    }

    private void OnRotationComplete()
    {
        if (endedOnce) return;
        endedOnce = true;

        isFinished = true;
        if (boss.onAttackEnd != null)
            boss.onAttackEnd.Invoke();
    }
}

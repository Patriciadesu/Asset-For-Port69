using UnityEngine;

public class WalkSound : PlayerExtension
{
    [Header("Footstep Settings")]
    public AudioSource audioSource;
    public AudioClip[] footstepClips;
    public float stepInterval = 0.5f; // Time between footsteps
    public float runStepMultiplier = 0.6f; // Faster steps when running

    private float _stepCycle;
    private float _nextStep;

    public override void OnStart(Player player)
    {
        base.OnStart(player);

        if (audioSource == null)
        {
            audioSource = player.gameObject.AddComponent<AudioSource>();
            audioSource.spatialBlend = 1f; // 3D sound
        }

        player.onUpdate += HandleFootsteps;
    }

    private void HandleFootsteps()
    {
        // Only play footsteps if player is grounded and moving
        if (!_player.Movement.isGrounded) return;

        float moveX = Input.GetAxis("Horizontal");
        float moveY = Input.GetAxis("Vertical");

        bool isMoving = Mathf.Abs(moveX) > 0.1f || Mathf.Abs(moveY) > 0.1f;

        if (isMoving) return;

        float speed = _player.Movement.Speed;
        float interval = stepInterval;

        // If running, footsteps come faster
        if (_player.animator.GetBool("isRun"))
            interval *= runStepMultiplier;

        _stepCycle += Time.deltaTime * speed;

        if (_stepCycle > _nextStep)
        {
            PlayFootstep();
            _nextStep = _stepCycle + interval;
        }
    }

    private void PlayFootstep()
    {
        if (footstepClips.Length == 0) return;

        int index = Random.Range(0, footstepClips.Length);
        audioSource.clip = footstepClips[index];
        audioSource.Play();
    }
}

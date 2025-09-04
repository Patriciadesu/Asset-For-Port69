using System.Collections;
using NaughtyAttributes;
using UnityEngine;
using Unity.Cinemachine;



public class BowShot : PlayerExtension
{
    [Header("Input")]
    public KeyCode activateKey = KeyCode.Mouse0;

    [Range(0.05f, 0.95f)] public float pauseTime = 0.4f;
    [Min(0.05f)] public float DrawTime = 0.6f;

    private bool canShot = false;
    private bool isHolding = false;
    private bool isPaused = false;
    private float clipLength = 0f;

    private float holdStartTime;
    private float timeHeldSoFar;

    [Header("Arrow")]
    public GameObject projectilePrefab;
    private Transform spawnPoint;
    public float speed = 30f;
    public float damage = 10f;

    public bool hasDestroyTime = true;
    [ShowIf(nameof(hasDestroyTime))] public float DestroyTime = 2.5f;

    [Header("Auto camera switching")]
    public bool switchBackToThirdPerson = true;
    [Min(0f)] public float switchBackDelay = 0.05f;
    [Min(0.05f)] public float returnLerpTimeMin = 0.05f;

    public override void OnStart(Player player)
    {
        base.OnStart(player);
        CacheClipLength();

        if (projectilePrefab == null)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            go.transform.localScale = new Vector3(0.05f, 0.02f, 0.5f);
            Rigidbody arrowRb = go.AddComponent<Rigidbody>();
            arrowRb.constraints = RigidbodyConstraints.FreezeRotation;
            projectilePrefab = go;
        }

        if (spawnPoint == null)
        {
            spawnPoint = new GameObject("ArrowSpawnPoint").transform;
        }
        if (_player != null && _player.camera != null)
        {
            spawnPoint.SetParent(_player.camera.transform, false);
            spawnPoint.localPosition = new Vector3(0.0f, -0.05f, 0.4f);
            spawnPoint.localRotation = Quaternion.identity;
        }
    }

    void Update()
    {
        CheckHolding();

        if (Input.GetKeyDown(activateKey))
            StartDraw();

        if (Input.GetKeyUp(activateKey))
            ReleaseOrCancel();
    }

    private void StartDraw()
    {
        if (canShot) return;

        if (_player != null && _player.cameraType == Player.CameraType.ThirdPerson)
        {
            // Start forward lerp to FP; this coroutine is now preemptible
            _player.StartCoroutine(_player.LerpTpCamToFpThenEnableFp(DrawTime));
        }

        isHolding = true;
        isPaused = false;
        canShot = false;

        holdStartTime = Time.time;
        timeHeldSoFar = 0f;

        float p = Mathf.Clamp01(pauseTime);
        float L = Mathf.Max(clipLength, 0.0001f);
        float T = Mathf.Max(DrawTime, 0.0001f);
        float speedForDraw = (p * L) / T;

        if (_player != null && _player.animator != null)
        {
            _player.animator.speed = Mathf.Max(speedForDraw, 0.001f);
            _player.animator.Play("BowShot", 0, 0f);
        }
    }

    private void CheckHolding()
    {
        if (!isHolding || isPaused) return;
        timeHeldSoFar = Time.time - holdStartTime;

        if (_player == null || _player.animator == null) return;
        var info = _player.animator.GetCurrentAnimatorStateInfo(0);
        if (!info.IsName("BowShot")) return;

        float t = info.normalizedTime % 1f;
        if (t + Time.deltaTime >= pauseTime)
        {
            _player.animator.speed = 0f;
            isPaused = true;
            canShot = true;
        }
    }

    private void ReleaseOrCancel()
    {
        if (canShot)
        {
            canShot = false;
            isHolding = false;

            if (_player != null && _player.animator != null)
                _player.animator.speed = 1f;

            Shoot();

            if (switchBackToThirdPerson)
                _player.StartCoroutine(SwitchBackToTPSAfterDelay());
        }
        else
        {
            CancelDraw();
        }
    }

    private void CancelDraw()
    {
        isHolding = false;
        isPaused = false;

        if (_player != null && _player.animator != null)
        {
            _player.animator.speed = 1f;
            _player.animator.Play("Idle", 0, 0f);
        }

        // 🔑 NEW: actively interrupt the forward lerp and reverse immediately
        if (_player != null)
        {
            float backTime = Mathf.Max(returnLerpTimeMin, timeHeldSoFar);
            _player.InterruptCamLerpAndReturn(backTime);
        }
    }

    private System.Collections.IEnumerator SwitchBackToTPSAfterDelay()
    {
        if (switchBackDelay > 0f) yield return new WaitForSeconds(switchBackDelay);

        if (_player != null && _player.cameraType == Player.CameraType.FirstPerson)
        {
            float backTime = Mathf.Max(returnLerpTimeMin, DrawTime * 0.5f);
            yield return _player.LerpBackToTpFromFp(backTime);
        }
    }

    private void Shoot()
    {
        if (projectilePrefab == null || spawnPoint == null)
        {
            Debug.LogWarning("[BowShot] Missing projectilePrefab or spawnPoint.");
            return;
        }

        var projectile = Object.Instantiate(projectilePrefab, spawnPoint.position, spawnPoint.rotation);
        if (hasDestroyTime) Object.Destroy(projectile, DestroyTime);

        var rb = projectile.GetComponent<Rigidbody>();
        if (rb == null) rb = projectile.AddComponent<Rigidbody>();
        rb.linearVelocity = spawnPoint.forward * speed;

        var arrow = projectile.GetComponent<Arrow>();
        if (arrow == null) arrow = projectile.AddComponent<Arrow>();
        arrow.SetUp(damage);
    }

    private void CacheClipLength()
    {
        if (_player == null || _player.animator == null) return;
        var controller = _player.animator.runtimeAnimatorController;
        if (controller == null) return;

        clipLength = 0f;
        foreach (var c in controller.animationClips)
        {
            if (c && c.name == "BowShot") { clipLength = c.length; break; }
        }
        if (clipLength <= 0f)
            Debug.LogWarning("[BowShot] Animation clip 'BowShot' not found or length is zero.");
    }
    public void OnDestroy()
    {
        DestroyImmediate(spawnPoint);
    }
}






public partial class Player
{
    // Cinemachine support (found anywhere under TPS camera hierarchy)
    private CinemachineBrain _tpsBrain;

    // Lerp state
    private bool _camLerping = false;
    private bool _handedOffToFP = false;   // becomes true when we actually activate FP cam
    private Coroutine _camLerpRoutine;

    // Cached TPS pose (where to return on cancel/back)

    private CinemachineBrain FindTpsBrain()
    {
        if (_tpsBrain != null) return _tpsBrain;
        if (tpsCamera != null)
            _tpsBrain = tpsCamera.GetComponentInChildren<CinemachineBrain>(true);
        if (_tpsBrain == null)
            _tpsBrain = GetComponentInChildren<CinemachineBrain>(true);
        return _tpsBrain;
    }

    private void StopCurrentCamLerp()
    {
        if (_camLerpRoutine != null)
        {
            StopCoroutine(_camLerpRoutine);
            _camLerpRoutine = null;
        }
        _camLerping = false;
    }

    /// <summary>
    /// Start a forward blend: Lerp the TPS camera to the FP pivot over 'duration', then enable FP.
    /// If another lerp is in progress, it is preempted.
    /// </summary>
    public IEnumerator LerpTpCamToFpThenEnableFp(float duration)
    {
        // Preempt any running lerp
        StopCurrentCamLerp();

        if (tpsCamera == null || fpsCameraPivot == null || camera == null)
            yield break;

        _handedOffToFP = false;
        _camLerping = true;

        // Disable CM so our manual motion isn't overridden
        var brain = FindTpsBrain();
        if (brain != null) brain.enabled = false;

        // Run as a managed coroutine so we can cancel it mid-flight
        _camLerpRoutine = StartCoroutine(DoLerpTpsToFp(duration, onCompleted: () =>
        {
            // Hand-off to FP cam only if we weren't interrupted
            if (_camLerping)
            {
                tpsCamera.gameObject.SetActive(false);
                camera.gameObject.SetActive(true);
                camera.transform.SetPositionAndRotation(fpsCameraPivot.position, fpsCameraPivot.rotation);
                cameraType = CameraType.FirstPerson;
                _handedOffToFP = true;
            }

            _camLerping = false;
            _camLerpRoutine = null;
        }));

        yield return _camLerpRoutine; // wait if caller yields on us
    }

    private IEnumerator DoLerpTpsToFp(float duration, System.Action onCompleted)
    {
        Vector3 startPos = tpsCamera.transform.position;
        Quaternion startRot = tpsCamera.transform.rotation;
        Vector3 endPos = fpsCameraPivot.position;
        Quaternion endRot = fpsCameraPivot.rotation;

        duration = Mathf.Max(0.0001f, duration);
        float t = 0f;

        while (t < 1f)
        {
            // If preempted, bail immediately (caller will start reverse, if needed)
            if (_camLerping == false) yield break;

            float s = t; // linear; swap to SmoothStep if desired: s = s * s * (3f - 2f * s);
            tpsCamera.transform.SetPositionAndRotation(
                Vector3.Lerp(startPos, endPos, s),
                Quaternion.Slerp(startRot, endRot, s)
            );
            t += Time.deltaTime / duration;
            yield return null;
        }

        // Snap to exact end before completion callback
        tpsCamera.transform.SetPositionAndRotation(endPos, endRot);
        onCompleted?.Invoke();
    }

    /// <summary>
    /// Called when the draw is canceled before the hold point.
    /// If the forward lerp is still running (no handoff yet), it is interrupted and we
    /// reverse back to the cached TPS pose immediately.
    /// If FP was already handed off (rare for a cancel), we use the normal FP->TPS return.
    /// </summary>
    public void InterruptCamLerpAndReturn(float duration)
    {
        duration = Mathf.Max(0.0001f, duration);

        // Case 1: We are in the middle of forward lerp and haven't enabled FP yet.
        if (_camLerping && !_handedOffToFP)
        {
            // Interrupt forward lerp right now
            _camLerping = false; // signals DoLerpTpsToFp to abort
            StopCurrentCamLerp();

            // Ensure FP is NOT active (we never handed off)
            if (camera != null) camera.gameObject.SetActive(false);
            if (tpsCamera != null) tpsCamera.gameObject.SetActive(true);
            cameraType = CameraType.ThirdPerson;

            // Now reverse from CURRENT TPS camera pose back to cached pose
            _camLerpRoutine = StartCoroutine(DoLerpTpsCurrentToCached(duration, onCompleted: () =>
            {
                // Re-enable Cinemachine to resume normal follow/orbit
                var brain = FindTpsBrain();
                if (brain != null) brain.enabled = true;

                _camLerpRoutine = null;
            }));
            return;
        }

        // Case 2: FP was already active — use the standard return path
        if (cameraType == CameraType.FirstPerson)
        {
            StopCurrentCamLerp();
            _camLerpRoutine = StartCoroutine(LerpBackToTpFromFp(duration));
        }
    }

    /// <summary>
    /// Smoothly lerp the TPS camera from its *current* pose back to the cached TPS pose,
    /// then re-enable Cinemachine.
    /// </summary>
    private IEnumerator DoLerpTpsCurrentToCached(float duration, System.Action onCompleted)
    {
        _camLerping = true;

        Vector3 startPos = tpsCamera.transform.position;
        Quaternion startRot = tpsCamera.transform.rotation;
        Vector3 endPos = tpsVirtualCamera.transform.position;
        Quaternion endRot = tpsVirtualCamera.transform.rotation;

        float t = 0f;
        while (t < 1f)
        {
            if (_camLerping == false) yield break;

            float s = t;
            tpsCamera.transform.SetPositionAndRotation(
                Vector3.Lerp(startPos, endPos, s),
                Quaternion.Slerp(startRot, endRot, s)
            );
            t += Time.deltaTime / duration;
            yield return null;
        }

        tpsCamera.transform.SetPositionAndRotation(endPos, endRot);

        _camLerping = false;
        onCompleted?.Invoke();
    }

    /// <summary>
    /// Normal FP->TPS return (after successful shot, or if FP is already active somehow).
    /// </summary>
    public IEnumerator LerpBackToTpFromFp(float duration)
    {
        // Make TPS visible, hide FP
        tpsCamera.gameObject.SetActive(true);
        camera.gameObject.SetActive(false);
        cameraType = CameraType.ThirdPerson;

        // Lerp from current TPS pose (which should be near FP pivot) back to cached TPS pose
        yield return DoLerpTpsCurrentToCached(duration, onCompleted: () =>
        {
            var brain = FindTpsBrain();
            if (brain != null) brain.enabled = true;
        });
    }
}

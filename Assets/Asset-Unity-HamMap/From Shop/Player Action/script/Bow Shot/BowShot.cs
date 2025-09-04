using System.Collections;
using NaughtyAttributes;
using UnityEngine;
using Unity.Cinemachine;
using UnityEngine.UI;


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
    [ShowAssetPreview(128, 128)] public GameObject projectilePrefab;
    private Transform spawnPoint;
    public float speed = 30f;
    public float damage = 10f;

    public bool hasDestroyTime = true;
    [ShowIf(nameof(hasDestroyTime))] public float DestroyTime = 2.5f;

    [Header("Aiming Mode"),ShowIf("playerCameraType",Player.CameraType.ThirdPerson)]
    [Tooltip("If true, lerp TPS -> FPS while drawing. If false, stay TPS and shoot via crosshair center ray.")]
    public bool aimZooming = true;

    private bool switchBackToThirdPerson = true;
    private float switchBackDelay = 0.05f;
    private float returnLerpTimeMin = 0.05f;

    [Header("Crosshair")]
    [Tooltip("Assign a RectTransform for a custom crosshair. If empty, a default will be created.")]
    [ShowAssetPreview]public RectTransform crosshairRoot;
    private bool showCrosshairOnlyWhenAiming = true;
    [Min(2f)] public float crosshairSize = 12f;
    [Min(1f)] public float crosshairThickness = 2f;
    public Color crosshairColor = Color.white;

    private Canvas _ownedCanvas;
    private bool _ownsCrosshairRoot = false;
    private static Sprite _pixelSprite;

    private Player.CameraType playerCameraType => Player.Instance.cameraType;

    public override void OnStart(Player player)
    {
        base.OnStart(player);
        CacheClipLength();

        if (spawnPoint == null)
            spawnPoint = new GameObject("ArrowSpawnPoint").transform;

        UpdateSpawnPointToActiveCamera();

        EnsureCrosshair();
        SetCrosshairVisible(!showCrosshairOnlyWhenAiming);
    }

    private void UpdateSpawnPointToActiveCamera()
    {
        var camTf = GetActiveCameraTransform();
        if (camTf == null) return;

        spawnPoint.SetParent(camTf, false);
        spawnPoint.localPosition = new Vector3(0f, -0.05f, 0.4f);
        spawnPoint.localRotation = Quaternion.identity;
    }

    private Transform GetActiveCameraTransform()
    {
        // Prefer whatever is currently the MainCamera (active render camera)
        if (Camera.main != null) return Camera.main.transform;
        // Fallback to player's FP camera if provided
        if (_player != null && _player.camera != null) return _player.camera.transform;
        return null;
    }

    GameObject CreateDefaultArrow()
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        go.transform.localScale = new Vector3(0.05f, 0.02f, 0.5f);
        var rb = go.AddComponent<Rigidbody>();
        rb.constraints = RigidbodyConstraints.FreezeRotation;
        return go;
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

        // Decide whether to lerp TPS -> FPS based on toggle
        if (aimZooming && _player != null && _player.cameraType == Player.CameraType.ThirdPerson)
        {
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

        if (showCrosshairOnlyWhenAiming) SetCrosshairVisible(true);
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

            if (showCrosshairOnlyWhenAiming) SetCrosshairVisible(false);

            // Only switch back if we actually zoomed in
            if (aimZooming && switchBackToThirdPerson)
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

        // Only interrupt/return camera if we zoomed
        if (aimZooming && _player != null)
        {
            float backTime = Mathf.Max(returnLerpTimeMin, timeHeldSoFar);
            _player.InterruptCamLerpAndReturn(backTime);
        }

        if (showCrosshairOnlyWhenAiming) SetCrosshairVisible(false);
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
        // Make sure spawn point follows the currently active camera just before firing
        UpdateSpawnPointToActiveCamera();

        // Compute aim direction from the center of the active camera (TPS or FPS)
        var camTf = GetActiveCameraTransform();
        Vector3 dir = spawnPoint.forward;
        if (camTf != null)
        {
            Ray ray = new Ray(camTf.position, camTf.forward);
            if (Physics.Raycast(ray, out var hit, 1000f, ~0, QueryTriggerInteraction.Ignore))
                dir = (hit.point - spawnPoint.position).normalized;
            else
                dir = (camTf.position + camTf.forward * 1000f - spawnPoint.position).normalized;
        }

        GameObject projectile = projectilePrefab == null
            ? CreateDefaultArrow()
            : Instantiate(projectilePrefab);

        projectile.transform.position = spawnPoint.position;
        projectile.transform.rotation = Quaternion.LookRotation(dir, Vector3.up);

        if (hasDestroyTime) Destroy(projectile, DestroyTime);

        var rb = projectile.GetComponent<Rigidbody>();
        if (rb == null) rb = projectile.AddComponent<Rigidbody>();
        rb.linearVelocity = dir * speed;

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
        DestroyImmediate(spawnPoint.gameObject);
        DestroyImmediate(crosshairRoot.gameObject);
        DestroyImmediate(_ownedCanvas.gameObject);
    }

    // -------- Crosshair helpers --------

    private void EnsureCrosshair()
    {
        if (crosshairRoot != null) return;

        var canvasGO = new GameObject("BowShot_Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        _ownedCanvas = canvasGO.GetComponent<Canvas>();
        _ownedCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        _ownedCanvas.sortingOrder = 5000;

        var scaler = canvasGO.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);

        var crossGO = new GameObject("Crosshair", typeof(RectTransform));
        crosshairRoot = crossGO.GetComponent<RectTransform>();
        crosshairRoot.SetParent(canvasGO.transform, false);
        crosshairRoot.anchorMin = crosshairRoot.anchorMax = new Vector2(0.5f, 0.5f);
        crosshairRoot.pivot = new Vector2(0.5f, 0.5f);
        crosshairRoot.anchoredPosition = Vector2.zero;

        BuildDefaultCrosshair(crosshairRoot);
        _ownsCrosshairRoot = true;
    }

    private void SetCrosshairVisible(bool visible)
    {
        if (crosshairRoot != null)
            crosshairRoot.gameObject.SetActive(visible);
    }

    private void BuildDefaultCrosshair(RectTransform parent)
    {
        CreateUIRect(parent, "Dot", new Vector2(crosshairThickness, crosshairThickness), Vector2.zero);

        float half = crosshairSize;
        float gap = half * 0.6f;

        CreateUIRect(parent, "Up",    new Vector2(crosshairThickness, half), new Vector2(0,  gap + half * 0.5f));
        CreateUIRect(parent, "Down",  new Vector2(crosshairThickness, half), new Vector2(0, -gap - half * 0.5f));
        CreateUIRect(parent, "Left",  new Vector2(half, crosshairThickness), new Vector2(-gap - half * 0.5f, 0));
        CreateUIRect(parent, "Right", new Vector2(half, crosshairThickness), new Vector2( gap + half * 0.5f, 0));
    }

    private Image CreateUIRect(Transform parent, string name, Vector2 size, Vector2 anchoredPos)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        var rt = go.GetComponent<RectTransform>();
        rt.sizeDelta = size;
        rt.anchoredPosition = anchoredPos;

        var img = go.GetComponent<Image>();
        img.sprite = GetPixelSprite();
        img.color = crosshairColor;
        img.raycastTarget = false;

        return img;
    }

    private static Sprite GetPixelSprite()
    {
        if (_pixelSprite != null) return _pixelSprite;
        var tex = new Texture2D(1, 1, TextureFormat.RGBA32, false);
        tex.SetPixel(0, 0, Color.white);
        tex.Apply();
        _pixelSprite = Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f));
        return _pixelSprite;
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

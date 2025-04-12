using UnityEngine;

public class GrapplingHook : PlayerExtension
{
    [Header("References")]
    public Transform cam;
    public Transform gunTip;
    public LayerMask whatIsGrappleable;
    public LineRenderer lr;

    [Header("Grappling")]
    public float maxGrappleDistance = 25f;
    public float grappleDelayTime = 0.5f;
    public float overshootYAxis = 2f;

    private Vector3 grapplePoint;

    [Header("Cooldown")]
    public float grapplingCd = 2.5f;
    private float grapplingCdTimer;

    [Header("Input")]
    public KeyCode grappleKey = KeyCode.Mouse1;

    private bool grappling;

    public override void OnStart(PlayerController player)
    {
        base.OnStart(player);
        lr.enabled = false;
    }

    public void Update()
    {
        if (Input.GetKeyDown(grappleKey))
            StartGrapple();

        if (grapplingCdTimer > 0)
            grapplingCdTimer -= Time.deltaTime;

        // Update player's grappling state
        _player.isGrappling = grappling;
    }

    public void LateUpdate()
    {
        if (grappling)
            lr.SetPosition(0, gunTip.position);
    }

    public void StartGrapple()
    {
        if (grapplingCdTimer > 0) return;

        grappling = true;
        _player.isGrappling = true;
        //_player.Freeze = true; // Freeze player movement

        RaycastHit hit;
        if (Physics.Raycast(cam.position, cam.forward, out hit, maxGrappleDistance, whatIsGrappleable))
        {
            grapplePoint = hit.point;
            Invoke(nameof(ExecuteGrapple), grappleDelayTime);
        }
        else
        {
            grapplePoint = cam.position + cam.forward * maxGrappleDistance;
            Invoke(nameof(StopGrapple), grappleDelayTime);
        }

        lr.enabled = true;
        lr.SetPosition(1, grapplePoint);
    }

    public void ExecuteGrapple()
    {
        //_player.Freeze = false; // Unfreeze for movement

        Vector3 lowestPoint = new Vector3(_player.transform.position.x, _player.transform.position.y - 1f, _player.transform.position.z);
        float grapplePointRelativeYPos = grapplePoint.y - lowestPoint.y;
        float highestPointOfArc = grapplePointRelativeYPos + overshootYAxis;

        if (grapplePointRelativeYPos < 0) highestPointOfArc = overshootYAxis;

        _player.JumpToPosition(grapplePoint, highestPointOfArc);

        Invoke(nameof(StopGrapple), 1f);
    }

    public void StopGrapple()
    {
        //_player.Freeze = false;
        grappling = false;
        _player.isGrappling = false;
        grapplingCdTimer = grapplingCd;

        lr.enabled = false;
    }

    public void OnObjectTouch()
    {
        if (grappling)
            StopGrapple();
    }

    public bool IsGrappling()
    {
        return grappling;
    }

    public Vector3 GetGrapplePoint()
    {
        return grapplePoint;
    }
}
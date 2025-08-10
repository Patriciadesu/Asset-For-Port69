using UnityEngine;
using Unity.VisualScripting;

#if UNITY_EDITOR
using UnityEditor;
#endif

public enum GrapplingMode
{
    Hold,    // Hold to attach, release to detach
    Toggle  // Press once to attach, press again to detach
}

public class GrapplingHook : PlayerExtension
{
    [Header("UI")]
    public bool enableGrapplingUI = true;
    private PlayerUIManager uiManager;

    [Header("Grappling Hook Properties")]
    public KeyCode activateKey = KeyCode.E;
    
    [Header("Control Mode")]
    public GrapplingMode controlMode = GrapplingMode.Toggle;
    [Tooltip("Toggle: Press once to attach/detach. Hold: Hold to keep attached, release to detach.")]
    
    public float maxRange = 30f;
    public float hookSpeed = 20f;
    public float swingForce = 15f;
    public float reelSpeed = 10f;
    public LayerMask grappableLayer = -1;
    public bool useStamina = true;
    public float staminaCost = 20f;
    public float cooldownTime = 1f;
    
    [Header("Hook Visuals")]
    public LineRenderer hookLineRenderer;
    public Transform hookOrigin; // Where the hook shoots from (camera or hand)
    
    [Header("Physics")]
    public float maxSwingAngle = 80f; // Maximum swing angle from vertical
    public float dampening = 2f; // Swing dampening
    
    private bool isGrappling = false;
    private bool isReeling = false;
    private Vector3 grapplingPoint;
    private SpringJoint springJoint;
    private Vector3 hookDirection;
    private float lastGrappleTime = -999f;
    private Camera playerCamera;
    public LineRenderer lineRenderer;
    
    // Properties
    private bool IsOnCooldown => Time.time - lastGrappleTime < cooldownTime;
    private bool CanGrapple => _player.canMove && !IsOnCooldown && (!useStamina || _player.currentstamina >= staminaCost);
    
    public void OnDestroy()
    {
        // Cleanup on destroy
        StopGrappling();
        
        if (lineRenderer != null)
        {
            DestroyImmediate(lineRenderer.gameObject);
        }
        
        if (springJoint != null)
        {
            DestroyImmediate(springJoint);
        }
    }

    public override void OnStart(Player player)
    {
        base.OnStart(player);

        playerCamera = _player.camera;

        // Set hook origin to camera if not assigned
        if (hookOrigin == null)
            hookOrigin = playerCamera.transform;

        // Create line renderer if not assigned
        if (hookLineRenderer == null)
        {
            GameObject lineObj = new GameObject("GrapplingHookLine");
            lineObj.transform.SetParent(transform);
            lineRenderer = lineObj.AddComponent<LineRenderer>();
            SetupLineRenderer();
        }
        else
        {
            lineRenderer = hookLineRenderer;
        }

        if (enableGrapplingUI)
            uiManager = Object.FindAnyObjectByType<PlayerUIManager>();
    }
    
    private void SetupLineRenderer()
    {
        lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
        lineRenderer.startColor = Color.white;
        lineRenderer.endColor = Color.white;
        lineRenderer.startWidth = 0.1f;
        lineRenderer.endWidth = 0.1f;
        lineRenderer.positionCount = 0;
        lineRenderer.useWorldSpace = true;
    }
    
    protected void Update()
    {
        HandleInput();
        UpdateGrappling();
        UpdateUI();
    }
    
    private void HandleInput()
    {
        switch (controlMode)
        {
            case GrapplingMode.Toggle:
                HandleToggleMode();
                break;
            case GrapplingMode.Hold:
                HandleHoldMode();
                break;
        }
        
        // Reel in/out with scroll wheel or additional keys (works in both modes)
        if (isGrappling)
        {
            float scroll = Input.GetAxis("Mouse ScrollWheel");
            if (scroll != 0f || Input.GetKey(KeyCode.LeftShift))
            {
                ReelHook(scroll > 0f || Input.GetKey(KeyCode.LeftShift));
            }
        }
    }
    
    private void HandleToggleMode()
    {
        if (Input.GetKeyDown(activateKey))
        {
            if (isGrappling)
            {
                StopGrappling();
            }
            else if (CanGrapple)
            {
                StartGrappling();
            }
        }
    }
    
    private void HandleHoldMode()
    {
        if (Input.GetKey(activateKey))
        {
            // Start grappling if not already grappling and can grapple
            if (!isGrappling && CanGrapple)
            {
                StartGrappling();
            }
        }
        else
        {
            // Stop grappling if currently grappling and key is released
            if (isGrappling)
            {
                StopGrappling();
            }
        }
    }
    
    private void StartGrappling()
    {
        // Raycast to find grappling point
        Ray ray = playerCamera.ScreenPointToRay(new Vector3(Screen.width / 2, Screen.height / 2, 0));
        RaycastHit hit;
        
        if (Physics.Raycast(ray, out hit, maxRange, grappableLayer))
        {
            grapplingPoint = hit.point;
            isGrappling = true;
            lastGrappleTime = Time.time;
            
            // Consume stamina
            if (useStamina)
            {
                _player.currentstamina -= staminaCost;
            }
            
            // Create spring joint
            CreateSpringJoint();
            
            // Update line renderer
            lineRenderer.positionCount = 2;
            
            Debug.Log($"Grappling hook attached to {hit.collider.name} at distance {Vector3.Distance(transform.position, grapplingPoint):F1}m");
        }
        else
        {
            Debug.Log("No grappable surface found within range");
        }
    }
    
    private void CreateSpringJoint()
    {
        springJoint = _player.gameObject.AddComponent<SpringJoint>();
        springJoint.autoConfigureConnectedAnchor = false;
        springJoint.connectedAnchor = grapplingPoint;
        
        float distanceFromPoint = Vector3.Distance(_player.transform.position, grapplingPoint);
        
        // Adjust these values for different swing feels
        springJoint.maxDistance = distanceFromPoint * 0.8f;
        springJoint.minDistance = distanceFromPoint * 0.25f;
        springJoint.spring = 4.5f;
        springJoint.damper = 7f;
        springJoint.massScale = 4.5f;
    }
    
    private void UpdateGrappling()
    {
        if (!isGrappling) return;
        
        // Update line renderer
        if (lineRenderer.positionCount == 2)
        {
            lineRenderer.SetPosition(0, hookOrigin.position);
            lineRenderer.SetPosition(1, grapplingPoint);
        }
        
        // Apply additional swing force based on input
        ApplySwingForce();
        
        // Check if we should auto-disconnect (too far, hit ground while swinging, etc.)
        CheckAutoDisconnect();
    }
    
    private void ApplySwingForce()
    {
        float horizontalInput = Input.GetAxis("Horizontal");
        float verticalInput = Input.GetAxis("Vertical");
        
        if (Mathf.Abs(horizontalInput) > 0.1f)
        {
            Vector3 forceDirection = _player.transform.right * horizontalInput;
            _player.rigidbody.AddForce(forceDirection * swingForce, ForceMode.Force);
        }
        
        // Forward/backward swing control
        if (Mathf.Abs(verticalInput) > 0.1f)
        {
            Vector3 perpendicular = Vector3.Cross(Vector3.up, (grapplingPoint - _player.transform.position).normalized);
            Vector3 forceDirection = Vector3.Cross(perpendicular, Vector3.up) * verticalInput;
            _player.rigidbody.AddForce(forceDirection * swingForce, ForceMode.Force);
        }
    }
    
    private void ReelHook(bool reelIn)
    {
        if (springJoint == null) return;
        
        float reelAmount = reelSpeed * Time.deltaTime;
        
        if (reelIn)
        {
            springJoint.maxDistance = Mathf.Max(springJoint.maxDistance - reelAmount, springJoint.minDistance);
        }
        else
        {
            springJoint.maxDistance = Mathf.Min(springJoint.maxDistance + reelAmount, maxRange);
        }
    }
    
    private void CheckAutoDisconnect()
    {
        // Disconnect if player is grounded and moving slowly
        if (_player.isGrounded && _player.rigidbody.linearVelocity.magnitude < 2f)
        {
            StopGrappling();
            return;
        }
        
        // Disconnect if too far from grapple point
        float distance = Vector3.Distance(_player.transform.position, grapplingPoint);
        if (distance > maxRange * 1.5f)
        {
            StopGrappling();
            return;
        }
    }
    
    private void StopGrappling()
    {
        if (!isGrappling) return;
        
        isGrappling = false;
        isReeling = false;
        
        // Destroy spring joint
        if (springJoint != null)
        {
            Destroy(springJoint);
            springJoint = null;
        }
        
        // Hide line renderer
        lineRenderer.positionCount = 0;
        
        Debug.Log("Grappling hook disconnected");
    }
    
    private void UpdateUI()
    {
        if (enableGrapplingUI && uiManager != null)
        {
            uiManager.UpdateGrapplingHook(isGrappling);
        }
        
        // Debug UI information
        if (Application.isEditor)
        {
            string modeText = controlMode == GrapplingMode.Toggle ? "Toggle" : "Hold";
            string statusText = isGrappling ? "Attached" : "Ready";
            string cooldownText = IsOnCooldown ? $"Cooldown: {(cooldownTime - (Time.time - lastGrappleTime)):F1}s" : "";
            
            Debug.DrawRay(playerCamera.transform.position, playerCamera.transform.forward * maxRange, 
                isGrappling ? Color.red : (CanGrapple ? Color.green : Color.yellow));
        }
    }
    
    
    
    // Visual debugging
    private void OnDrawGizmosSelected()
    {
        if (isGrappling)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(grapplingPoint, 1f);
            Gizmos.DrawLine(transform.position, grapplingPoint);
            
#if UNITY_EDITOR
            // Draw mode indicator
            Vector3 labelPos = grapplingPoint + Vector3.up * 2f;
            Handles.Label(labelPos, $"Mode: {controlMode}");
#endif
        }
        
        // Draw grappling range
        Gizmos.color = CanGrapple ? Color.green : (IsOnCooldown ? Color.yellow : Color.red);
        if (playerCamera != null)
        {
            Gizmos.DrawRay(playerCamera.transform.position, playerCamera.transform.forward * maxRange);
            
            // Draw range sphere
            Gizmos.color = new Color(Gizmos.color.r, Gizmos.color.g, Gizmos.color.b, 0.1f);
            Gizmos.DrawSphere(playerCamera.transform.position, maxRange);
        }
        
#if UNITY_EDITOR
        // Draw control mode info
        Vector3 infoPos = transform.position + Vector3.up * 3f;
        string info = $"Grappling Mode: {controlMode}\nKey: {activateKey}\nRange: {maxRange}m";
        Handles.Label(infoPos, info);
#endif
    }
}

using UnityEngine;

public class JetPack : PlayerExtension
{
    [Header("UI")]
    public bool enableJetpackUI = true;
    private PlayerUIManager uiManager;

    [Header("Properties")]
    public KeyCode activateKey = KeyCode.X;
    public float jetPackSpeed = 5f;
    public float jetPackFuel = 100f;
    public float fuelConsumptionRate = 20f;
    public float fuelRegenerationRate = 15f;
    private bool isJetPacking = false;
    private float currentFuel;
    private bool hasUsedJetpack = false; // Track if jetpack has been used
    private bool CanJetPack => _player.canMove && currentFuel > 0;

    public override void OnStart(Player player)
    {
        base.OnStart(player);
        currentFuel = jetPackFuel;
        if (enableJetpackUI)
            uiManager = Object.FindAnyObjectByType<PlayerUIManager>();
    }

    protected void Update()
    {
        if (Input.GetKey(activateKey) && CanJetPack)
        {
            if (!isJetPacking)
            {
                StartJetPack();
            }
            JetPackMove();
        }
        else if (isJetPacking)
        {
            StopJetPack();
        }

        // if (!isJetPacking && currentFuel < jetPackFuel)
        // {
        //     RegenerateFuel();
        // }

        if (!isJetPacking && _player.isGrounded)
        {
            currentFuel = jetPackFuel;
            hasUsedJetpack = false; // Reset flag when landing
        }

        if (enableJetpackUI && uiManager != null)
            uiManager.UpdateJetpack(currentFuel, jetPackFuel, isJetPacking, _player.isGrounded, hasUsedJetpack);
    }

    private void StartJetPack()
    {
        isJetPacking = true;
        hasUsedJetpack = true; // Mark that jetpack has been used
        _player.animator.SetTrigger("jetpack");
        _player.OnUpdate -= _player.JumpHandler;
        _player.canApplyGravity = false;
    }

    private void JetPackMove()
    {
        Vector3 jetPackVelocity = _player.transform.up * jetPackSpeed;
        _player.rigidbody.linearVelocity = new Vector3(
            _player.rigidbody.linearVelocity.x,
            jetPackVelocity.y,
            _player.rigidbody.linearVelocity.z
        );
        currentFuel -= fuelConsumptionRate * Time.deltaTime;
        if (currentFuel <= 0)
        {
            currentFuel = 0;
            StopJetPack();
        }
    }

    private void StopJetPack()
    {
        isJetPacking = false;
        _player.OnUpdate += _player.JumpHandler;
        _player.canApplyGravity = true;
    }

    // private void RegenerateFuel()
    // {
    //     currentFuel += fuelRegenerationRate * Time.deltaTime;
    //     if (currentFuel > jetPackFuel)
    //     {
    //         currentFuel = jetPackFuel;
    //     }
    // }
}
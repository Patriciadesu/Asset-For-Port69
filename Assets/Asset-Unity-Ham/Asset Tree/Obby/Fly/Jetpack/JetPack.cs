using UnityEngine;

public class JetPack : PlayerExtension, ICancleGravity
{
    [Header("UI")]
    public bool enableJetpackUI = true;
    private PlayerUIManager uiManager;

    [Header("Properties")]
    public KeyCode activateKey = KeyCode.Space;
    public float jetPackSpeed = 5f;
    public float jetPackFuel = 100f;
    public float fuelConsumptionRate = 20f;
    public float fuelRegenerationRate = 15f;
    private bool isJetPacking = false;
    private float _currentFuel;
    public float currentFuel
    {
        get => _currentFuel;
        set
        {
            _currentFuel = value;
            if (_currentFuel >= jetPackFuel)
            {
                _currentFuel = jetPackFuel;
            }
        }
    }
    private bool hasUsedJetpack = false; // Track if jetpack has been used
    private bool CanJetPack => _player.canMove && currentFuel > 0;
    public bool canApplyGravity { get; set; } = true;

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

        if (!isJetPacking && _player.Movement.isGrounded)
        {
            currentFuel = jetPackFuel;
            hasUsedJetpack = false; // Reset flag when landing
        }

        if (enableJetpackUI && uiManager != null)
            uiManager.UpdateJetpack(currentFuel, jetPackFuel, isJetPacking, _player.Movement.isGrounded, hasUsedJetpack);
    }

    private void StartJetPack()
    {
        isJetPacking = true;
        hasUsedJetpack = true; // Mark that jetpack has been used
        _player.animator.SetTrigger("jetpack");
        _player.onUpdate -= _player.Movement.JumpHandler;
        canApplyGravity = false; // Disable gravity while jetpacking
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
        _player.onUpdate += _player.Movement.JumpHandler;
        canApplyGravity = true;
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

public partial class PlayerUIManager : Singleton<PlayerUIManager>
{

    public bool enableJetpackFuelUI = true;
    public void UpdateJetpack(float currentFuel, float maxFuel, bool isJetpacking, bool isGrounded, bool hasUsedJetpack)
    {
        if (!enableJetpackFuelUI) return;
        bool showUI = isJetpacking || (!isGrounded && hasUsedJetpack);
        jetpackFuelUI.gameObject.SetActive(showUI);
        if (showUI)
            jetpackFuelUI.value = currentFuel / maxFuel;
    }

}

public class JetPackUISetter : IPlayerUISetter
{
    public void OnStart(PlayerUIManager playerUI)
    {
        if (playerUI.enableJetpackFuelUI) playerUI.jetpackFuelUI.gameObject.SetActive(false);
    }
}
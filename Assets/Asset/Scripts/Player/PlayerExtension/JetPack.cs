using UnityEngine;

public class JetPack : PlayerExtension
{
    public KeyCode activateKey = KeyCode.Space;
    [Range(0, 100)] public float jetPackSpeed = 5f;
    [Range(0, 1000)] public float jetPackFuel = 100f;
    [Range(0, 100)] public float fuelConsumptionRate = 10f; // Fuel consumed per second
    [Range(0, 100)] public float fuelRegenerationRate = 5f; // Fuel regenerated per second when not jetpacking

    private bool isJetPacking = false;
    private float currentFuel;
    private bool CanJetPack => _player.canMove && currentFuel > 0;

    public override void OnStart(Player player)
    {
        base.OnStart(player);
        currentFuel = jetPackFuel;
    }

    protected void Update()
    {
        // Start jetpack when key is held and conditions are met
        if (Input.GetKey(activateKey) && CanJetPack)
        {
            if (!isJetPacking)
            {
                isJetPacking = true;
                _player.animator.SetTrigger("jetpack");
                _player.OnUpdate -= _player.JumpHandler;
                _player.canApplyGravity = false;
            }

            Vector3 jetPackVelocity = _player.transform.up * jetPackSpeed;
            _player.rigidbody.linearVelocity = new Vector3(
                _player.rigidbody.linearVelocity.x,
                jetPackVelocity.y,
                _player.rigidbody.linearVelocity.z
            );

            currentFuel -= fuelConsumptionRate * Time.deltaTime;
            if (currentFuel <= 0f)
            {
                currentFuel = 0f;
                StopJetPack();
            }
        }
        else if (isJetPacking)
        {
            // Stop jetpack when key is released or can't jetpack anymore
            StopJetPack();
        }

        // Regenerate fuel when grounded and not jetpacking
        if (!isJetPacking && currentFuel < jetPackFuel && _player.isGrounded)
        {
            RegenerateFuel();
        }
    }

    private void StopJetPack()
    {
        isJetPacking = false;
        _player.OnUpdate += _player.JumpHandler;
        _player.canApplyGravity = true;
    }

    private void RegenerateFuel()
    {
        currentFuel += fuelRegenerationRate * Time.deltaTime;
        if (currentFuel > jetPackFuel)
        {
            currentFuel = jetPackFuel;
        }
    }
}

using UnityEngine;

public class Crouch : PlayerExtension
{
    [Header("UI")]
    public bool enableCrouchUI = true;
    private PlayerUIManager uiManager;

    [Header("Properties")]
    public KeyCode activateKey = KeyCode.C;
    public float crouchSpeed = 2f;
    private bool isCrouching = false;
    private bool CanCrouch => _player.canMove && _player.isGrounded && _player.canApplyGravity;

    public override void OnStart(Player player)
    {
        base.OnStart(player);
        if (enableCrouchUI)
            uiManager = Object.FindAnyObjectByType<PlayerUIManager>();
    }

    protected void Update()
    {
        if (Input.GetKeyDown(activateKey) && CanCrouch)
        {
            ToggleCrouch();
        }
        if (enableCrouchUI && uiManager != null)
            uiManager.UpdateCrouch(isCrouching);
    }

    public void ToggleCrouch()
    {
        isCrouching = !isCrouching;
        _player.animator.SetBool("isCrouching", isCrouching);
        CapsuleCollider collider = _player.capsuleCollider;
        if (isCrouching)
        {
            _player.additionalSpeed -= crouchSpeed;
            collider.height /= 2;
            collider.center = new Vector3(collider.center.x, collider.center.y / 2, collider.center.z);
        }
        else
        {
            _player.additionalSpeed += crouchSpeed;
            collider.height *= 2;
            collider.center = new Vector3(collider.center.x, collider.center.y * 2, collider.center.z);
        }
    }
}
public partial class PlayerUIManager : Singleton<PlayerUIManager>
{
    public bool enableCrouchUI = true;
    public void UpdateCrouch(bool isCrouching)
    {
        if (!enableCrouchUI) return;
        crouchUI.SetActive(isCrouching);
    }

}
public class CrouchUISetter : IPlayerUISetter
{
    public void OnStart(PlayerUIManager playerUI)
    {
        if (playerUI.enableCrouchUI) playerUI.crouchUI.SetActive(false);
    }
}
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PlayerUIManager : MonoBehaviour
{
    public Slider staminaBar; // Always visible
    public Slider healthBar; // Always visible
    public Slider rollCooldownUI; // Shown during cooldown
    public Slider dashCooldownUI; // Shown during cooldown
    public GameObject sprintUI; // Shown when sprinting
    public Slider jetpackFuelUI; // Shown when jetpacking
    public TextMeshProUGUI multipleJumpUI; // Shown when not grounded
    public GameObject crouchUI; // Shown when crouching
    public GameObject wallRunUI; // Shown when wall running

    // Enable/Disable toggles for UI elements
    [Header("Enable/Disable UI Elements")]
    public bool enableStaminaBar = true;
    public bool enableHealthBar = true;
    public bool enableRollCooldownUI = true;
    public bool enableDashCooldownUI = true;
    public bool enableSprintUI = true;
    public bool enableJetpackFuelUI = true;
    public bool enableMultipleJumpUI = true;
    public bool enableCrouchUI = true;
    public bool enableWallRunUI = true;

    private Player player;

    void Start()
    {
        player = Object.FindAnyObjectByType<Player>();
        
        // Apply UI settings from Player
        enableHealthBar = player.enableHealthBar;
        enableStaminaBar = player.enableStaminaBar;
        
        // Initialize UI states
        if(enableRollCooldownUI) rollCooldownUI.gameObject.SetActive(false);
        if(enableDashCooldownUI) dashCooldownUI.gameObject.SetActive(false);
        if(enableSprintUI) sprintUI.SetActive(false);
        if(enableJetpackFuelUI) jetpackFuelUI.gameObject.SetActive(false);
        if(enableMultipleJumpUI) multipleJumpUI.gameObject.SetActive(false);
        if(enableCrouchUI) crouchUI.SetActive(false);
        if(enableWallRunUI) wallRunUI.SetActive(false);
    }

    void Update()
    {
        // Always update stamina bar if enabled
        if(enableStaminaBar)
            staminaBar.value = player.currentstamina / player.maxstamina;
        
        // Always update health bar if enabled
        if(enableHealthBar)
            healthBar.value = player.currenthealth / player.maxhealth;
    }

    // Methods to update UI for each ability
    public void UpdateRollCooldown(float timeSinceLastRoll, float cooldownTime)
    {
        if (!enableRollCooldownUI) return;
        bool isOnCooldown = timeSinceLastRoll < cooldownTime;
        rollCooldownUI.gameObject.SetActive(isOnCooldown);
        if (isOnCooldown)
            rollCooldownUI.value = timeSinceLastRoll / cooldownTime;
    }

    public void UpdateDashCooldown(float timeSinceLastDash, float cooldownTime)
    {
        if (!enableDashCooldownUI) return;
        bool isOnCooldown = timeSinceLastDash < cooldownTime;
        dashCooldownUI.gameObject.SetActive(isOnCooldown);
        if (isOnCooldown)
            dashCooldownUI.value = timeSinceLastDash / cooldownTime;
    }

    public void UpdateSprint(bool isSprinting)
    {
        if (!enableSprintUI) return;
        sprintUI.SetActive(isSprinting);
    }

    public void UpdateJetpack(float currentFuel, float maxFuel, bool isJetpacking, bool isGrounded, bool hasUsedJetpack)
    {
        if (!enableJetpackFuelUI) return;
        bool showUI = isJetpacking || (!isGrounded && hasUsedJetpack);
        jetpackFuelUI.gameObject.SetActive(showUI);
        if (showUI)
            jetpackFuelUI.value = currentFuel / maxFuel;
    }

    public void UpdateMultipleJump(int jumpCount, int maxJumps, bool isGrounded)
    {
        if (!enableMultipleJumpUI) return;
        bool showUI = !isGrounded && 1 < jumpCount && jumpCount <= maxJumps;
        multipleJumpUI.gameObject.SetActive(showUI);
        if (showUI)
            multipleJumpUI.text = $"Jump X{jumpCount}";
    }

    public void UpdateCrouch(bool isCrouching)
    {
        if (!enableCrouchUI) return;
        crouchUI.SetActive(isCrouching);
    }

    public void UpdateWallRun(bool isWallRunning)
    {
        if (!enableWallRunUI) return;
        wallRunUI.SetActive(isWallRunning);
    }
}
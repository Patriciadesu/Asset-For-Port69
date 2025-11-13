using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Events;
using NaughtyAttributes;
using Unity.VisualScripting;
using System;

public partial class PlayerUIManager : Singleton<PlayerUIManager>
{
    [Foldout("UI Components")]public Slider staminaBar; // Always visible
    [Foldout("UI Components")]public Slider healthBar; // Always visible
    [Foldout("UI Components")]public Slider rollCooldownUI; // Shown during cooldown
    [Foldout("UI Components")]public Slider dashCooldownUI; // Shown during cooldown
    [Foldout("UI Components")]public GameObject sprintUI; // Shown when sprinting
    [Foldout("UI Components")]public Slider jetpackFuelUI; // Shown when jetpacking
    [Foldout("UI Components")]public TextMeshProUGUI multipleJumpUI; // Shown when not grounded
    [Foldout("UI Components")]public GameObject crouchUI; // Shown when crouching
    [Foldout("UI Components")]public GameObject wallRunUI; // Shown when wall running
    [Foldout("UI Components")]public GameObject grapplingHookUI; // Shown when grappling

    // Enable/Disable toggles for UI elements
    [Header("Enable/Disable UI Elements")]
    public bool enableStaminaBar = true;
    public bool enableHealthBar = true;

    private Player player;
    private StatModule stat;

    public void Start()
    {
        player = Player.Instance;
        stat = Player.Instance.Stat;
        // Apply UI settings from Player
        enableHealthBar = player.enableHealthBar;
        enableStaminaBar = player.enableStaminaBar;

        // Initialize UI states
        rollCooldownUI.gameObject.SetActive(false);
        dashCooldownUI.gameObject.SetActive(false);
        sprintUI.gameObject.SetActive(false);
        jetpackFuelUI.gameObject.SetActive(false);
        multipleJumpUI.gameObject.SetActive(false);
        crouchUI.gameObject.SetActive(false);
        wallRunUI.gameObject.SetActive(false);

        foreach (IPlayerUISetter uISetter in GetComponents<IPlayerUISetter>())
        {
            uISetter.OnStart(this);
        }

        grapplingHookUI.SetActive(false);
    }

    void Update()
    {
        // Always update stamina bar if enabled
        if (enableStaminaBar)
            staminaBar.value = stat.currentstamina / stat.maxstamina;

        // Always update health bar if enabled
        if (enableHealthBar)
            healthBar.value = stat.currenthealth / stat.maxhealth;
    }

    // Methods to update UI for each ability
    
    
    }

using NaughtyAttributes;
using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using System;

[ExecuteInEditMode]
public class PlayerAnimation : MonoBehaviour
{
    private Animator animator => GetComponent<Animator>();
    private string walk = "Walk";//blend tree
    private string run = "Run";//blend tree
    private string crouch = "Crouch";//blend tree
    private string idle = "Idle";
    private string jump = "Jump";
    private string wallRideLeft = "WallRide_L";
    private string wallRideRight = "WallRide_R";
    [SerializeField,Tooltip("PLS DONT TOUCH THIS")] private PlayerAnimator defaultAnimation;
    [Header("Overriding Animation")]
    [SerializeField] private bool overrideAnimations = false;

    [ShowIf("overrideAnimations"), SerializeField] private PlayerAnimator playerAnimator;

    public void OnValidate()
    {
        if (!Application.isPlaying&&overrideAnimations && playerAnimator == null)
        {
            string resourcesPath = "Assets/Resources";
        if (!AssetDatabase.IsValidFolder(resourcesPath))
        {
            AssetDatabase.CreateFolder("Assets", "Resources");
        }

        // Ensure Assets/Resources/PlayerAnimatorOverrides exists
        string overridesPath = $"{resourcesPath}/PlayerAnimatorOverrides";
        if (!AssetDatabase.IsValidFolder(overridesPath))
        {
            AssetDatabase.CreateFolder(resourcesPath, "PlayerAnimatorOverrides");
        }

        // Create the PlayerAnimator asset
        playerAnimator = ScriptableObject.CreateInstance<PlayerAnimator>();
        playerAnimator.name = $"{gameObject.name}_PlayerAnimatorOverride";

        string assetPath = $"{overridesPath}/{playerAnimator.name}.asset";
        AssetDatabase.CreateAsset(playerAnimator, assetPath);
        AssetDatabase.SaveAssets();

        Debug.Log($"✅ Created new PlayerAnimator asset at: {assetPath}");
        }
    }

    public void Update()
    {
        if (!Application.isPlaying && animator != null)
        {
            if (overrideAnimations)
            {
                Debug.Log("Overriding Player Animation");
                AnimatorController controller = animator.runtimeAnimatorController as AnimatorController;
                if (controller != null)
                {
                    foreach (var layer in controller.layers)
                    {
                        foreach (var state in layer.stateMachine.states)
                        {
                            switch (state.state.name)
                            {
                                // case "Walk":
                                //     state.state.motion = playerAnimator.walk_front;
                                //     break;
                                // case "Run":
                                //     state.state.motion = playerAnimator.run_front;
                                //     break;
                                // case "Crouch":
                                //     state.state.motion = playerAnimator.crouch_front;
                                //     break;
                                case "Idle":
                                    state.state.motion = playerAnimator.idle;
                                    break;
                                case "Jump":
                                    state.state.motion = playerAnimator.jump;
                                    break;
                                case "WallRide_L":
                                    state.state.motion = playerAnimator.wallRideLeft;
                                    break;
                                case "WallRide_R":
                                    state.state.motion = playerAnimator.wallRideRight;
                                    break;
                                case "Dash":
                                    state.state.motion = playerAnimator.dash;
                                    break;
                                case "Jetpack":
                                    state.state.motion = playerAnimator.jetpack;
                                    break;
                                case "Slide":
                                    state.state.motion = playerAnimator.roll;
                                    break;
                            }
                            EditorUtility.SetDirty(controller);
                        }
                    }
                }
            }
            else
            {
                Debug.Log("Using Default Player Animation");
                AnimatorController controller = animator.runtimeAnimatorController as AnimatorController;
                if (controller != null)
                {
                    foreach (var layer in controller.layers)
                    {
                        foreach (var state in layer.stateMachine.states)
                        {
                            switch (state.state.name)
                            {
                                // case "Walk":
                                //     state.state.motion = defaultAnimation.walk_front;
                                //     break;
                                // case "Run":
                                //     state.state.motion = defaultAnimation.run_front;
                                //     break;
                                // case "Crouch":
                                //     state.state.motion = defaultAnimation.crouch_front;
                                //     break;
                                case "Idle":
                                    state.state.motion = defaultAnimation.idle;
                                    break;
                                case "Jump":
                                    state.state.motion = defaultAnimation.jump;
                                    break;
                                case "WallRide_L":
                                    state.state.motion = defaultAnimation.wallRideLeft;
                                    break;
                                case "WallRide_R":
                                    state.state.motion = defaultAnimation.wallRideRight;
                                    break;
                                case "Dash":
                                    state.state.motion = defaultAnimation.dash;
                                    break;
                                case "Jetpack":
                                    state.state.motion = defaultAnimation.jetpack;
                                    break;
                                case "Roll":
                                    state.state.motion = defaultAnimation.roll;
                                    break;
                            }
                            EditorUtility.SetDirty(controller);
                        }
                    }
                }
            }
            
        }
    }

}

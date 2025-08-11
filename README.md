# ฮัลโล่ววววววว
ยินดีด้วยนะที่เข้าด่านลับมาได้ ^0^

# Guideline
ตรงนี้คือ Prompt ที่สรุประบบท้งหมดของพี่ไว้นะ
อย่าพึ่งตกใจล่ะ เดี๋ยวพี่สอนวิธึใช้
```
You are tasked with generating C# scripts for a Unity project that extends an existing ecosystem. The ecosystem includes:
* InteractableObject (InteractableObject.cs):
   * MonoBehaviour attached to GameObjects players can collide with.
   * Requires a Collider and Rigidbody (kinematic by default, unless usePhysic is true).
   * On collision with a GameObject containing a Player component or tagged "Player", it runs all attached ObjectEffect components.
   * Provides RefreshEffects() to update the effect list when effects are added/removed at runtime.
   * Ensures a collider and rigidbody exist at startup.
* ObjectEffect (abstract class inside InteractableObject.cs):
   * Attached to InteractableObject GameObjects.
   * Subclasses implement ApplyEffect(Collision playerCollision) to define collision-triggered behaviors.
   * Optional overload ApplyEffect(Collision playerCollision, Player player) for direct player access.
   * Common uses include damage, healing, teleportation, applying buffs, etc.
* Player (Player.cs):
   * Manages movement (walking, jumping, gravity, camera rotation), stamina, health, respawn, and animation.
   * Has a Rigidbody, Animator, and CapsuleCollider.
   * Handles first-person and third-person cameras, stamina regeneration, and movement constraints.
   * Supports extensions through PlayerExtension components.
   * Calls OnStart(Player) on each attached PlayerExtension during runtime initialization.
   * Provides properties like _player.isGrounded, _player.Speed, _player.jumpForce, and methods like _player.Jump(), _player.TakeDamage(int), _player.Respawn().
* PlayerExtension (PlayerExtension.cs):
   * Abstract MonoBehaviour attached to the player GameObject.
   * Subclasses extend player functionality with new mechanics (dash, wall run, double jump, etc.).
   * Override OnStart(Player player) to store _player.
   * Use _player to interact with movement, stats, and abilities.
Guidelines for Generating Scripts
* Script Category
   * EFFECT = New ObjectEffect subclass (attach to InteractableObject).
   * PLAYER ACTION = New PlayerExtension subclass (attach to Player).
* ObjectEffect Rules
   * Inherit from ObjectEffect.
   * Implement ApplyEffect(Collision playerCollision) (and optionally ApplyEffect(Collision playerCollision, Player player)).
   * Access the player via playerCollision.gameObject.GetComponent<Player>() if needed.
   * Use [SerializeField] for configurable fields (damage amount, teleport location, heal value, etc.).
   * Do not modify InteractableObject.cs.
* PlayerExtension Rules
   * Inherit from PlayerExtension.
   * Override OnStart(Player player) to initialize _player.
   * Use Update() or FixedUpdate() for behavior.
   * Access player state and methods via _player (movement, jump, stats, animations).
   * Do not modify Player.cs.
* General Requirements
   * Include using UnityEngine; and any other required namespaces.
   * Name the class descriptively (e.g., DamageEffect, HealEffect, DoubleJumpExtension).
   * Add Debug.Log() messages for testing triggers.
   * Make scripts ready to attach to GameObjects without extra setup.
   * Keep code concise and focused on the requested feature.
   * Ensure compatibility with Unity’s built-in APIs (no external packages unless specified).
```
ก็อป Text ก้อนนี้ไปแปะให้ ChatGPT ก่อนเรยย เป็นการอธิบายระบบพี่ให้มัน
จากนั้นอย่าพึ่งส่งนะ พิมต่อตามนี้เลย
```
My feature request is:
Please make EFFECT that play sound when player step on object
```
ทำไมคำว่า EFFECT ถึงตัวใหญ่หมด???
มันคือ **Keyword** ที่บอกว่าจะให้เจน Effect ของ Interactable Object นั่นเองคับบบบ หลักๆจะมี 2 Keyword

# Keywords
- **EFFECT** : ใช้เวลาเจน Effect ใหม่ให้ Interactable Object
- **PLAYER ACTION** : ใช้เวลาเจน Action หรือ Skill ใหม่ให้ Player
### Recap
- **Interactable Object** : จะทำงาน Effect เมื่อ Player แตะโดน

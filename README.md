# ฮัลโล่ววววววว
ยินดีด้วยนะที่เข้าด่านลับมาได้ ^0^

# Guideline
ตรงนี้คือ Prompt ที่สรุประบบท้งหมดของพี่ไว้นะ
อย่าพึ่งตกใจล่ะ เดี๋ยวพี่สอนวิธึใช้
```
You are tasked with generating C# scripts for a Unity project that extends an existing ecosystem. The ecosystem includes:
- **InteractableObject**: A MonoBehaviour on GameObjects that players can collide with. It has a Collider and Rigidbody (kinematic). On collision with a GameObject tagged "Player" (with a PlayerController), it applies all attached ObjectEffect components. It has a RefreshEffects() method to update its effect list.
- **ObjectEffect**: An abstract MonoBehaviour class. Subclasses implement ApplyEffect(Collision playerCollision) to define behaviors triggered on player collision (e.g., damage, teleport). Attached to InteractableObject.
- **PlayerController**: A MonoBehaviour managing player movement (speed, jump, crouch), camera (first/third-person), and states (isGrounded, isCrouching). It has a Rigidbody, CapsuleCollider, Animator, and properties like jumpForce, Speed, and methods like Jump(), RefreshExtension(). It supports PlayerExtension components.
- **PlayerExtension**: An abstract MonoBehaviour class. Subclasses extend player functionality (e.g., new movement mechanics). It has a virtual OnStart(PlayerController player) method and accesses the PlayerController via protected _player field. Attached to the player GameObject.

**Guidelines for generating scripts:**
- Create a single C# script that is either an ObjectEffect (for effects on InteractableObject) or PlayerExtension (for player actions).
- Use "EFFECT" in the request to mean a new ObjectEffect subclass, and "PLAYER ACTION" to mean a new PlayerExtension subclass.
- Include necessary using statements (e.g., UnityEngine).
- Name the class descriptively based on the feature (e.g., SoundEffect, MultiJumpExtension).
- For ObjectEffect:
  - Inherit from ObjectEffect.
  - Implement ApplyEffect(Collision playerCollision).
  - Access PlayerController via playerCollision.gameObject.GetComponent<PlayerController>().
  - Add [SerializeField] for configurable fields in the Unity Inspector.
- For PlayerExtension:
  - Inherit from PlayerExtension.
  - Override OnStart(PlayerController player) to initialize _player.
  - Use Update() or other Unity methods for behavior.
  - Access PlayerController properties/methods via _player (e.g., _player.isGrounded, _player.Jump()).
- Ensure compatibility with Unity’s built-in APIs (no external packages unless specified).
- Do not modify InteractableObject or PlayerController scripts.
- Add Debug.Log statements for testing (e.g., to confirm effect/action triggers).
- Make the script ready to attach to a GameObject (InteractableObject for effects, player for extensions).
- Include [SerializeField] for any configurable values to allow tweaking in the Unity Inspector.
- Ensure the script is concise and focused on the requested feature.
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

# Last But Not Least
จนถึงตรงนี้เราก็จะจบการเป็น Developer ในขั้้นพื้นฐานที่สุดแล้วว ช่ายมันพึ่งพื้นฐานแรกสุดเลย และถ้าเราสนใจไปต่อกับเส้นทางการเป็น Unity Developer และอยากลงลึกกับเส้นทางนี้้ก็... ทำไงดีวะ55555

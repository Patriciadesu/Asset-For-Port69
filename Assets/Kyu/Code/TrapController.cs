// using UnityEngine;
// using UnityEngine.Playables; // ต้องใช้สำหรับ Timeline
// using System.Collections; // ต้องใช้สำหรับ Coroutine ใน PlayerMovement

// /// <summary>
// /// ควบคุมพฤติกรรมของกับดัก: หยุดผู้เล่น, เล่น Timeline, สามารถทำลายได้, และทำลายตัวเองหลังจากใช้งาน
// /// </summary>
// public class TrapController : ObjectEffect
// {
//     [Header("Dependencies")]
//     [Tooltip("ลาก PlayableDirector ที่มี Timeline มาใส่")]
//     [SerializeField]
//     private PlayableDirector trapTimeline;
    
//     [Header("Settings")]
//     [Tooltip("ระยะเวลาที่ผู้เล่นถูกหยุดเมื่อโดนกับดัก")]
//     [SerializeField]
//     private float stunDuration = 2f; 

//     [Tooltip("รัศมีที่ผู้เล่นต้องอยู่ในระยะเพื่อทำลายกับดัก")]
//     [SerializeField]
//     private float disarmRange = 1.5f;

//     [Tooltip("ระยะเวลาที่กับดักจะทำลายตัวเองหลังจากถูกเปิดใช้งาน (ตามคำขอ: 5 วินาที)")]
//     [SerializeField]
//     private float selfDestructDelay = 5f; // ตัวแปรสำหรับกำหนดเวลาทำลายตัวเอง

//     // Property สำหรับให้ PlayerInteraction เข้าถึงระยะ Disarm ได้ (ใช้ในโค้ด PlayerInteraction ก่อนหน้า)
//     public float DisarmRange => disarmRange; 

//     private bool isActivated = false;
//     private PlayerMovement hitPlayer; // เก็บ PlayerMovement ของผู้เล่นที่ชน

//     private void Awake()
//     {
//         if (trapTimeline == null)
//         {
//             Debug.LogError($"Timeline is missing on the trap: {gameObject.name}");
//         }
//     }

//     // 1. ตรวจจับการชนกับผู้เล่น
//     private void OnTriggerEnter(Collider other)
//     {
//         if (isActivated) return; // ไม่ทำงานซ้ำ

//         // ตรวจสอบ Tag และดึง Component การเคลื่อนไหว
//         if (other.CompareTag("Player"))
//         {
//             hitPlayer = other.GetComponent<PlayerMovement>();
//             if (hitPlayer != null)
//             {
//                 ActivateTrap(hitPlayer);
//             }
//         }
//     }

//     private void ActivateTrap(PlayerMovement player)
//     {
//         isActivated = true;
        
//         // A. เล่น Timeline
//         if (trapTimeline != null)
//         {
//             trapTimeline.Play();
//         }

//         // B. หยุดผู้เล่น
//         player.Stun(stunDuration);

//         // C. สั่งทำลายตัวเองหลังจากเวลาที่กำหนด (5 วินาที)
//         Destroy(gameObject, selfDestructDelay); 

//         Debug.Log($"Trap Activated: Player Stunned and Timeline Playing. Trap will self-destruct in {selfDestructDelay} seconds.");
//     }

//     /// <summary>
//     /// เมธอดที่เรียกจาก PlayerInteraction เมื่อกด 'E' เพื่อทำลาย
//     /// </summary>
//     public void Disarm(Transform playerTransform)
//     {
//         // ตรวจสอบระยะห่าง
//         if (Vector3.Distance(transform.position, playerTransform.position) <= disarmRange)
//         {
//             // C. ทำลายกับดักทันที
//             Destroy(gameObject);
//             Debug.Log("Trap successfully DISARMED and destroyed.");
            
//             // หากมีการ Stun ผู้เล่นอยู่ (กรณีทำลายก่อนที่ stunDuration จะหมด)
//             if (isActivated && hitPlayer != null)
//             {
//                 hitPlayer.Unstun(); // คลาย Stun ผู้เล่นทันที
//             }
//         }
//         else
//         {
//             Debug.Log("Too far to disarm the trap.");
//         }
//     }
// }
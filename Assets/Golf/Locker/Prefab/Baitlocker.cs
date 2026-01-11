// using System.Collections;
// using UnityEngine;

// public class Baitlocker : MonoBehaviour
// {
//     [Header("References")]
//     public GameObject player;// your player GameObject
//     Player Playerscript;
//     public Camera lockerCamera;
//     public Boss bossscript;

//     [Header("Settings")]
//     public KeyCode toggleHideKey;  // key to toggle hiding
//     public Transform lockerCameraPosition;     // optional: transform to move normalCamera to if you reuse the same camera
//     public Transform Exitpos;
//     [Header("Boolsacript")]
//     public EntryPoint entryPoint;
//     public static bool isHiding = false;
//     private Coroutine beforeDieCoroutine = null;

//     [Header("Timebeforedie")]
//     public float timeBeforeDie = 2f;

//     void Start()
//     {
//         player = GameObject.FindGameObjectWithTag("Player");
//         Playerscript = player.GetComponent<Player>();
//         entryPoint = GetComponentInChildren<EntryPoint>();
//         if (lockerCamera != null)
//         {
//             lockerCameraPosition = lockerCamera.transform;
//         }

//         lockerCamera.enabled = false;

//     }

//     void Update()
//     {
//         if (isHiding)
//         {
//             if (bossscript != null)
//             {
//                 bossscript.PlayerOutOfSight();
//                 bossscript.PlayerOutOfAttackRange();
//             }
//         }

//         if (Input.GetKeyDown(toggleHideKey) && entryPoint.isInzone == true)
//         {
//             if (!isHiding)
//             {
//                 Debug.Log("Entering Locker");
//                 EnterLocker();
//             }
//             else
//             {
//                 Debug.Log("Exiting Locker");
//                 entryPoint.isInzone = false;
//                 ExitLocker();

//             }
//         }
//     }

//     void EnterLocker()
//     {
//         if (beforeDieCoroutine != null)
//         {
//             StopCoroutine(beforeDieCoroutine);
//             beforeDieCoroutine = null;
//         }

//         // Now start the new one and store its reference
//         beforeDieCoroutine = StartCoroutine(Beforedie());
//         player.transform.position = Exitpos.transform.position;
//         player.transform.rotation = Exitpos.transform.rotation;
//         isHiding = true;
//         player.SetActive(false);
//         lockerCamera.enabled = true;


//         // Optionally reposition the locker camera (if it's same camera you�re moving)
//         if (lockerCameraPosition != null && lockerCamera != null)
//         {
//             lockerCamera.transform.position = lockerCameraPosition.position;
//             lockerCamera.transform.rotation = lockerCameraPosition.rotation;
//         }
//     }

//     void ExitLocker()
//     {
//         isHiding = false;
//         player.SetActive(true);
//         lockerCamera.enabled = false;
//     }

//     public IEnumerator Beforedie() {
    
//         Debug.Log("Waiting to die");
//         yield return new WaitForSeconds(timeBeforeDie);
//         if (isHiding)
//         {
//             Debug.Log("Player is hiding, will die now wa");
//             Playerscript.Stat.currenthealth = 0;
//         }
//         else
//         {
//             Debug.Log("Player is not hiding, will not die wa");
//             yield break;
//         }
//         beforeDieCoroutine = null;

//     }
// }

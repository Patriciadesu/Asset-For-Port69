using UnityEngine;

public class lockerhiding : MonoBehaviour
{
    [Header("References")]
    public GameObject player;// your player GameObject
    public Boss bossscript;
    public Camera lockerCamera;

    [Header("Settings")]
    public KeyCode toggleHideKey = KeyCode.E;  // key to toggle hiding
    public Transform lockerCameraPosition;     // optional: transform to move normalCamera to if you reuse the same camera
    public Transform Exitpos;
    [Header("Boolsacript")]
    public EntryPoint entryPoint;
    public static bool isHiding = false;

    void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player");
        entryPoint = GetComponentInChildren<EntryPoint>();
        if (lockerCamera != null)
        {
            lockerCameraPosition = lockerCamera.transform;
        }

        lockerCamera.enabled = false;

    }

    void Update()
    {
        if (isHiding)
        {
            if (bossscript != null)
            { 
                bossscript.PlayerOutOfSight();
                bossscript.PlayerOutOfAttackRange();
            }
        }


        if (Input.GetKeyDown(toggleHideKey) && entryPoint.isInzone == true)
        {
            if (!isHiding)
            {
                Debug.Log("Entering Locker");
                EnterLocker();
            }
            else
            {
                Debug.Log("Exiting Locker");
                entryPoint.isInzone = false;
                ExitLocker();

            }
        }
    }

    void EnterLocker()
    {
        player.transform.position = Exitpos.transform.position;
        player.transform.rotation = Exitpos.transform.rotation;
        isHiding = true;
        player.SetActive(false);
        lockerCamera.enabled = true;


        // Optionally reposition the locker camera (if it's same camera you’re moving)
        if (lockerCameraPosition != null && lockerCamera != null)
        {
            lockerCamera.transform.position = lockerCameraPosition.position;
            lockerCamera.transform.rotation = lockerCameraPosition.rotation;
        }
    }

    void ExitLocker()
    {
        isHiding = false;
        player.SetActive(true);
        lockerCamera.enabled = false;
    }

}

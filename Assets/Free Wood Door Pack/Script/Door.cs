using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace DoorScript
{
    public enum GizmoType
    {
        Box,
        Sphere,
        WireCube,
        WireSphere
    }
    [RequireComponent(typeof(AudioSource))]
    public class Door : MonoBehaviour
    {
        [Header("Door Settings")]
        public bool open;
        public float smooth = 1.0f;
        public float DoorOpenAngle = -90.0f;
        public float DoorCloseAngle = 0.0f;
        [Header("Door Timer")]
        public bool useAutoClose = true;
        [Range(0.1f, 30f)]
        public float autoCloseTime = 2.0f;
        private float closeTimer;
        private bool timerActive = false;
        [Header("Audio Settings")]
        public AudioSource asource;
        public AudioClip openDoor, closeDoor;
        [Header("Trigger Gizmo Settings")]
        public GizmoType gizmoType = GizmoType.Box;
        public Vector3 gizmoSize = new Vector3(2f, 2f, 2f);
        public Vector3 gizmoPivot = Vector3.zero;
        public Color gizmoColor = Color.green;
        [Range(0.1f, 1f)]
        public float gizmoAlpha = 0.3f;
        [Header("Player Detection")]
        public string playerTag = "Player";
        public bool useTriggerDetection = true;
        public bool useColliderDetection = true;
        [Header("Debug")]
        public bool enableDebugLogs = true;
        private List<GameObject> playersInZone = new List<GameObject>();
        private bool wasPlayerInZone = false;
        void Start()
        {
            asource = GetComponent<AudioSource>();
            if (GetComponent<Rigidbody>() == null)
            {
                Rigidbody rb = gameObject.AddComponent<Rigidbody>();
                rb.isKinematic = true;
                if (enableDebugLogs) Debug.Log("Added kinematic Rigidbody to door for trigger detection");
            }
            if (GetComponent<Collider>() == null)
            {
                BoxCollider collider = gameObject.AddComponent<BoxCollider>();
                collider.isTrigger = useTriggerDetection;
                collider.size = gizmoSize;
                collider.center = gizmoPivot;
                if (enableDebugLogs) Debug.Log("Added BoxCollider to door. isTrigger = " + collider.isTrigger);
            }
            else
            {
                Collider existingCollider = GetComponent<Collider>();
                if (useTriggerDetection && !useColliderDetection)
                {
                    existingCollider.isTrigger = true;
                }
                else if (!useTriggerDetection && useColliderDetection)
                {
                    existingCollider.isTrigger = false;
                }
                else if (useTriggerDetection && useColliderDetection)
                {
                    existingCollider.isTrigger = true;
                }
                if (enableDebugLogs) Debug.Log("Configured existing collider. isTrigger = " + existingCollider.isTrigger);
            }
            if (enableDebugLogs)
            {
                Debug.Log("Door setup complete. Looking for player tag: '" + playerTag + "'");
                Debug.Log("Trigger Detection: " + useTriggerDetection + ", Collider Detection: " + useColliderDetection);
            }
        }
        void Update()
        {
            if (open)
            {
                var target = Quaternion.Euler(0, DoorOpenAngle, 0);
                transform.localRotation = Quaternion.Slerp(transform.localRotation, target, Time.deltaTime * 5 * smooth);
            }
            else
            {
                var target1 = Quaternion.Euler(0, DoorCloseAngle, 0);
                transform.localRotation = Quaternion.Slerp(transform.localRotation, target1, Time.deltaTime * 5 * smooth);
            }
            if (useAutoClose && timerActive)
            {
                closeTimer -= Time.deltaTime;
                if (closeTimer <= 0)
                {
                    if (open && playersInZone.Count == 0)
                    {
                        OpenDoor();
                        timerActive = false;
                    }
                }
            }
            bool playerCurrentlyInZone = playersInZone.Count > 0;
            if (playerCurrentlyInZone && !wasPlayerInZone)
            {
                if (!open)
                {
                    OpenDoor();
                }
                if (useAutoClose)
                {
                    closeTimer = autoCloseTime;
                    timerActive = true;
                }
            }
            else if (!playerCurrentlyInZone && wasPlayerInZone)
            {
                if (useAutoClose && open)
                {
                    closeTimer = autoCloseTime;
                    timerActive = true;
                }
            }
            wasPlayerInZone = playerCurrentlyInZone;
            Collider col = GetComponent<Collider>();
            if (col != null)
            {
                if (col is BoxCollider boxCol)
                {
                    boxCol.size = gizmoSize;
                    boxCol.center = gizmoPivot;
                }
                else if (col is SphereCollider sphereCol)
                {
                    sphereCol.radius = gizmoSize.x * 0.5f;
                    sphereCol.center = gizmoPivot;
                }
            }
        }
        public void OpenDoor()
        {
            open = !open;
            asource.clip = open ? openDoor : closeDoor;
            asource.Play();
        }
        void OnTriggerEnter(Collider other)
        {
            if (enableDebugLogs) Debug.Log("OnTriggerEnter detected: " + other.name + " with tag: " + other.tag);
            if (useTriggerDetection && other.CompareTag(playerTag))
            {
                if (enableDebugLogs) Debug.Log("Player entered trigger zone!");
                AddPlayerToZone(other.gameObject);
            }
        }
        void OnTriggerExit(Collider other)
        {
            if (enableDebugLogs) Debug.Log("OnTriggerExit detected: " + other.name + " with tag: " + other.tag);
            if (useTriggerDetection && other.CompareTag(playerTag))
            {
                if (enableDebugLogs) Debug.Log("Player exited trigger zone!");
                RemovePlayerFromZone(other.gameObject);
            }
        }
        void OnCollisionEnter(Collision collision)
        {
            if (enableDebugLogs) Debug.Log("OnCollisionEnter detected: " + collision.gameObject.name + " with tag: " + collision.gameObject.tag);
            if (useColliderDetection && collision.gameObject.CompareTag(playerTag))
            {
                if (enableDebugLogs) Debug.Log("Player collision detected!");
                AddPlayerToZone(collision.gameObject);
            }
        }
        void OnCollisionExit(Collision collision)
        {
            if (useColliderDetection && collision.gameObject.CompareTag(playerTag))
            {
                RemovePlayerFromZone(collision.gameObject);
            }
        }
        void OnCollisionStay(Collision collision)
        {
            if (useColliderDetection && collision.gameObject.CompareTag(playerTag))
            {
                AddPlayerToZone(collision.gameObject);
            }
        }
        private void AddPlayerToZone(GameObject player)
        {
            if (!playersInZone.Contains(player))
            {
                playersInZone.Add(player);
                if (enableDebugLogs) Debug.Log("Added player to zone. Total players: " + playersInZone.Count);
            }
        }
        private void RemovePlayerFromZone(GameObject player)
        {
            if (playersInZone.Contains(player))
            {
                playersInZone.Remove(player);
                if (enableDebugLogs) Debug.Log("Removed player from zone. Total players: " + playersInZone.Count);
            }
        }
        void OnDrawGizmos()
        {
            Color gizmoColorWithAlpha = gizmoColor;
            gizmoColorWithAlpha.a = gizmoAlpha;
            Gizmos.color = gizmoColorWithAlpha;
            Vector3 gizmoPosition = transform.position + transform.TransformDirection(gizmoPivot);
            switch (gizmoType)
            {
                case GizmoType.Box:
                    Gizmos.matrix = Matrix4x4.TRS(gizmoPosition, transform.rotation, Vector3.one);
                    Gizmos.DrawCube(Vector3.zero, gizmoSize);
                    break;
                case GizmoType.Sphere:
                    Gizmos.matrix = Matrix4x4.TRS(gizmoPosition, transform.rotation, Vector3.one);
                    Gizmos.DrawSphere(Vector3.zero, gizmoSize.x * 0.5f);
                    break;
                case GizmoType.WireCube:
                    Gizmos.matrix = Matrix4x4.TRS(gizmoPosition, transform.rotation, Vector3.one);
                    Gizmos.DrawWireCube(Vector3.zero, gizmoSize);
                    break;
                case GizmoType.WireSphere:
                    Gizmos.matrix = Matrix4x4.TRS(gizmoPosition, transform.rotation, Vector3.one);
                    Gizmos.DrawWireSphere(Vector3.zero, gizmoSize.x * 0.5f);
                    break;
            }
            Gizmos.matrix = Matrix4x4.identity;
        }
        void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            Vector3 gizmoPosition = transform.position + transform.TransformDirection(gizmoPivot);
            switch (gizmoType)
            {
                case GizmoType.Box:
                case GizmoType.WireCube:
                    Gizmos.matrix = Matrix4x4.TRS(gizmoPosition, transform.rotation, Vector3.one);
                    Gizmos.DrawWireCube(Vector3.zero, gizmoSize);
                    break;
                case GizmoType.Sphere:
                case GizmoType.WireSphere:
                    Gizmos.matrix = Matrix4x4.TRS(gizmoPosition, transform.rotation, Vector3.one);
                    Gizmos.DrawWireSphere(Vector3.zero, gizmoSize.x * 0.5f);
                    break;
            }
            Gizmos.matrix = Matrix4x4.identity;
        }
    }
}
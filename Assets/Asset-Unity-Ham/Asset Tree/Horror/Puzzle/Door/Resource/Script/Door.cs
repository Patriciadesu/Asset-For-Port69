using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using NaughtyAttributes;
using UnityEngine.Events;

namespace DoorScript
{
    public enum GizmoType { Box, Sphere, WireCube, WireSphere }

    [RequireComponent(typeof(AudioSource))]
    public class Door : MonoBehaviour
    {
        // ─────────────────────────────────────────────────────────────────────────────
        // Lock & key (ALWAYS consume one key on first use)
        // ─────────────────────────────────────────────────────────────────────────────
        [BoxGroup("Unlock Settings")]
        [Tooltip("If true, a key is required to open. On first successful use, one key is consumed and the door becomes unlocked.")]
        public bool isLocked = true;

        [BoxGroup("Unlock Settings")]
        [Tooltip("If set, this exact KeyItem is required. If left null, ANY KeyItem works.")]
        public KeyItem requiredKey;

        [BoxGroup("Unlock Settings")]
        [Tooltip("Open automatically when a player with the key enters the zone. If off, player must press Interact Key.")]
        public UnityEvent onDoorUnlocked;
        private bool openOnEnter = true;

        // ─────────────────────────────────────────────────────────────────────────────
        // Door motion / audio
        // ─────────────────────────────────────────────────────────────────────────────
        private const string G = "Door Settings";
        [Foldout(G)] public bool open;
        [Foldout(G)] public float smooth = 1.0f;
        [Foldout(G)] public float DoorOpenAngle = -90.0f;
        [Foldout(G)] public float DoorCloseAngle = 0.0f;

        [Foldout(G), Header("Auto-Close")]
        public bool useAutoClose = true;
        [Foldout(G), Range(0.1f, 30f)]
        public float autoCloseTime = 2.0f;

        private float closeTimer;
        private bool timerActive = false;

        [Foldout(G), Header("Audio")]
        public AudioSource asource;
        [Foldout(G)] public AudioClip openDoor, closeDoor;

        // ─────────────────────────────────────────────────────────────────────────────
        // Detection / gizmos
        // ─────────────────────────────────────────────────────────────────────────────
        [Foldout(G), Header("Trigger Gizmo")]
        public GizmoType gizmoType = GizmoType.Box;
        [Foldout(G)] public Vector3 gizmoSize = new Vector3(2f, 2f, 2f);
        [Foldout(G)] public Vector3 gizmoPivot = Vector3.zero;
        [Foldout(G)] public Color gizmoColor = Color.green;
        [Foldout(G), Range(0.1f, 1f)] public float gizmoAlpha = 0.3f;
        private const float MinBidirectionalDepth = 0.6f;

        [Foldout(G), Header("Player Detection")]
        public string playerTag = "Player";
        [Foldout(G)] public bool useTriggerDetection = true;
        [Foldout(G)] public bool useColliderDetection = true;

        [Foldout(G), Header("Debug")]
        public bool enableDebugLogs = false;

        private readonly List<GameObject> playersInZone = new();
        private bool wasPlayerInZone = false;

        // ─────────────────────────────────────────────────────────────────────────────
        // Unity lifecycle
        // ─────────────────────────────────────────────────────────────────────────────
        void Reset() => EnsurePhysicsAndCollider();
        void OnValidate() => SyncColliderToGizmo();
        void Awake() { asource = GetComponent<AudioSource>(); EnsurePhysicsAndCollider(); }
        void Start() { SyncColliderToGizmo(); }

        public void SetLock(bool newIsLocked)
        {
            isLocked = newIsLocked;
        }

        void Update()
        {
            // Smooth hinge motion
            var target = Quaternion.Euler(0, open ? DoorOpenAngle : DoorCloseAngle, 0);
            transform.localRotation = Quaternion.Slerp(transform.localRotation, target, Time.deltaTime * 5f * smooth);

            // Auto-close logic
            if (useAutoClose && timerActive)
            {
                closeTimer -= Time.deltaTime;
                if (closeTimer <= 0f && open && playersInZone.Count == 0)
                {
                    ToggleDoor(false);
                    timerActive = false;
                    // IMPORTANT: We do NOT relock here. Door stays unlocked once a key has been consumed.
                }
            }

            bool playerCurrentlyInZone = playersInZone.Count > 0;

            // Auto-open
            if (playerCurrentlyInZone && !wasPlayerInZone)
            {
                TryOpenWithLockCheck();
            }

            // Start auto-close when player leaves
            if (!playerCurrentlyInZone && wasPlayerInZone)
            {
                if (useAutoClose && open)
                {
                    closeTimer = autoCloseTime;
                    timerActive = true;
                }
            }

            wasPlayerInZone = playerCurrentlyInZone;

            // Keep collider sized with gizmo settings (useful if tweaked live)
            SyncColliderToGizmo();
        }

        // ─────────────────────────────────────────────────────────────────────────────
        // Core behaviors
        // ─────────────────────────────────────────────────────────────────────────────
        public void TryOpenWithLockCheck()
        {
            if (open) return;

            if (!isLocked)
            {
                // Already unlocked previously; just open
                ToggleDoor(true);
                if (useAutoClose) { closeTimer = autoCloseTime; timerActive = true; }
                return;
            }

            // Locked: must have (and consume) a key
            if (PlayerHasRequiredKey())
            {
                // ALWAYS consume one key on first successful use
                if (TryConsumeOneKey())
                {
                    onDoorUnlocked?.Invoke();
                    isLocked = false; // permanently unlocked (until you set it true again)
                    ToggleDoor(true);
                    if (useAutoClose) { closeTimer = autoCloseTime; timerActive = true; }
                }
                else if (enableDebugLogs)
                {
                    Debug.LogWarning("[Door] Had key but failed to consume (inventory mismatch?)");
                }
            }
            else if (enableDebugLogs)
            {
                Debug.Log("[Door] Locked and player doesn't have the required key.");
            }
        }

        void ToggleDoor(bool toOpen)
        {
            open = toOpen;
            if (asource != null)
            {
                asource.clip = open ? openDoor : closeDoor;
                if (asource.clip != null) asource.Play();
            }
        }

        bool PlayerHasRequiredKey()
        {
            var inv = Inventory.Instance;
            if (inv == null || inv.items == null) return false;

            if (requiredKey != null)
                return inv.items.Any(i => i != null && i.type == requiredKey);
            else
                return inv.items.Any(i => i != null && i.type is KeyItem);
        }

        // ALWAYS consumes exactly ONE matching key (specific or any)
        bool TryConsumeOneKey()
        {
            var inv = Inventory.Instance;
            if (inv == null || inv.items == null) return false;

            for (int i = 0; i < inv.items.Length; i++)
            {
                var it = inv.items[i];
                if (it == null) continue;

                bool match = (requiredKey != null) ? (it.type == requiredKey) : (it.type is KeyItem);
                if (match)
                {
                    inv.RemoveItem(it);
                    if (enableDebugLogs) Debug.Log("[Door] Consumed one key. Door is now unlocked.");
                    return true;
                }
            }
            return false;
        }

        // ─────────────────────────────────────────────────────────────────────────────
        // Detection hooks
        // ─────────────────────────────────────────────────────────────────────────────
        void OnTriggerEnter(Collider other)
        {
            if (!useTriggerDetection) return;
            if (other.CompareTag(playerTag)) AddPlayerToZone(other.gameObject);
        }

        void OnTriggerExit(Collider other)
        {
            if (!useTriggerDetection) return;
            if (other.CompareTag(playerTag)) RemovePlayerFromZone(other.gameObject);
        }

        void OnCollisionEnter(Collision collision)
        {
            if (!useColliderDetection) return;
            if (collision.gameObject.CompareTag(playerTag)) AddPlayerToZone(collision.gameObject);
        }

        void OnCollisionExit(Collision collision)
        {
            if (!useColliderDetection) return;
            if (collision.gameObject.CompareTag(playerTag)) RemovePlayerFromZone(collision.gameObject);
        }

        void OnCollisionStay(Collision collision)
        {
            if (!useColliderDetection) return;
            if (collision.gameObject.CompareTag(playerTag)) AddPlayerToZone(collision.gameObject);
        }

        void AddPlayerToZone(GameObject player)
        {
            if (!playersInZone.Contains(player))
            {
                playersInZone.Add(player);
                if (enableDebugLogs) Debug.Log($"[Door] Player entered zone. Count: {playersInZone.Count}");
            }
        }

        void RemovePlayerFromZone(GameObject player)
        {
            if (playersInZone.Remove(player) && enableDebugLogs)
                Debug.Log($"[Door] Player left zone. Count: {playersInZone.Count}");
        }

        // ─────────────────────────────────────────────────────────────────────────────
        // Helpers
        // ─────────────────────────────────────────────────────────────────────────────
        void EnsurePhysicsAndCollider()
        {
            if (!asource) asource = GetComponent<AudioSource>();

            var rb = GetComponent<Rigidbody>();
            if (!rb)
            {
                rb = gameObject.AddComponent<Rigidbody>();
                rb.isKinematic = true;
            }
            else rb.isKinematic = true;

            var col = GetComponent<Collider>();
            if (!col)
            {
                var b = gameObject.AddComponent<BoxCollider>();
                b.isTrigger = useTriggerDetection || !useColliderDetection;
            }
            else
            {
                col.isTrigger = (useTriggerDetection && !useColliderDetection) || (useTriggerDetection && useColliderDetection);
            }
        }

        void SyncColliderToGizmo()
        {
            var col = GetComponent<Collider>();
            if (!col) return;

            if (col is BoxCollider box)
            {
                Vector3 size = gizmoSize;
                size.z = Mathf.Max(MinBidirectionalDepth, size.z);
                box.size = size;

                Vector3 pivot = gizmoPivot;
                pivot.z = 0f;
                box.center = pivot;
            }
            else if (col is SphereCollider sphere)
            {
                sphere.radius = gizmoSize.x * 0.5f;
                sphere.center = gizmoPivot;
            }
        }

        // ─────────────────────────────────────────────────────────────────────────────
        // Gizmos
        // ─────────────────────────────────────────────────────────────────────────────
        void OnDrawGizmos()
        {
            var gizColor = gizmoColor; gizColor.a = gizmoAlpha;
            Gizmos.color = gizColor;
            var pos = transform.position + transform.TransformDirection(gizmoPivot);
            Gizmos.matrix = Matrix4x4.TRS(pos, transform.rotation, Vector3.one);

            switch (gizmoType)
            {
                case GizmoType.Box:        Gizmos.DrawCube(Vector3.zero, gizmoSize); break;
                case GizmoType.Sphere:     Gizmos.DrawSphere(Vector3.zero, gizmoSize.x * 0.5f); break;
                case GizmoType.WireCube:   Gizmos.DrawWireCube(Vector3.zero, gizmoSize); break;
                case GizmoType.WireSphere: Gizmos.DrawWireSphere(Vector3.zero, gizmoSize.x * 0.5f); break;
            }

            Gizmos.matrix = Matrix4x4.identity;
        }

        void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            var pos = transform.position + transform.TransformDirection(gizmoPivot);
            Gizmos.matrix = Matrix4x4.TRS(pos, transform.rotation, Vector3.one);

            switch (gizmoType)
            {
                case GizmoType.Box:
                case GizmoType.WireCube:
                    Gizmos.DrawWireCube(Vector3.zero, gizmoSize); break;
                case GizmoType.Sphere:
                case GizmoType.WireSphere:
                    Gizmos.DrawWireSphere(Vector3.zero, gizmoSize.x * 0.5f); break;
            }

            Gizmos.matrix = Matrix4x4.identity;
        }
    }
}

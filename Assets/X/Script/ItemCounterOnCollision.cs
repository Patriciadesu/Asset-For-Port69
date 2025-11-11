using UnityEngine;

[RequireComponent(typeof(Collider))]
public class ItemCounterOnCollision : MonoBehaviour
{
    [Header("Who can trigger")]
    public string playerTag = "Player";   // ชนแล้วต้องเป็นแท็กนี้เท่านั้น
    public bool once = false;             // ยิงครั้งเดียวต่อชีวิตสคริปต์

    [Header("Target Counter")]
    [SerializeField] private ItemCounter target; // ลากมานี่ได้ ถ้าเว้นว่างจะหาให้อัตโนมัติ

    [Header("Actions")]
    public bool doAdd;
    [Min(1)] public int addAmount = 1;

    public bool doRemove;
    [Min(1)] public int removeAmount = 1;

    public bool doReset;


    void Awake()
    {
        AutoResolveTargetIfNeeded();
    }

    void OnCollisionEnter(Collision collision)
    {
        if (!collision.gameObject.CompareTag(playerTag)) return;

        if (!target && !AutoResolveTargetIfNeeded()) return;

        if (doReset) target.ResetCounter();
        if (doAdd) target.Add(Mathf.Abs(addAmount));
        if (doRemove) target.Remove(Mathf.Abs(removeAmount));

        if (once)
        {
            Destroy(gameObject);
            return;
        }
    }

    bool AutoResolveTargetIfNeeded()
    {
        if (target) return true;

        // ลำดับการหา: ตัวเอง → พาเรนต์ → ลูก → อันแรกในซีน
        target = GetComponent<ItemCounter>()
              ?? GetComponentInParent<ItemCounter>()
              ?? GetComponentInChildren<ItemCounter>(true)
#if UNITY_2023_1_OR_NEWER
              ?? FindFirstObjectByType<ItemCounter>();
#else
              ?? FindObjectOfType<ItemCounter>();
#endif
        return target != null;
    }
}

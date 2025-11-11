using UnityEngine;
using UnityEngine.Events;

public class ItemCounter : PlayerExtension
{
    [Min(1)] public int limit = 5;

    [SerializeField] private int count;
    public int Count
    {
        get => count;
        set { count = Mathf.Max(0, value); CheckLimit(); OnChanged?.Invoke(count, limit); }
    }

    [Header("Events")]
    public UnityEvent<int, int> OnChanged; // (count, limit)
    public UnityEvent OnLimited;          // ยิงเมื่อ count == limit

    [Tooltip("ให้ OnLimited ยิงครั้งเดียวต่อรอบ (จะรีเซ็ตเมื่อ count < limit)")]
    public bool fireOncePerCycle = true;

    private bool hasFired;

    void Update()
    {
        // กันกรณีปรับค่าใน Inspector ระหว่างรัน
        CheckLimit();
    }

    void CheckLimit()
    {
        if (count == limit)
        {
            if (!fireOncePerCycle || !hasFired)
            {
                hasFired = true;
                OnLimited?.Invoke();
            }
        }
        else if (count < limit)
        {
            hasFired = false; // ลดลงต่ำกว่า limit เมื่อไร อนุญาตให้ยิงใหม่
        }
    }

    // ------- Helpers สำหรับเรียกจากที่อื่น/Inspector -------
    public void Add(int amount = 1) => Count = count + amount;
    public void Remove(int amount = 1) => Count = count - amount;
    public void ResetCounter() { Count = 0; hasFired = false; }

#if UNITY_EDITOR
    [ContextMenu("Add 1 (Test)")] void _Add1() => Add(1);
#endif
}

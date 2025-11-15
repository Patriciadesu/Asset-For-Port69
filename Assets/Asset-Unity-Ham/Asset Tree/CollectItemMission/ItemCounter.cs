using System.Collections;
using UnityEngine;
using UnityEngine.Events;

public partial class ItemCounter : PlayerExtension
{
    [Min(1),HideInInspector] public int limit = 5;

    [SerializeField] private int count;
    public int Count
    {
        get => count;
        set { count = Mathf.Max(0, value); CheckLimit(); OnChanged?.Invoke(count, limit); }
    }

    [Header("Events")]
    [HideInInspector]public UnityEvent<int, int> OnChanged; // (count, limit)
    public UnityEvent OnLimited;          // �ԧ����� count == limit

    [Tooltip("��� OnLimited �ԧ�������ǵ���ͺ (����������� count < limit)")]
    private bool fireOncePerCycle = true;

    private bool hasFired;
    private Coroutine autoLimitRoutine;
    partial void InitializeRandomSpawnModule();

    void OnEnable()
    {
        if (autoLimitRoutine != null)
        {
            StopCoroutine(autoLimitRoutine);
        }
        autoLimitRoutine = StartCoroutine(DelayAndSetLimit());
        InitializeRandomSpawnModule();
    }

    IEnumerator DelayAndSetLimit()
    {
        yield return new WaitForSeconds(0.5f);
        int triggerCount = CountItemCounterTriggers();
        if (triggerCount > 0)
        {
            limit = triggerCount;
            OnChanged?.Invoke(count, limit);
            CheckLimit();
        }
        autoLimitRoutine = null;
    }

    void Update()
    {
        // �ѹ�óջ�Ѻ���� Inspector �����ҧ�ѹ
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
            hasFired = false; // Ŵŧ��ӡ��� limit ������� ͹حҵ����ԧ����
        }
    }

    // ------- Helpers ����Ѻ���¡�ҡ������/Inspector -------
    public void Add(int amount = 1) => Count = count + amount;
    public void Remove(int amount = 1) => Count = count - amount;
    public void ResetCounter() { Count = 0; hasFired = false; }

#if UNITY_EDITOR
    [ContextMenu("Add 1 (Test)")] void _Add1() => Add(1);
#endif

    private int CountItemCounterTriggers()
    {
#if UNITY_2023_1_OR_NEWER
        return FindObjectsByType<ItemToCollect>(FindObjectsInactive.Exclude, FindObjectsSortMode.None).Length;
#else
        return FindObjectsOfType<ItemToCollect>().Length;
#endif
    }
}

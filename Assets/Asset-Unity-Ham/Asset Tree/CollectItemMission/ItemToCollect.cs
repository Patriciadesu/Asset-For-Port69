using System.Reflection;
using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(Collider))]
public class ItemToCollect : ObjectEffect
{
    [Header("Who can trigger")]
    private string playerTag = "Player";
    private bool once = true;

    [Header("Target Counter")]
    private ItemCounter target;

    [Header("Collect Settings")]
    [SerializeField, Min(1)] private int addAmount = 1;
    [SerializeField] private UnityEvent onCollect;

    private void Start()
    {
        if (Player.Instance.gameObject.TryGetComponent<ItemCounter>(out ItemCounter itemCounter))
        {
            target = itemCounter;
        }
        else
        {
            target = Player.Instance.gameObject.AddComponent<ItemCounter>();
        }
    }

    public override void ApplyEffect(Collision collision,Player player)
    {
        if (!collision.gameObject.CompareTag(playerTag)) return;

        target.Add(Mathf.Abs(addAmount));
        onCollect?.Invoke();

        if (once)
        {
            Destroy(gameObject);
        }
    }

}

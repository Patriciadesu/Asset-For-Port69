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

    private bool collected;

    private void Start()
    {
            target = ItemCounter.Instance;
        
    }

    public override void ApplyEffect(Collision collision,Player player)
    {
        if (!collision.gameObject.CompareTag(playerTag)) return;
        HandleCollect();
    }

    public override void ApplyEffect(Player player)
    {
        if (!player.CompareTag(playerTag)) return;
        HandleCollect();
    }

    private void HandleCollect()
    {
        if (collected) return;
        collected = true;

        target.Add(Mathf.Abs(addAmount));
        onCollect?.Invoke();

        if (once)
        {
            Destroy(gameObject);
        }
    }

}

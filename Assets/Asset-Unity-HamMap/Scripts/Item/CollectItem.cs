using UnityEngine;
using UnityEngine.Events;

public class CollectItem : ObjectEffect
{

    public ItemData itemData;
    public UnityEvent onItemCollected;

    public override void ApplyEffect(Player _player)
    {
        Inventory itemslot = FindAnyObjectByType<Inventory>();
        if (itemData.type is IPerformOnCollect)
        {
            StartCoroutine(itemData.Use());
            Destroy(gameObject); // Destroy the item after adding it to the inventory
            return;
        }
        for (int i = 0; i < itemslot.items.Length; i++)
        {
            if (itemslot.items[i] == null) // Check if the slot is empty
            {
                onItemCollected?.Invoke();
                itemslot.AddItem(itemData); // Add the item to the inventory
                Destroy(gameObject); // Destroy the item after adding it to the inventory
                break; // Exit the loop once an empty slot is found
            }
            else
            {
                Debug.Log("Inventory is full, cannot add item.");
            }
        }
    }
}


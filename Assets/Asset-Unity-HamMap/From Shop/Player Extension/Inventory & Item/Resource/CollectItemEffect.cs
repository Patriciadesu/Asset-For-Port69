using UnityEngine;

public class CollectItemEffect : ObjectEffect
{

    public ItemData itemData;

    public override void ApplyEffect(Player _player)
    {
        Itemslot itemslot = FindAnyObjectByType<Itemslot>();
        for (int i = 0; i < itemslot.items.Length; i++)
        {
            if (itemslot.items[i] == null) // Check if the slot is empty
            {
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


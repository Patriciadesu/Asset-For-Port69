using UnityEngine.UI;
using UnityEngine;
using UnityEngine.Events;

public partial class Inventory : Singleton<Inventory>
{
    public ItemData[] items = new ItemData[9];
    public UnityEvent<int, ItemData> onItemCollected;
    public UnityEvent<int> onItemRemoved;




    public void AddItem(ItemData item)
    {
        for (int i = 0; i < items.Length; i++)
        {
            if (items[i] == null)
            {
                items[i] = item;
                onItemCollected.Invoke(i, item);
                return;
            }
        }

    }
    public void RemoveItem(ItemData item)
    {
        for (int i = 0; i < items.Length; i++)
        {
            if (items[i] == null)
            {
                items[i] = null;
                onItemRemoved.Invoke(i);
                return;
            }
        }
    }
    


}

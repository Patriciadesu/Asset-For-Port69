using System;
using UnityEngine;
using UnityEngine.UI;

public partial class Inventory : Singleton<Inventory>
{
    int currentIndex = 0;
    public RectTransform highlight;
    public Image[] slots = new Image[9]; // Array of slots for the inventory
    public void Start()
    {
        onItemCollected.AddListener(AddItem);
        onItemRemoved.AddListener(RemoveItem);
    }
    public void AddItem(int index, ItemData item)
    {
        slots[index].sprite = item.itemimage;
    }
    public void RemoveItem(int index)
    {
        slots[index].sprite = null;
    }
    private void Update()
    {
        if (highlight != null)
        {
            if (Input.GetAxis("Mouse ScrollWheel") > 0f) // Scroll up
            {
                currentIndex += 1; // Increment the index to scroll to the next item
                if (currentIndex >= items.Length) // Check if the index exceeds the array length
                {
                    currentIndex = 0; // Reset to the first item if it exceeds
                }


            }
            else if (Input.GetAxis("Mouse ScrollWheel") < 0f) // Scroll down
            {
                // Logic to scroll through the inventory items
                currentIndex -= 1; // Decrement the index to scroll to the previous item
                if (currentIndex < 0) // Check if the index goes below zero
                {
                    currentIndex = 8; // Reset to the last item if it goes below zero
                }
            }
            if (Input.GetKeyDown(KeyCode.E))
            {
                if (items[currentIndex] == null) // Check if the current slot is empty
                {
                    Debug.Log("No item in the current slot to use.");
                    return; // Exit if there's no item to use
                }
                else if (items[currentIndex] is IUsableItem)
                {
                    IUsableItem usableItem = items[currentIndex] as IUsableItem;
                    StartCoroutine(usableItem.Use()); // Use the item in the current slot
                    slots[currentIndex].sprite = null; // Clear the item image after use
                    items[currentIndex] = null; // Remove the item from the inventory after use
                }

            }
            highlight.position = slots[currentIndex].transform.position; // Move the highlight to the current slot position
            RefreshSlotAlphas();
        }

    }
    private void RefreshSlotAlphas()
    {
        for (int i = 0; i < slots.Length; i++)
        {
            Color c = slots[i].color;
            c.a = (items[i] != null) ? 1f : 0f;
            slots[i].color = c;
        }
    }
}
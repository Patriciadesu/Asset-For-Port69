using NaughtyAttributes;
using UnityEngine;
using System.Collections;
[CreateAssetMenu(fileName = "ItemData", menuName = "ScriptableObjects/ItemDatainventory", order = 1)]
public class ItemData : ScriptableObject
{
    public Sprite itemimage; // Image of the item
    [ShowIf("type",itemType.Heal)] public int healthAmount; // Amount of health the item restores
    [ShowIf("type", itemType.addStamina)] public int AddstaminaAmount; // Amount of stamina the item restores
    [ShowIf("type", itemType.PlayerTakedam)] public int Damageamount;
    [ShowIf("type", itemType.addspeed)] public int speedaddamount;
    public enum itemType { Heal, addStamina , PlayerTakedam ,addspeed}
    public itemType type; // Type of the item
    public void UseItem()
    {
        switch (type)
        {
            case itemType.Heal:
                Player.Instance.currenthealth += healthAmount; // Restore health
                break;
            case itemType.addStamina:
                Player.Instance.currentstamina += AddstaminaAmount; // Restore stamina
                break;
            case itemType.PlayerTakedam:
                Player.Instance.TakeDamage(Damageamount); // Example for an unknown type, just for demonstration
                break;
            case itemType.addspeed:
                Player.Instance.StartCoroutine(addspeed()); // Start the coroutine to add speed
                break;


        }

    }
    public IEnumerator addspeed() { 
        Player.Instance.additionalSpeed = speedaddamount; // Add speed
        yield return new WaitForSeconds(3f); // Wait for 5 seconds
        Player.Instance.additionalSpeed = 0; // Remove speed after 5 seconds

    }
    




}


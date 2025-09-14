using UnityEngine;
using System.Collections;
[CreateAssetMenu(fileName = "ItemData", menuName = "ScriptableObjects/ItemDatainventory", order = 1)]
public class ItemData : ScriptableObject
{
    public Sprite itemimage; // Image of the item
    [SerializeReference]
    public ItemType type;
    public IEnumerator Use() => type.OnUse();
}

[System.Serializable]
public class ItemType
{
    public virtual IEnumerator OnUse()
    {
        yield return null;
    }
}
public class Heal : ItemType,IUsableItem
{
    public float healAmount;
    public override IEnumerator OnUse()
    {
        base.OnUse();
        Player.Instance.Stat.currenthealth += healAmount;
        yield return null;
    }
}
public class Stamina : ItemType,IUsableItem
{
    public int addStaminaAmount;
    public override IEnumerator OnUse()
    {
        base.OnUse();
         Player.Instance.Stat.currentstamina += addStaminaAmount;
        yield return null;
    }
}
public class DoDamageToPlayer : ItemType,IUsableItem
{
    public int damageAmount;
    public override IEnumerator OnUse()
    {
        base.OnUse();
        Player.Instance.Stat.TakeDamage(damageAmount);
        yield return null;
    }
}
public class SpeedBoost : ItemType,IUsableItem
{
    public int speedAmount;
    public float useTime;

    public override IEnumerator OnUse()
    {
        base.OnUse();
        Player.Instance.Movement.additionalSpeed = speedAmount;
        yield return new WaitForSeconds(useTime);
        Player.Instance.Movement.additionalSpeed = 0;
    }
}


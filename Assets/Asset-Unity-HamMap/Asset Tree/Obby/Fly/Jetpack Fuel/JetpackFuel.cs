using System.Collections;
using UnityEngine;

public class JetpackFuel : ItemType
{
    public float refillAmount;
    public override IEnumerator OnUse()
    {
        base.OnUse();
        Player.Instance.gameObject.GetComponent<JetPack>().currentFuel += refillAmount;
        yield return null;
    }
}

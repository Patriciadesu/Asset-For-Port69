using UnityEngine;

public class TriggerObjectEffect : ObjectEffect
{
    public GameObject[] objectToTrigger;

    public void Awake()
    {
        foreach (GameObject go in objectToTrigger)
        {
            go.SetActive(false);
        }
    }
    
    public override void ApplyEffect(Player player)
    {
        foreach (GameObject obj in objectToTrigger)
        {
            obj.SetActive(true);
        }
    }
}
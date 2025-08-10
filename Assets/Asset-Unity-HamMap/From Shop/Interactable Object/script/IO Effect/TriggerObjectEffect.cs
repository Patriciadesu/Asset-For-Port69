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
    public override void ApplyEffect(Collision playerCollision)
    {
        foreach (GameObject obj in objectToTrigger)
        {
            obj.SetActive(true);
        }
    }
    
    public override void ApplyEffect(Collision playerCollision, Player player)
    {
        foreach (GameObject obj in objectToTrigger)
        {
            obj.SetActive(true);
        }
    }
}
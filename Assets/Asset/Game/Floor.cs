using System.Collections.Generic;
using UnityEngine;

public class Floor : MonoBehaviour
{
    [SerializeReference] public List<FloorEffect> objectives = new List<FloorEffect>();

    public void OnCollisionEnter(Collision collision)
    {
        foreach (FloorEffect obj in objectives)
        {
            obj.CollisionEnter(collision);
        }
    }
    public void OnCollisionStay(Collision collision)
    {
        foreach (FloorEffect obj in objectives)
        {
            obj.CollisionStay(collision);
        }
    }

}

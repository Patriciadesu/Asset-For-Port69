using System;
using UnityEngine;

[Serializable]
public class FloorEffect 
{
    public string name;
    public void CollisionEnter(Collision collision)
    {

    }
    public void CollisionStay(Collision collision)
    {

    }
}

public class SpeedFloor : FloorEffect
{

}
public class Jumpad : FloorEffect
{

}

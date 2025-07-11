using System;
using UnityEngine;

public class PlayerData : SingletonPersistent<PlayerData>
{
    public Vector3 position;
    public String lastScene;
    public bool isOnBoat;
    public int money;
    public int health;
    public Inventory inventory;
}

public class Inventory {
    
}

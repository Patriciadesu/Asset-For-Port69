using UnityEngine;

public class PlayerExtension : MonoBehaviour
{
    protected PlayerController _player;
    public virtual void OnStart(PlayerController player) 
    {
        _player = player;
    }
}







using UnityEngine;

public class PlayerExtension : MonoBehaviour
{
    protected Player _player;
    public virtual void OnStart(Player player)
    {
        _player = player;
        _player.OnUpdate += OnUpdate;
        _player.OnFixedUpdate += OnFixedUpdate;
        _player.OnCollisionEnterEvent += OnCollisionEnterEvent;
        _player.OnCollisionStayEvent += OnCollisionStayEvent;
        _player.OnCollisionExitEvent += OnCollisionExitEvent;
        _player.OnTriggerEnterEvent += OnTriggerEnterEvent;
        _player.OnTriggerStayEvent += OnTriggerStayEvent;
        _player.OnTriggerExitEvent += OnTriggerExitEvent;
    }
    protected virtual void OnFixedUpdate()
    {
        // Override this method in derived classes to implement fixed update logic
    }
    protected virtual void OnUpdate()
    {
        // Override this method in derived classes to implement update logic
    }
    protected virtual void OnCollisionEnterEvent(Collision collision)
    {
        // Override this method in derived classes to handle collision enter events
    }
    protected virtual void OnCollisionStayEvent(Collision collision)
    {
        // Override this method in derived classes to handle collision stay events
    }
    protected virtual void OnCollisionExitEvent(Collision collision)
    {
        // Override this method in derived classes to handle collision exit events
    }
    protected virtual void OnTriggerEnterEvent(Collider other)
    {
        // Override this method in derived classes to handle trigger enter events
    }
    protected virtual void OnTriggerStayEvent(Collider other)
    {
        // Override this method in derived classes to handle trigger stay events
    }
    protected virtual void OnTriggerExitEvent(Collider other)
    {
        // Override this method in derived classes to handle trigger exit events
    }

}







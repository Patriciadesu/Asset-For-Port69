using UnityEngine;

public class Reflect : PlayerExtension
{
    PlayerController _player;
    public KeyCode activateKey = KeyCode.E;
    public float cooldown = 1.0f;
    public float reflectDuration = 0.1f;
    public float reflectDelay = 0.1f;
    public bool isReady = true;
    public override void Apply(PlayerController player)
    {
        _player = player;
        if(isReady && Input.GetKeyDown(activateKey))
        {
            this.Invoke("Reflecting", reflectDelay);
            this.Invoke("ReflectDown", reflectDuration + reflectDelay);
            this.Invoke("ResetCooldown", cooldown);
        }
    }
    public void Reflecting()
    {
        _player.isReflecting = true;
        isReady = false;
        _player.anim.SetTrigger("Reflect");
    }
    public void ReflectDown()
    {
        _player.isReflecting = false;
    }
    public void ResetCooldown()
    {
        isReady = true;
    }
}

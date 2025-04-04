using UnityEngine;

public class Roll : PlayerExtension
{
    PlayerController _player;
    public KeyCode activateKey = KeyCode.Q;
    public float slideSpeed = 10f;
    public float slideDuration = 0.5f;
    private Vector3 slideDirection;
    private float slideAnimSpeed;


    public override void Apply(PlayerController player)
    {
        if(_player == null) _player = player;
        if (player.isSliding) 
        {
            player.controller.Move((slideDirection * slideSpeed) * player.speed * Time.deltaTime);
        }
        else
        {
            if (Input.GetKeyDown(activateKey))
            {
                slideAnimSpeed = slideSpeed / player.GetAnimationLength("Slide");
                player.isSliding = true;
                player.controller.height /= 2;
                player.controller.center = new Vector3(player.controller.center.x, player.controller.center.y / 2, player.controller.center.z);
                slideDirection = player.transform.forward; // Lock slide direction
                player.anim.speed = slideAnimSpeed;
                player.anim.SetTrigger("Slide");
                this.Invoke("StopSlide", slideDuration+0.25f);
            }
        }
    }
    void StopSlide()
    {
        _player.controller.height *= 2;
        _player.controller.center = new Vector3(_player.controller.center.x, _player.controller.center.y * 2, _player.controller.center.z);
        _player.isSliding = false;
        _player.anim.speed = 1;
    }
}
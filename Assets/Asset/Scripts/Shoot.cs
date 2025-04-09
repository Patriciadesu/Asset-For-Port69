using UnityEngine;

public class Shoot : PlayerExtension
{
    PlayerController _player;
    public GameObject bulletPrefab;
    public float bulletSpeed = 4;
    public float cooldown = 2;

    public override void Apply(PlayerController player)
    {
        if (_player == null) _player = player;
        
    }

    void StopSlide()
    {
        // Modify collider back instead of controller
        CapsuleCollider collider = _player.GetComponent<CapsuleCollider>();
        if (collider != null)
        {
            collider.height *= 2;
            collider.center = new Vector3(collider.center.x, collider.center.y * 2, collider.center.z);
        }

        _player.isSliding = false;
        _player.anim.speed = 1;
    }
}

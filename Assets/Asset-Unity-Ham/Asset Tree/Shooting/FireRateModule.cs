using UnityEngine;

public class FireRateModule : GunModule
{
    [Header("Fire Rate Settings")]
    [SerializeField] private float fireRate = 0.1f;
    
    private float nextFireTime = 0f;
    
    public override bool CanShoot()
    {
        if (gun.IsInternalShot) return true;
        return Time.time >= nextFireTime;
    }
    
    public override void OnAfterShoot()
    {
        nextFireTime = Time.time + fireRate;
    }
}
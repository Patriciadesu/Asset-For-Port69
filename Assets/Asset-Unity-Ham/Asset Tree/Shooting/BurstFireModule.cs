using UnityEngine;
using System.Collections;

public class BurstFireModule : GunModule
{
    [Header("Burst Settings")]
    [SerializeField] private int burstCount = 3;
    [SerializeField] private float burstDelay = 0.1f;
    [SerializeField] private float burstCooldown = 0.5f;
    
    private bool isBursting = false;
    private float nextBurstTime = 0f;
    
    public override bool CanShoot()
    {
        if (gun.IsInternalShot) return true;
        return !isBursting && Time.time >= nextBurstTime;
    }
    
    public override void OnBeforeShoot()
    {
        // Only start the burst if it's NOT already bursting and NOT an internal shot
        if (!isBursting && !gun.IsInternalShot)
        {
            StartCoroutine(BurstFire());
        }
    }
    
    private IEnumerator BurstFire()
    {
        isBursting = true;
        
        // The first shot is already being handled by BaseGun.
        // We only need to trigger the remaining (burstCount - 1) shots.
        for (int i = 0; i < burstCount - 1; i++)
        {
            yield return new WaitForSeconds(burstDelay);
            gun.InternalShoot();
        }
        
        nextBurstTime = Time.time + burstCooldown;
        isBursting = false;
    }
}
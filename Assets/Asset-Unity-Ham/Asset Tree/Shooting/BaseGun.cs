using UnityEngine;
using System.Collections.Generic;

public class BaseGun : MonoBehaviour
{
    [Header("Core References")]
    [Tooltip("The bullet prefab to be spawned when shooting.")]
    [SerializeField] private GameObject bulletPrefab;
    [Tooltip("The point from which bullets will be spawned.")]
    [SerializeField] private Transform spawnPoint;
    
    [Header("Projectile Settings")]
    [Tooltip("The initial forward force/speed applied to the bullet.")]
    [SerializeField] private float bulletSpeed = 50f;
    
    private List<GunModule> modules = new List<GunModule>();
    private bool isInternalShot = false;
    
    public bool IsInternalShot => isInternalShot;

    private void Awake()
    {
        modules.AddRange(GetComponents<GunModule>());
        
        foreach (var module in modules)
        {
            module.Initialize(this);
        }
    }
    
    private void Update()
    {
        foreach (var module in modules)
        {
            module.OnUpdate();
        }
        
        if (Input.GetButtonDown("Fire1"))
        {
            TryShoot();
        }
    }

    private void LateUpdate()
    {
        foreach (var module in modules)
        {
            module.OnLateUpdate();
        }
    }
    
    public void TryShoot()
    {
        if (!CanShoot()) return;
        
        foreach (var module in modules)
        {
            module.OnBeforeShoot();
        }
        
        SpawnBullet();
        
        foreach (var module in modules)
        {
            module.OnAfterShoot();
        }
    }

    /// <summary>
    /// Called by modules to trigger a shot that should bypass certain cooldowns (like burst shots).
    /// </summary>
    public void InternalShoot()
    {
        isInternalShot = true;
        TryShoot();
        isInternalShot = false;
    }
    
    private bool CanShoot()
    {
        foreach (var module in modules)
        {
            if (!module.CanShoot())
                return false;
        }
        return true;
    }
    
    private void SpawnBullet()
    {
        if (bulletPrefab != null && spawnPoint != null)
        {
            GameObject bulletObj = Instantiate(bulletPrefab, spawnPoint.position, spawnPoint.rotation);
            Rigidbody rb = bulletObj.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.velocity = spawnPoint.forward * bulletSpeed;
            }
        }
    }
    
    public Transform GetSpawnPoint() => spawnPoint;
    public GameObject GetBulletPrefab() => bulletPrefab;
}
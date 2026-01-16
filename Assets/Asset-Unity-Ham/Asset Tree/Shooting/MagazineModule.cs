using UnityEngine;

public class MagazineModule : GunModule
{
    [Header("Magazine Settings")]
    [SerializeField] private int maxAmmo = 30;
    [SerializeField] private int currentAmmo;
    [SerializeField] private float reloadTime = 2f;
    
    private bool isReloading = false;
    private float reloadTimer = 0f;
    
    public override void Initialize(BaseGun baseGun)
    {
        base.Initialize(baseGun);
        currentAmmo = maxAmmo;
    }
    
    public override void OnUpdate()
    {
        if (isReloading)
        {
            reloadTimer += Time.deltaTime;
            if (reloadTimer >= reloadTime)
            {
                currentAmmo = maxAmmo;
                isReloading = false;
                Debug.Log("Reload Complete!");
            }
        }
        
        if (Input.GetKeyDown(KeyCode.R) && !isReloading && currentAmmo < maxAmmo)
        {
            StartReload();
        }
    }
    
    public override bool CanShoot()
    {
        return currentAmmo > 0 && !isReloading;
    }
    
    public override void OnAfterShoot()
    {
        currentAmmo--;
        Debug.Log($"Ammo: {currentAmmo}/{maxAmmo}");
        
        if (currentAmmo <= 0)
        {
            StartReload();
        }
    }
    
    private void StartReload()
    {
        isReloading = true;
        reloadTimer = 0f;
        Debug.Log("Reloading...");
    }
    
    public int GetCurrentAmmo() => currentAmmo;
    public int GetMaxAmmo() => maxAmmo;
    public bool IsReloading() => isReloading;
}
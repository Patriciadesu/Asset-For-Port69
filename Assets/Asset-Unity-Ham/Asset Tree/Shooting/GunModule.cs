using UnityEngine;

public abstract class GunModule : MonoBehaviour
{
    protected BaseGun gun;
    
    public virtual void Initialize(BaseGun baseGun)
    {
        gun = baseGun;
    }
    
    public virtual void OnUpdate() { }
    public virtual void OnLateUpdate() { }
    public virtual bool CanShoot() { return true; }
    public virtual void OnBeforeShoot() { }
    public virtual void OnAfterShoot() { }
}
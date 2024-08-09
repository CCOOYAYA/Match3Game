using Spine;
using Spine.Unity;
using UnityEngine;

/// <summary>
/// 棋子被消除时的动效对象池
/// </summary>
public class DamageVFXPool : PoolerBase<DamageVFX>
{
    [SerializeField] private DamageVFX damageVFXPrefab;
    [SerializeField] private int initialCount;
    [SerializeField] private int maxCount;

    public void InitializePool()
    {
        InitializePool(damageVFXPrefab, initialCount, maxCount);
    }


    protected override DamageVFX CreateSetup()
    {
        var vfx = base.CreateSetup();
        vfx.transform.parent = transform;
        vfx.SkeletonAnimation = vfx.GetComponent<SkeletonAnimation>();
        return vfx;
    }


    public void PlayDamageVFXAt(Vector3 playPosition, AnimationReferenceAsset damageAnimation)
    {
        var vfx = Get();
        vfx.transform.position = playPosition;
        vfx.Play(damageAnimation);
        vfx.SkeletonAnimation.AnimationState.Complete += delegate { Release(vfx); };
    }
}

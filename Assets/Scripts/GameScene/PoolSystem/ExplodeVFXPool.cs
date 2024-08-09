using Spine.Unity;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// 所有无脚本绑定的(只是表现用的)动效对象池
/// </summary>
public class ExplodeVFXPool : PoolerBase<ExplodeVFX>
{
    [SerializeField] private ExplodeVFX explodeVFXPrefab;
    [SerializeField] private int initialCount;
    [SerializeField] private int maxCount;

    public void InitializePool()
    {
        InitializePool(explodeVFXPrefab, initialCount, maxCount);
    }

    protected override ExplodeVFX CreateSetup()
    {
        var vfx = base.CreateSetup();
        vfx.transform.parent = transform;
        vfx.SortingGroup = vfx.GetComponent<SortingGroup>();
        vfx.SkeletonAnimation = vfx.GetComponent<SkeletonAnimation>();
        return vfx;
    }


    public void PlayExplodeVFXAt(GridPosition playPosition, AnimationReferenceAsset explodeAnimation, int order = 2)
    {
        var vfx = Get();
        vfx.transform.SetPositionAndRotation(GameBoardManager.instance.GetGridPositionWorldPosition(playPosition), Quaternion.identity);
        vfx.PlayExplode(explodeAnimation, order);
        vfx.SkeletonAnimation.AnimationState.Complete += delegate { Release(vfx); };
    }

    public void PlayExplodeVFXAt(Vector3 playWorldPosition, Quaternion rotation, AnimationReferenceAsset explodeAnimation, int order = 2)
    {
        var vfx = Get();
        vfx.transform.SetPositionAndRotation(playWorldPosition, rotation);
        vfx.PlayExplode(explodeAnimation, order);
        vfx.SkeletonAnimation.AnimationState.Complete += delegate { Release(vfx); };
    }
}
